using System;
using System.IO;
using System.Linq;
using Dex.Editor;
using UnityEngine;
using UnityEditor;

public static class Evidence
{
    public static string State(){return "pending="+SessionState.GetBool("Dex.Agent.pending",false)+" running="+SessionState.GetBool("Dex.Agent.running",false)+" stage="+SessionState.GetString("Dex.Agent.stage","")+"\n"+SessionState.GetString("Dex.Agent.report","");}
    public static string Summary(){return SessionState.GetString("Dex.Agent.stage","")+" | "+SessionState.GetString("Dex.Agent.summary","")+" | pending="+SessionState.GetBool("Dex.Agent.pending",false)+" | "+SessionState.GetString("Dex.Agent.directory","");}
    public static string WideLayout(){var w=EditorWindow.GetWindow<AgentWorkbench>();w.position=new Rect(100,80,900,850);w.Repaint();return "Workbench resized to 900 x 850.";}
    public static async System.Threading.Tasks.Task<string> ReplayEvidence()
    {
        var arena=UnityEngine.Object.FindAnyObjectByType<Dex.DexArena>();if(arena==null)throw new Exception("Play mode required.");
        arena.StartInteractionReplay();var deadline=DateTime.UtcNow.AddSeconds(15);
        while(!arena.ReplayResult.Contains("REPLAY PASSED")&&!arena.ReplayResult.Contains("REPLAY FAILED")&&DateTime.UtcNow<deadline)await System.Threading.Tasks.Task.Delay(50);
        string result=DateTime.UtcNow.ToString("O")+"\n"+arena.ReplayResult;
        File.WriteAllText("DexReports/evidence/replay-after.txt",result);return result;
    }
    public static string RecoveryChecks()
    {
        var window=UnityEditor.EditorWindow.GetWindow<AgentWorkbench>();
        string[] keys={"started","stage","report","directory","origin","elapsed","compileStarted","summary","timeline","exploration","beforeReplay","afterReplay","verdict","reproduction","verifierHash","caseId"};
        var saved=keys.ToDictionary(k=>k,k=>SessionState.GetString("Dex.Agent."+k,""));
        bool pending=SessionState.GetBool("Dex.Agent.pending",false),running=SessionState.GetBool("Dex.Agent.running",false);
        string key=Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var log=new System.Text.StringBuilder();int passed=0;
        try
        {
            bool missingRejected=false;
            try{CodexRunner.CheckLogin(Path.GetFullPath("DexReports/missing-codex.exe"),System.Threading.CancellationToken.None).GetAwaiter().GetResult();}catch(InvalidOperationException){missingRejected=true;}
            bool ok=missingRejected;
            if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+"missing native Codex executable stops without a fabricated run");
            SessionState.SetString("Dex.Agent.directory","DexReports/evidence/recovery-probe-"+Guid.NewGuid().ToString("N"));
            SessionState.SetString("Dex.Agent.report","");
            SessionState.SetString("Dex.Agent.started",DateTime.UtcNow.Ticks.ToString());SessionState.SetString("Dex.Agent.compileStarted",DateTime.UtcNow.AddMinutes(-3).Ticks.ToString());
            SessionState.SetBool("Dex.Agent.running",true);SessionState.SetBool("Dex.Agent.pending",true);
            typeof(AgentWorkbench).GetField("resumeAfterReload",flags).SetValue(null,false);typeof(AgentWorkbench).GetMethod("Watch",flags).Invoke(null,null);
            ok=!SessionState.GetBool("Dex.Agent.pending",true)&&!SessionState.GetBool("Dex.Agent.running",true)&&SessionState.GetString("Dex.Agent.stage","")=="Failed";
            if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+"compile timeout releases pending and running state");
            string original=File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs");bool rejected=false;
            try{typeof(AgentWorkbench).GetMethod("WritePatch",flags).Invoke(null,new object[]{original+"\n// stale snapshot","true","false","test",null});}catch(System.Reflection.TargetInvocationException e){rejected=e.InnerException.Message.Contains("Source changed");}
            ok=rejected&&File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs")==original;if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+"stale source snapshot cannot overwrite current source");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY",key,EnvironmentVariableTarget.Process);
            foreach(var pair in saved)SessionState.SetString("Dex.Agent."+pair.Key,pair.Value);
            SessionState.SetBool("Dex.Agent.pending",pending);SessionState.SetBool("Dex.Agent.running",running);
        }
        string result="Recovery checks: "+passed+"/3\n"+log;File.WriteAllText("DexReports/evidence/recovery-checks.txt",result);return result;
    }
    public static string Broken(){return Capture("broken-fixture");}
    public static string Corrected(){return Capture("reference-corrected");}
    public static string RejectAll(){return Mutate("false","magicShield");}
    public static string RejectAllEvidence(){return Capture("reject-all-mutation");}
    public static string MissingGround(){return Mutate("!stunned && !rooted && !charmed","magicShield");}
    public static string MissingGroundEvidence(){return Capture("missing-ground-mutation");}
    public static string Capture(string label)
    {
        Directory.CreateDirectory("DexReports/evidence");
        string result=DateTime.UtcNow.ToString("O")+"\n"+Verification.Visible()+"\n"+Verification.Holdout();
        File.WriteAllText("DexReports/evidence/"+label+".txt",result);
        File.WriteAllText("DexReports/evidence/"+label+".source.txt",File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs"));
        return result.Split('\n').First(line=>line.StartsWith("Independent verifier:",StringComparison.Ordinal));
    }
    public static string Mutate(string move,string shield)
    {
        const string target="Assets/Dex/Runtime/CombatRules.cs";
        string source=File.ReadAllText(target);File.WriteAllText(target,RuleSource.Patch(source,move,shield));
        AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);
        return "Developer mutation applied. Await compile; no verifier changes.";
    }
    public static string Smoke()
    {
        var arena=UnityEngine.Object.FindAnyObjectByType<Dex.DexArena>();
        if(arena==null)throw new Exception("Enter Play mode first.");
        string result=arena.RunSmokeChecks();Directory.CreateDirectory("DexReports/evidence");File.WriteAllText("DexReports/evidence/arena-smoke.txt",result);return result;
    }
    public static string GuardChecks()
    {
        int total=0,passed=0;var log=new System.Text.StringBuilder();
        Action<string,Action,bool> check=(name,action,shouldReject)=>{total++;bool rejected=false;try{action();}catch{rejected=true;}bool ok=rejected==shouldReject;if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+name);};
        string source=File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs");
        foreach(var bad in new[]{"true; System.IO.File.Delete(\"x\")","stunned ||","stunnedX","/*comment*/true","System.Environment.Exit(0)","rooted ? true : false",new string('!',241)})
            check("reject unsupported expression: "+bad.Substring(0,Math.Min(40,bad.Length)),()=>RuleSource.Patch(source,bad,"magicShield"),true);
        check("reject wrong shield variable",()=>RuleSource.Patch(source,"true","rooted"),true);
        check("reject wrong nova variable",()=>RuleSource.Patch(source,"true","false","magicShield"),true);
        check("reject nova code injection",()=>RuleSource.Patch(source,"true","false","novaActive; System.IO.File.Delete(\"x\")"),true);
        check("accept valid nova grammar",()=>RuleSource.Patch(source,"true","false","novaActive && (enemyProjectile && insideNova)"),false);
        check("reject nova identifier prefix",()=>RuleSource.Patch(source,"true","false","novaActiveX"),true);
        check("accept valid boolean grammar",()=>RuleSource.Patch(source,"!(stunned || rooted) && !(charmed || grounded)","magicShield && (physicalDamage || !physicalDamage)"),false);
        check("preserve surrounding source",()=>{string marker="// USER CONTENT\n";if(!RuleSource.Patch(marker+source,"true","false").StartsWith(marker))throw new Exception();},false);
        check("reject non expression-only method",()=>RuleSource.Patch(source.Replace("return !stunned", "bool other=true; return !stunned"),"true","false"),true);
        check("reject prose around JSON",()=>AgentWorkbench.Clean("Here is the answer {\"move\":\"true\"}"),true);
        check("accept fenced JSON",()=>AgentWorkbench.Clean("```json\n{\"move\":\"true\"}\n```"),false);
        string result="Workbench guard checks: "+passed+"/"+total+"\n"+log;Directory.CreateDirectory("DexReports/evidence");File.WriteAllText("DexReports/evidence/workbench-guards.txt",result);return result;
    }
}
