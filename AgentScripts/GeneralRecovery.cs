using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dex.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Explicit host recovery tests, never a repair-agent tool.
public static class GeneralRecovery
{
    const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void SetCurrent(ProjectDebugRun run){typeof(ProjectDebugAgent).GetField("current",Flags).SetValue(null,run);}
    static void SaveHost(){typeof(ProjectDebugAgent).GetMethod("Save",Flags).Invoke(null,null);}
    static void Remember(){Directory.CreateDirectory("DexReports/evidence/general");File.WriteAllText("DexReports/evidence/general/restore-pointer.txt",File.ReadAllText("DexReports/general-current.txt"));}
    public static string CompilerRollback()
    {
        if(ProjectDebugAgent.Busy||CodexRunner.IsBusy||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("An idle Editor is required.");
        Remember();string dir="DexReports/general-recovery-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(dir);
        var run=new ProjectDebugRun{directory=dir,request="INTENTIONAL HOST RECOVERY TEST: compiler rejection at exhausted attempt budget",stage="Compiling",running=true,sourceChanged=true,attempt=3,started=DateTime.UtcNow.Ticks,compileStarted=DateTime.UtcNow.Ticks};
        run.baseline=ProjectWorkspace.Capture(dir);run.expected=run.baseline;run.protectedHash=ProjectWorkspace.ProtectedHash();
        ProjectWorkspace.Stage(dir,new PatchProposal{edits=new[]{new SourceEdit{path="Assets/Dex/Runtime/DexCompileRecoveryProbe.cs",create=true,find="",replace="namespace Dex { class DexCompileRecoveryProbe { THIS IS INTENTIONALLY INVALID CSHARP } }"}}});
        run.expected=ProjectWorkspace.Apply(dir,run.expected);SetCurrent(run);File.WriteAllText("DexReports/general-current.txt",dir);SaveHost();AssetDatabase.Refresh();CompilationPipeline.RequestScriptCompilation();
        return "Started actual compiler-failure/attempt-exhaustion/rollback test. This is a labeled host recovery test, not an AI repair.";
    }
    public static string CaptureRollbackAndRestore()
    {
        var run=ProjectDebugAgent.Current;
        bool ok=run!=null&&!run.running&&run.outcome=="Unresolved (rolled back)"&&!run.sourceChanged&&!run.verified&&!File.Exists("Assets/Dex/Runtime/DexCompileRecoveryProbe.cs")&&!EditorUtility.scriptCompilationFailed&&!EditorApplication.isCompiling;
        string result=(ok?"PASS":"FAIL")+" actual compiler rejection and bounded recovery: "+(run==null?"missing state":run.outcome)+"\nProbe source absent="+!File.Exists("Assets/Dex/Runtime/DexCompileRecoveryProbe.cs")+"; compilationFailed="+EditorUtility.scriptCompilationFailed+"\n";
        File.WriteAllText("DexReports/evidence/general/compiler-recovery.txt",result);
        if(!ok)throw new Exception(result);Restore();return result;
    }
    static void Restore()
    {
        string dir=File.ReadAllText("DexReports/evidence/general/restore-pointer.txt").Trim();var previous=JsonUtility.FromJson<ProjectDebugRun>(File.ReadAllText(Path.Combine(dir,"state.json")));SetCurrent(previous);File.WriteAllText("DexReports/general-current.txt",dir);
    }
    public static async Task<string> Cancellation()
    {
        if(ProjectDebugAgent.Busy||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle Editor required.");Remember();
        ProjectDebugAgent.Begin("INTENTIONAL CANCELLATION PROBE. Investigate why a projectile might miss; this run will be cancelled and must not apply or certify a source change.",CodexRunner.FindExecutable(),"");
        await Task.Delay(100);ProjectDebugAgent.Cancel();DateTime deadline=DateTime.UtcNow.AddSeconds(20);
        while((ProjectDebugAgent.Busy||CodexRunner.IsBusy||ArenaScenarioDriver.Busy)&&DateTime.UtcNow<deadline)await Task.Delay(50);
        var run=ProjectDebugAgent.Current;bool ok=!run.running&&!run.sourceChanged&&!run.verified&&!CodexRunner.IsBusy&&!EditorApplication.isPlayingOrWillChangePlaymode;
        string result=(ok?"PASS":"FAIL")+" cancellation stops live request without source application or success claim: "+run.outcome+"\n";File.WriteAllText("DexReports/evidence/general/cancellation.txt",result);Restore();return result;
    }
    public static async Task<string> CancelReplay()
    {
        var run=ProjectDebugAgent.Current;var scenario=JsonUtility.FromJson<ArenaScenario>(File.ReadAllText(Path.Combine(run.directory,"frozen-scenario.json")));bool cancelled=false;
        using(var cts=new CancellationTokenSource()){cts.CancelAfter(150);try{await ArenaScenarioDriver.Run(scenario,"DexReports/evidence/general","cancelled-replay",cts.Token,true);}catch(OperationCanceledException){cancelled=true;}}
        bool ok=cancelled&&!ArenaScenarioDriver.Busy&&!EditorApplication.isPlayingOrWillChangePlaymode;string result=(ok?"PASS":"FAIL")+" cancelled owned replay cleans Play and busy state\n";File.WriteAllText("DexReports/evidence/general/replay-cancellation.txt",result);return result;
    }
    public static string Persisted()
    {
        var before=ProjectDebugAgent.Current;SetCurrent(null);var loaded=ProjectDebugAgent.Current;bool ok=loaded!=null&&loaded.directory==before.directory&&loaded.scenarioHash==before.scenarioHash&&loaded.outcome==before.outcome;
        string result=(ok?"PASS":"FAIL")+" disk checkpoint reload preserves run and frozen scenario identity\n";File.WriteAllText("DexReports/evidence/general/persistence.txt",result);return result;
    }
}
