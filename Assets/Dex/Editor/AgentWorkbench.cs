using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace Dex.Editor
{
    public partial class AgentWorkbench : EditorWindow
    {
        const string Target="Assets/Dex/Runtime/CombatRules.cs", K="Dex.Agent.";
        [SerializeField] string request="Ezreal can blink while rooted by Morgana. Reproduce the failure, repair the mobility rule, and preserve legitimate casts.";
        [SerializeField] string codexModel="";
        [SerializeField] string codexPath="";
        [SerializeField] int selectedCase;
        [SerializeField] bool autoEvidence=true;
        [SerializeField] Vector2 scroll;
        GUIStyle code;
        static CancellationTokenSource cancellation;
        static bool resumeAfterReload;
        static string Get(string name,string fallback=""){return SessionState.GetString(K+name,fallback);}
        static void Set(string name,string value){SessionState.SetString(K+name,value);}
        static string VerifierHash(){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText("Assets/Dex/Editor/Verification.cs")+File.ReadAllText("Assets/Dex/Editor/ReplayAutomation.cs")))).Replace("-","").ToLowerInvariant();}
        static bool Running {get{return SessionState.GetBool(K+"running",false);}set{SessionState.SetBool(K+"running",value);}}
        static bool Pending {get{return SessionState.GetBool(K+"pending",false);}set{SessionState.SetBool(K+"pending",value);}}
        static double Elapsed {get{long ticks;return long.TryParse(Get("started"),out ticks)?(DateTime.UtcNow-new DateTime(ticks,DateTimeKind.Utc)).TotalSeconds:0;}}
        static void Log(string s)
        {
            Set("report",Get("report")+DateTime.Now.ToString("HH:mm:ss")+"  "+s+"\n\n");
            string dir=Get("directory");if(!string.IsNullOrEmpty(dir)){Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"run.txt"),Get("report"));}
        }
        static void Stage(string name){Set("stage",name);Set("timeline",Get("timeline")+Elapsed.ToString("0.0")+"s  /  "+name+"\n");Log(name+" / elapsed "+Elapsed.ToString("0.0")+"s");}
        static void Finish(bool passed,string reason)
        {
            string completedStage=Get("stage");
            Running=false;Pending=false;Set("elapsed",Elapsed.ToString("0.0"));Stage(passed?"Passed":"Failed");Log(reason);
            string directory=Get("directory");if(!string.IsNullOrEmpty(directory))
            {File.WriteAllText(Path.Combine(directory,"timeline.txt"),Get("timeline"));File.WriteAllText(Path.Combine(directory,"SUMMARY.md"),"# Dex run\n\nRun type: "+Get("origin")+"\n\nResult: "+Get("stage")+" — "+Get("summary")+"\n\nElapsed: "+Get("elapsed")+" seconds. No manual baseline has been measured.\n\n"+reason+"\n\n## Timeline\n\n"+Get("timeline")+"\n## Exploration\n\n"+Get("exploration")+"\n\nThe agent has read-only tools. The workbench applies validated expressions and verifies them independently. See source diffs, verifier hashes, CLI event transcripts and before/after replay captures in this directory.");}
            if(SessionState.GetBool(K+"demoAutoRun",false)&&Get("origin").StartsWith("Intentional",StringComparison.Ordinal))
            {SessionState.SetBool(K+"demoAutoRun",false);if(completedStage=="Verifying"&&!EditorUtility.scriptCompilationFailed)SessionState.SetBool(K+"beginAfterFixture",true);}
        }
        static void StartRecord(string origin)
        {
            cancellation?.Cancel();cancellation=new CancellationTokenSource();
            Set("started",DateTime.UtcNow.Ticks.ToString());Set("elapsed","");Set("report","");Set("diff","");Set("summary","");Set("timeline","");Set("exploration","");Set("reproduction","");Set("verdict","");Set("beforeReplay","");Set("afterReplay","");
            Set("directory","DexReports/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));
            Set("origin",origin);SessionState.SetInt(K+"attempt",0);Log("Run type: "+origin+". No manual baseline or time-savings measurement.");
            Set("verifierHash",VerifierHash());File.WriteAllText(Path.Combine(Get("directory"),"verifier-before.sha256"),Get("verifierHash"));
        }
        [MenuItem("Dex/Agent Workbench")]
        public static void Open(){GetWindow<AgentWorkbench>("Dex Agent").minSize=new Vector2(360,420);}
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            resumeAfterReload=Pending;
            AssemblyReloadEvents.beforeAssemblyReload+=CodexRunner.Abort;
            EditorApplication.quitting+=CodexRunner.Abort;
            EditorApplication.update+=Watch;
            if(SessionState.GetBool(K+"replayPending",false))EditorApplication.update+=StartReplayWhenReady;
            CompilationPipeline.assemblyCompilationFinished+=(path,messages)=>
            {
                if(!Pending)return;
                var errors=messages.Where(m=>m.type==CompilerMessageType.Error).ToArray();
                if(errors.Length>0)Finish(false,"Compilation failed. Source and backups preserved.\n"+string.Join("\n",errors.Select(m=>m.message)));
            };
            if(Running&&!Pending)Finish(false,"Editor reloaded during a request. No success assumed; inspect reports and start again.");
        }
        [DidReloadScripts]
        static void Reloaded(){if(Pending)resumeAfterReload=true;}
        static async void ContinueAfterCompile()
        {
            if(!Pending)return;
            try
            {
                if(File.ReadAllText(Target)!=Get("expectedSource")){Finish(false,"Source changed during compilation. Verification stopped to preserve external edits.");return;}
                if(EditorUtility.scriptCompilationFailed){Finish(false,"Unity reports compilation failure. Backup preserved.");return;}
                string verifierHash=VerifierHash();File.WriteAllText(Path.Combine(Get("directory"),"verifier-after.sha256"),verifierHash);
                if(verifierHash!=Get("verifierHash")){Finish(false,"Verifier source changed during the run. Refusing to claim independent verification; inspect the saved hashes.");return;}
                Pending=false;Stage("Verifying");var checks=Verification.Evaluate();Set("summary",checks.passed+" / "+checks.total+" independent checks passed");
                Log(Verification.Visible()+"\n"+checks);File.WriteAllText(Path.Combine(Get("directory"),"verification.txt"),checks.ToString());
                Set("verdict",checks.ToString());
                if(checks.Success)await CompleteVerifiedRun();
                else if(Get("origin")=="Live Codex CLI"&&SessionState.GetInt(K+"attempt",0)<3){cancellation=new CancellationTokenSource();await Repair(cancellation.Token);}
                else Finish(false,"Independent verification rejected this source. No assertions were changed.");
            }
            catch(Exception e){Finish(false,"Continuation failed: "+e.Message);}
        }
        static void Watch()
        {
            if(SessionState.GetBool(K+"beginAfterFixture",false)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&!CodexRunner.IsBusy&&!ReplayAutomation.Busy)
            {SessionState.SetBool(K+"beginAfterFixture",false);GetWindow<AgentWorkbench>().BeginCodex();}
            if(!Running)return;
            if(Pending&&resumeAfterReload&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){resumeAfterReload=false;ContinueAfterCompile();return;}
            long stamp;if(Pending&&long.TryParse(Get("compileStarted"),out stamp)&&(DateTime.UtcNow-new DateTime(stamp,DateTimeKind.Utc)).TotalSeconds>120)
                Finish(false,"Compilation/domain reload timed out after 120s. Inspect Console; original and proposed source are saved. No verification success assumed.");
            else if(Elapsed>600){cancellation?.Cancel();CodexRunner.Abort();Finish(false,"Run exceeded the 10 minute safety limit. Inspect report and retry.");}
        }
        void OnInspectorUpdate(){if(Running)Repaint();}
        public static void Checks()
        {StartRecord("Independent checks (no AI)");Stage("Investigating");string visible=Verification.Visible();Log(visible);if(visible.Contains(": FAIL"))Stage("Reproduced");Stage("Verifying");var r=Verification.Evaluate();Set("summary",r.passed+" / "+r.total+" independent checks passed");Log(r.ToString());File.WriteAllText(Path.Combine(Get("directory"),"verification.txt"),r.ToString());Finish(r.Success,"Checked the currently loaded rules. No source edited.");}
        public static void Fixture(){FixtureFor("root-blink");}
        public static void FixtureFor(string id)
        {
            Set("caseId",DemoCases.Find(id).id);
            ApplyDeveloper(id=="root-blink"||id=="root-dash"?"!stunned":"!stunned && !rooted && !charmed && !grounded",id=="shield-charm"?"false":"magicShield","Intentional "+id+" fixture reset",id=="mel-nova"?"false":"novaActive && enemyProjectile && insideNova");
        }
        public static void ReferenceRepair(){ApplyDeveloper("!stunned && !rooted && !charmed && !grounded","magicShield","Offline reference repair (NOT AI)","novaActive && enemyProjectile && insideNova");}
        static void ApplyDeveloper(string move,string shield,string origin,string projectile=null)
        {
            if(Running||ProjectDebugAgent.Busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the active run and Play mode first.");
            StartRecord(origin);Running=true;
            try{Stage("Investigating");string visible=Verification.Visible();Log(visible);if(visible.Contains(": FAIL"))Stage("Reproduced");Stage("Repairing");WritePatch(File.ReadAllText(Target),move,shield,"Explicit developer action.",projectile);}
            catch(Exception e){Finish(false,e.Message);}
        }
        async void CheckCodex()
        {
            try{Set("cliStatus","Checking saved Codex sign-in...");Set("cliStatus",await CodexRunner.CheckLogin(codexPath,CancellationToken.None));}
            catch(Exception e){Set("cliStatus",e.Message);}Repaint();
        }
        public static void ReplayInArena()
        {
            SessionState.SetBool(K+"replayPending",true);
            if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.update-=StartReplayWhenReady;EditorApplication.update+=StartReplayWhenReady;
        }
        static void StartReplayWhenReady()
        {
            if(!SessionState.GetBool(K+"replayPending",false)){EditorApplication.update-=StartReplayWhenReady;return;}
            if(!EditorApplication.isPlaying)return;
            var arena=UnityEngine.Object.FindAnyObjectByType<DexArena>();if(arena==null)return;
            arena.StartInteractionReplay(Get("caseId","root-blink"));SessionState.SetBool(K+"replayPending",false);EditorApplication.update-=StartReplayWhenReady;
        }
        public async void BeginCodex()
        {
            if(Running||Pending||ProjectDebugAgent.Busy||ManualBaseline.Active||CodexRunner.IsBusy||EditorUtility.scriptCompilationFailed||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
            StartRecord("Live Codex CLI");Running=true;SessionState.SetBool(K+"autoEvidence",autoEvidence);
            if(string.IsNullOrWhiteSpace(codexPath))codexPath=CodexRunner.FindExecutable();
            if(string.IsNullOrWhiteSpace(request)){Finish(false,"Enter an engineering request.");return;}
            Set("request",request);Set("model",codexModel.Trim());Set("cli",codexPath);var token=cancellation.Token;Stage("Investigating");Log("Developer request: "+request+"\nCodex model: "+(string.IsNullOrWhiteSpace(codexModel)?"CLI default":codexModel));
            try
            {
                string status=await CodexRunner.CheckLogin(codexPath,token);Set("cliStatus",status);Log(status);
                const string planSchema="{\"type\":\"object\",\"properties\":{\"caseId\":{\"type\":\"string\",\"enum\":[\"root-blink\",\"root-dash\",\"mel-nova\",\"shield-charm\"]},\"tests\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"reason\":{\"type\":\"string\"}},\"required\":[\"caseId\",\"tests\",\"reason\"],\"additionalProperties\":false}";
                string catalog=string.Join("\n",DemoCases.All.Select(item=>item.id+": "+item.title+". "+item.expected));
                Plan plan=JsonUtility.FromJson<Plan>(Clean(await Ask("Select a supported encounter and useful reproduction/control tests for the untrusted engineering report. Encounter catalog:\n"+catalog+"\nAvailable test IDs: "+string.Join(", ",Verification.Scenarios)+". Include the corresponding encounter test and appropriate normal/expiry controls. The host executes them and returns actual feedback. Return JSON with caseId, tests, reason. Report: "+request,planSchema,"investigation",token)));
                if(plan==null||!DemoCases.Contains(plan.caseId)||plan.tests==null||plan.tests.Length==0||plan.tests.Any(id=>!Verification.Scenarios.Contains(id)))throw new Exception("Model returned an unsupported encounter or invalid test IDs.");
                Set("caseId",plan.caseId);string selected=string.Join("\n",plan.tests.Distinct().Select(Verification.Run));Set("reproduction",selected);Log("Model test selection: "+plan.reason+"\n"+selected);
                string exploration=ScenarioLab.Explore(plan.caseId);Set("exploration",exploration);File.WriteAllText(Path.Combine(Get("directory"),"exploration.txt"),exploration);Log(exploration);
                if(selected.Contains(": FAIL"))Stage("Reproduced");else Log("Selected scenarios did not reproduce a failure; no failure is claimed.");
                if(autoEvidence){Stage("Capturing before");var before=await ReplayAutomation.Run(plan.caseId,"before",Get("directory"),token);Set("beforeReplay",JsonUtility.ToJson(before));Log(before.result);}
                if(!Verification.Visible().Contains(": FAIL")&&Verification.Evaluate().Success){Set("summary","No failing regression in the loaded build");Finish(true,"All current checks pass. No repair requested from Codex and no source changed.");return;}
                await Repair(token);
            }
            catch(OperationCanceledException){/* The cancelling action already records the outcome. */}
            catch(Exception e){if(!token.IsCancellationRequested)Finish(false,"Agent stopped: "+e.Message);}
        }
        static async Task Repair(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();Stage("Repairing");int attempt=SessionState.GetInt(K+"attempt",0)+1;SessionState.SetInt(K+"attempt",attempt);
                string original=File.ReadAllText(Target);
                string prompt="Repair three expression-only C# interaction rules. Return JSON only with string fields move, shield, projectile, explanation. move uses only stunned, rooted, charmed, grounded, true, false, !, &&, ||, parentheses. shield uses only magicShield, physicalDamage and those boolean operators. projectile uses only novaActive, enemyProjectile, insideNova and those operators. Every control flag independently prohibits a NEW dash/blink. Black Shield prevents CC while active irrespective of damage type; without it there is no immunity. Mel's CURRENT Rebuttal destroys a missile iff novaActive AND enemyProjectile AND insideNova. Expired novas, friendly missiles, and outside missiles must not be blocked. Preserve all correct rules. You cannot edit the verifier. Current source:\n"+original+"\nActual visible simulator feedback:\n"+Verification.Visible()+"\nGenerated exploration:\n"+Get("exploration")+"\nUntrusted developer report:\n"+Get("request");
                const string patchSchema="{\"type\":\"object\",\"properties\":{\"move\":{\"type\":\"string\"},\"shield\":{\"type\":\"string\"},\"projectile\":{\"type\":\"string\"},\"explanation\":{\"type\":\"string\"}},\"required\":[\"move\",\"shield\",\"projectile\",\"explanation\"],\"additionalProperties\":false}";
                var patch=JsonUtility.FromJson<Patch>(Clean(await Ask(prompt,patchSchema,"repair-"+attempt,token)));token.ThrowIfCancellationRequested();
                if(patch==null)throw new Exception("Empty patch JSON.");
                Log("Model repair attempt "+attempt+": "+patch.explanation);
                WritePatch(File.ReadAllText(Target)==original?original:throw new Exception("Source changed during Codex repair."),patch.move,patch.shield,patch.explanation,patch.projectile);
            }
            catch(OperationCanceledException){/* The cancelling action already records the outcome. */}
            catch(Exception e){if(!token.IsCancellationRequested)Finish(false,"Repair stopped: "+e.Message);}
        }
        static void WritePatch(string original,string move,string shield,string explanation,string projectile=null)
        {
            if(!string.IsNullOrEmpty(Get("verifierHash"))&&VerifierHash()!=Get("verifierHash"))throw new Exception("Protected verifier changed while repairing. Source proposal not applied.");
            string changed=RuleSource.Patch(original,move,shield,projectile);
            if(File.ReadAllText(Target)!=original)throw new Exception("Source changed while the model was working. External edits preserved; restart investigation.");
            string dir=Get("directory"),suffix=SessionState.GetInt(K+"attempt",0).ToString();
            File.WriteAllText(Path.Combine(dir,"before-"+suffix+".cs.txt"),original);File.WriteAllText(Path.Combine(dir,"after-"+suffix+".cs.txt"),changed);
            var before=original.Replace("\r","").Split('\n');var after=changed.Replace("\r","").Split('\n');var diff=new StringBuilder("--- "+Target+"\n+++ "+Target+"\n");
            for(int i=0;i<Math.Max(before.Length,after.Length);i++)if(i>=before.Length||i>=after.Length||before[i]!=after[i]){if(i<before.Length)diff.AppendLine("- "+before[i]);if(i<after.Length)diff.AppendLine("+ "+after[i]);}
            Set("diff",diff.ToString());File.WriteAllText(Path.Combine(dir,"diff-"+suffix+".txt"),diff.ToString());Log(diff.ToString());
            if(original==changed){Stage("Verifying");var r=Verification.Evaluate();Log(r.ToString());Finish(r.Success,"No source change proposed. Result reflects current loaded code.");return;}
            Set("expectedSource",changed);Set("compileStarted",DateTime.UtcNow.Ticks.ToString());Pending=true;Stage("Compiling");
            // A crash before reload leaves a checkpoint; the watchdog never treats an import as a pass.
            string temporary=Path.Combine(dir,"candidate-source.tmp");File.WriteAllText(temporary,changed);
            if(File.ReadAllText(Target)!=original)throw new Exception("Source changed before replacement; external edits preserved.");
            try{File.Replace(temporary,Path.GetFullPath(Target),null);}
            catch(IOException)
            {
                // Windows/Mono can reject Replace while the asset watcher holds a handle.
                // Acquire an exclusive handle, recheck the exact original under that lock,
                // then write the validated proposal. The durable before/after copies above
                // remain available even if the Editor or machine stops during this write.
                Log("Atomic file replacement unavailable; retrying with an exclusive, checked source write.");
                using(var file=new FileStream(Target,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                {
                    string current;using(var reader=new StreamReader(file,Encoding.UTF8,true,1024,true))current=reader.ReadToEnd();
                    if(current!=original)throw new IOException("Source changed before exclusive write; external edits preserved.");
                    byte[] bytes=new UTF8Encoding(false).GetBytes(changed);file.Position=0;file.Write(bytes,0,bytes.Length);file.SetLength(bytes.Length);file.Flush(true);
                }
            }
            AssetDatabase.ImportAsset(Target,ImportAssetOptions.ForceUpdate);CompilationPipeline.RequestScriptCompilation();
        }
        public static string Clean(string text)
        {
            if(text==null||text.Length>20000)throw new Exception("Missing or oversized model JSON.");
            text=text.Trim();if(text.StartsWith("```",StringComparison.Ordinal)){int n=text.IndexOf('\n');if(n<0||!text.EndsWith("```",StringComparison.Ordinal))throw new Exception("Malformed JSON fence.");text=text.Substring(n+1,text.Length-n-4).Trim();}
            if(!text.StartsWith("{",StringComparison.Ordinal)||!text.EndsWith("}",StringComparison.Ordinal))throw new Exception("Expected a JSON object without surrounding prose.");return text;
        }
        static Task<string> Ask(string input,string schema,string tag,CancellationToken token)
        {return CodexRunner.Ask(Get("cli"),Get("model"),input,schema,Get("directory"),tag,token,message=>Log(message));}
        static async Task CompleteVerifiedRun()
        {
            if(Get("origin")=="Live Codex CLI"&&SessionState.GetBool(K+"autoEvidence",true))
            {Stage("Replaying");cancellation=new CancellationTokenSource();var after=await ReplayAutomation.Run(Get("caseId","root-blink"),"after",Get("directory"),cancellation.Token);Set("afterReplay",JsonUtility.ToJson(after));Log(after.result);if(!after.passed){Finish(false,"Compiled rules passed core checks, but the playable replay failed. Inspect the captured evidence.");return;}}
            Finish(true,"Independent compiled checks passed"+(Get("origin")=="Live Codex CLI"&&SessionState.GetBool(K+"autoEvidence",true)?" and the actual arena replay passed. Before/after evidence saved automatically.":". Inspect the saved source diff and evidence."));GetWindow<AgentWorkbench>().Repaint();
        }
        public void SelectCase(string id){selectedCase=Array.FindIndex(DemoCases.All,item=>item.id==id);if(selectedCase<0)selectedCase=0;request=DemoCases.All[selectedCase].report;Set("caseId",DemoCases.All[selectedCase].id);}
        public void RunGuidedDemo(){if(Running||Pending||ManualBaseline.Active||EditorUtility.scriptCompilationFailed||EditorApplication.isPlaying||EditorApplication.isCompiling)return;SelectCase(DemoCases.All[Mathf.Clamp(selectedCase,0,DemoCases.All.Length-1)].id);SessionState.SetBool(K+"demoAutoRun",true);FixtureFor(DemoCases.All[selectedCase].id);}
        [Serializable] class Plan {public string[] tests;public string reason,caseId;}
        [Serializable] class Patch {public string move,shield,projectile,explanation;}
    }
}
