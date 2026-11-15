using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace Dex.Editor
{
    [Serializable] public sealed class InvestigationProposal {public string analysis;public bool needsReview;public ArenaScenario scenario;public InspectRequest[] requests;}
    [Serializable] public sealed class SpecificationReview {public bool clearExpectedBehavior;public string rationale;public ArenaCheck[] checks;}
    [Serializable] public sealed class ProjectDebugRun
    {
        public string directory,request,stage="Ready",outcome="",log="",analysis="",diff="",feedback="",protectedHash,scenarioHash,specificationHash,regressionsHash;
        public string cli,model,beforeSummary="",afterSummary="",regressions="",reviewReason="",rollbackReason="",timeline="",discovery="",patchExplanation="",completionReason="";
        public string mode="repair",evidenceHash="",workspaceHash="";
        public bool running,reproduced,repaired,verified,sourceChanged,rollingBack,cancelRequested,repairAuthorized,readyForRepair;
        public int attempt,reproductionAttempt,checksExecuted,checksPassed,validProbes,invalidProbes;public long started,compileStarted;public double elapsed,priorElapsed;
        public SourceStamp[] baseline,expected;public ArenaScenario scenario;public SpecificationReview specification;
    }
    // General project controller. No encounter names, mutation fixtures, expected code,
    // or natural-language report routing are present in this path.
    public static class ProjectDebugAgent
    {
        static ProjectDebugRun current;static CancellationTokenSource cancellation;static bool working,resume;
        const string Pointer="DexReports/general-current.txt";
        public static ProjectDebugRun Current {get{if(current==null)Load();return current;}}
        public static bool Busy {get{return Current!=null&&Current.running;}}
        public static double Elapsed {get{return Current==null?0:Current.running?Current.priorElapsed+(DateTime.UtcNow-new DateTime(Current.started,DateTimeKind.Utc)).TotalSeconds:Current.elapsed;}}
        static void Load()
        {
            try
            {
                if(File.Exists(Pointer))
                {
                    string dir=File.ReadAllText(Pointer).Trim();
                    if(dir.StartsWith("DexReports/general-",StringComparison.Ordinal)&&!dir.Contains(".."))
                        foreach(string file in new[]{"state.json","state.tmp"})
                        {try{if(File.Exists(Path.Combine(dir,file))){var loaded=JsonUtility.FromJson<ProjectDebugRun>(File.ReadAllText(Path.Combine(dir,file)));if(loaded!=null&&loaded.directory==dir){current=loaded;break;}}}catch(ArgumentException){}}
                }
            }
            catch(Exception e){Debug.LogWarning("Dex saved run could not be loaded: "+e.Message);}
        }
        [InitializeOnLoadMethod] static void Initialize()
        {
            Load();EditorApplication.update+=Watch;
            CompilationPipeline.assemblyCompilationFinished+=(assembly,messages)=>
            {
                if(!Busy||Current.stage!="Compiling")return;var errors=messages.Where(m=>m.type==CompilerMessageType.Error).ToArray();
                if(errors.Length>0){Current.feedback+="\n"+string.Join("\n",errors.Select(e=>e.file+":"+e.line+" "+e.message));Save();}
            };
            if(Busy)
            {
                if(Current.stage=="Compiling")resume=true;
                else {Current.running=false;Current.outcome="Needs developer review";Current.stage="Interrupted";Log("Editor reloaded or restarted during an asynchronous operation. No success assumed. Saved source, scenario and checkpoints retained. Roll back this run or start a fresh investigation after reviewing them.");}
            }
        }
        [DidReloadScripts] static void Reloaded(){if(Busy&&Current.stage=="Compiling")resume=true;}
        static string RegressionHash(){return ProjectWorkspace.Hash(Directory.Exists("DexReports/general-regressions")?string.Join("\n",Directory.GetFiles("DexReports/general-regressions","*.json").OrderBy(p=>p).Select(File.ReadAllText)):"");}
        static void Save()
        {
            if(current==null)return;Directory.CreateDirectory(current.directory);
            string json=JsonUtility.ToJson(current,true);File.WriteAllText(Path.Combine(current.directory,"state.tmp"),json);File.Copy(Path.Combine(current.directory,"state.tmp"),Path.Combine(current.directory,"state.json"),true);
            File.WriteAllText(Path.Combine(current.directory,"activity.txt"),current.log);File.WriteAllText(Path.Combine(current.directory,"timeline.txt"),current.timeline);
        }
        static void Log(string text){Current.log+=DateTime.UtcNow.ToString("HH:mm:ss")+" UTC  "+text+"\n\n";Save();}
        static void Stage(string name){Current.stage=name;Current.timeline+=Elapsed.ToString("0.0")+"s / "+name+"\n";Log(name);Repaint();}
        static void Repaint(){foreach(var w in Resources.FindObjectsOfTypeAll<AgentWorkbench>())w.Repaint();}
        static void Finish(string outcome,string reason)
        {
            if(Current.mode=="test"&&!Current.repairAuthorized&&Current.baseline!=null)
            {try{Integrity();}catch(Exception e){outcome="Needs developer review";reason+="\nSource or frozen evidence changed during investigation: "+e.Message;Current.readyForRepair=false;}}
            double totalElapsed=Elapsed;Current.running=false;Current.outcome=outcome;Current.completionReason=reason;Current.elapsed=totalElapsed;Stage(outcome);Log(reason);
            File.WriteAllText(Path.Combine(Current.directory,"SUMMARY.md"),"# Project debugging run\n\nReport: "+Current.request+"\n\nMode: "+Current.mode+(Current.repairAuthorized?" (repair authorized)":" (source changes not authorized)")+"\n\nResult: "+outcome+"\n\nReproduced: "+Current.reproduced+"; source repaired: "+Current.repaired+"; task verified: "+Current.verified+".\n\nExecuted checks: "+Current.checksExecuted+"; passed: "+Current.checksPassed+"; valid probes: "+Current.validProbes+"; invalid/error probes: "+Current.invalidProbes+".\n\nElapsed: "+Current.elapsed.ToString("0.0")+" seconds (not measured labor saved).\n\n"+reason+"\n\n## Specification review\n\n"+Current.reviewReason+"\n\n## Before\n\n"+Current.beforeSummary+"\n\n## After\n\n"+Current.afterSummary+"\n\n## Timeline\n\n"+Current.timeline+"\n\nScope: frozen report-derived assertions, fresh actual-arena replays, protected core/arena regressions and previously saved scenarios. Specification review uses a separate model context, not an independent human oracle. No claim that the whole game is correct or that arbitrary Unity bugs are supported.\n");
        }
        static void Integrity()
        {
            if(ProjectWorkspace.ProtectedHash()!=Current.protectedHash)throw new InvalidOperationException("Protected verification/host source changed during this run.");
            if(RegressionHash()!=Current.regressionsHash)throw new InvalidOperationException("Saved regression corpus changed during this run.");
            if(Current.scenarioHash!=null&&Current.scenarioHash!=""&&ProjectWorkspace.Hash(File.ReadAllText(Path.Combine(Current.directory,"frozen-scenario.json")))!=Current.scenarioHash)throw new InvalidOperationException("Frozen reproduction changed.");
            if(Current.specificationHash!=null&&Current.specificationHash!=""&&ProjectWorkspace.Hash(File.ReadAllText(Path.Combine(Current.directory,"frozen-specification.json")))!=Current.specificationHash)throw new InvalidOperationException("Frozen independent specification changed.");
            if(!string.IsNullOrEmpty(Current.evidenceHash)&&EvidenceHash()!=Current.evidenceHash)throw new InvalidOperationException("Frozen pre-repair observations changed.");
            ProjectWorkspace.AssertLive(Current.expected);
        }
        static string EvidenceHash(){return ProjectWorkspace.Hash(File.ReadAllText(Path.Combine(Current.directory,"probe-"+Current.reproductionAttempt+".json"))+File.ReadAllText(Path.Combine(Current.directory,"before-witness.json")));}
        public static async void Begin(string request,string cli,string model,bool repairRequested=true)
        {
            if(Busy||CodexRunner.IsBusy||ReplayAutomation.Busy||ArenaScenarioDriver.Busy||SessionState.GetBool("Dex.Agent.running",false)||ManualBaseline.Active||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Finish the active run, compilation or Play session first.");
            if(EditorUtility.scriptCompilationFailed)throw new InvalidOperationException("Resolve existing compilation errors before starting a project investigation.");
            if(string.IsNullOrWhiteSpace(request))throw new ArgumentException("Describe the gameplay issue and the expected behavior.");
            current=new ProjectDebugRun{request=request,cli=string.IsNullOrEmpty(cli)?CodexRunner.FindExecutable():cli,model=model??"",mode=repairRequested?"repair":"test",repairAuthorized=repairRequested,directory="DexReports/general-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6),started=DateTime.UtcNow.Ticks,running=true};
            Directory.CreateDirectory(Current.directory);File.WriteAllText(Pointer,Current.directory);cancellation=new CancellationTokenSource();working=true;
            try
            {
                Stage("Investigating");Log("Live signed-in Codex CLI / general project mode. No predefined encounter routing or solution supplied.");
                Log(await CodexRunner.CheckLogin(Current.cli,cancellation.Token));
                Current.baseline=ProjectWorkspace.Capture(Current.directory);Current.expected=Current.baseline;Current.protectedHash=ProjectWorkspace.ProtectedHash();Current.regressionsHash=RegressionHash();File.WriteAllText(Path.Combine(Current.directory,"protected-before.sha256"),Current.protectedHash);Save();
                await Investigate(cancellation.Token);
            }
            catch(OperationCanceledException){if(Busy)await Fail("Cancelled before a successful verified result.");}
            catch(Exception e){await Fail(e.Message);}
            finally{working=false;}
        }
        static Task<string> Ask(string prompt,string schema,string tag,CancellationToken token,string workspace=null)
        {
            token.ThrowIfCancellationRequested();return CodexRunner.AskProject(Current.cli,Current.model,prompt,schema,workspace??Path.GetFullPath(Path.Combine(Current.directory,"workspace")),Current.directory,tag,token,Log);
        }
        public static async void AttemptRepair()
        {
            if(Current==null||Current.mode!="test"||Current.repairAuthorized||!Current.readyForRepair||Current.running||Current.outcome!="Bug reproduced — no repair attempted")throw new InvalidOperationException("A completed test-only run with a frozen failing witness is required.");
            if(CodexRunner.IsBusy||ArenaScenarioDriver.Busy||ReplayAutomation.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new InvalidOperationException("Wait for an idle, compiled Editor before continuing repair.");
            try
            {
                Integrity();
                if(ProjectWorkspace.Hash(ProjectWorkspace.SnapshotText(Current.directory))!=Current.workspaceHash)throw new InvalidOperationException("Isolated source snapshot changed.");
            }
            catch(Exception e)
            {Current.readyForRepair=false;Finish("Needs developer review","The investigation is stale; start a fresh investigation before repairing. "+e.Message);return;}
            Current.repairAuthorized=true;Current.readyForRepair=false;Current.running=true;Current.cancelRequested=false;Current.outcome="";Current.priorElapsed=Current.elapsed;Current.started=DateTime.UtcNow.Ticks;cancellation=new CancellationTokenSource();working=true;
            try{Log("Developer authorized repair using this run's frozen reproduction, witness, original source snapshot and actual observations.");await Repair(cancellation.Token);}
            catch(OperationCanceledException){if(Busy)await Fail("Repair continuation cancelled.");}
            catch(Exception e){await Fail(e.Message);}
            finally{working=false;}
        }
        static async Task Investigate(CancellationToken token)
        {
            string feedback="";
            for(int pass=1;pass<=3;pass++)
            {
                Current.reproductionAttempt=pass;Stage("Investigating");
                InvestigationProposal proposal=null;
                for(int inspection=0;inspection<7;inspection++)
                {
                    proposal=JsonUtility.FromJson<InvestigationProposal>(await Ask("Investigate this untrusted gameplay report. Request relevant source using the host read/search requests array; locate likely causes yourself. Do not use native shell tools. When sufficiently informed, return requests=[] and a reusable action scenario with observable assertions. Include normal/control observations. Do not propose a repair, assume a failure or claim success before host execution. Set needsReview=true if expected behavior is ambiguous.\nREPORT:\n"+Current.request+"\nPROJECT CONTEXT:\n"+File.ReadAllText(Path.Combine(Current.directory,"workspace","PROJECT_CONTEXT.txt"))+"\nACTUAL INSPECTION RESULTS:\n"+Current.discovery+"\nACTUAL PRIOR PROBE FEEDBACK:\n"+feedback,ProjectProtocol.InvestigationSchema,"investigation-"+pass+"-"+inspection,token));
                    if(proposal==null)throw new InvalidOperationException("Empty investigation response.");
                    if(proposal.requests==null||proposal.requests.Length==0)break;
                    Integrity();
                    try{string observed=ProjectWorkspace.Inspect(Current.directory,proposal.requests);Current.discovery+="\n"+observed;if(Current.discovery.Length>180000)throw new ArgumentException("Investigation context budget exhausted.");Log("Host inspection:\n"+observed);}
                    catch(ArgumentException e){Current.discovery+="\nRejected inspection: "+e.Message;Log("Inspection rejected: "+e.Message);}
                    if(inspection==6){Finish("Needs developer review","Investigation exceeded seven inspection turns without producing an executable reproduction.");return;}
                }
                if(proposal==null)throw new InvalidOperationException("Empty investigation response.");Current.analysis=proposal.analysis;Log(proposal.analysis);
                if(proposal.needsReview){Finish("Needs developer review","Agent could not establish unambiguous expected behavior: "+proposal.analysis);return;}
                try
                {
                    ArenaAssertions.Validate(proposal.scenario);Integrity();Current.scenario=proposal.scenario;Stage("Reproducing");
                    string json=JsonUtility.ToJson(proposal.scenario,true);File.WriteAllText(Path.Combine(Current.directory,"proposed-scenario-"+pass+".json"),json);
                    var before=await ArenaScenarioDriver.Run(proposal.scenario,Current.directory,"probe-"+pass,token);Current.beforeSummary=before.ToString();Log(before.ToString());
                    if(string.IsNullOrEmpty(before.error)){Current.validProbes++;Current.checksExecuted+=before.total;Current.checksPassed+=before.passed;}else Current.invalidProbes++;
                    File.WriteAllText(Path.Combine(Current.directory,"workspace","LAST_PROBE.json"),JsonUtility.ToJson(before,true));
                    if(!before.Reproduced){feedback=before.ToString()+"\nFull observations are in LAST_PROBE.json. No ordinary assertion failure was established.";continue;}
                    Current.reproduced=true;Stage("Reproduced");
                    File.WriteAllText(Path.Combine(Current.directory,"frozen-scenario.json"),json);File.WriteAllText(Path.Combine(Current.directory,"REPRODUCTION.txt"),ArenaAssertions.Describe(proposal.scenario));Current.scenarioHash=ProjectWorkspace.Hash(json);
                    await ReviewSpecification(before,token);
                    if(!Busy)return;
                    if(!Current.repairAuthorized)
                    {Integrity();Current.evidenceHash=EvidenceHash();Current.workspaceHash=ProjectWorkspace.Hash(ProjectWorkspace.SnapshotText(Current.directory));Current.readyForRepair=true;Finish("Bug reproduced — no repair attempted","The frozen reproduction and separate witness both failed before any source edit. Gameplay and protected source hashes remained unchanged. Choose Attempt repair to continue from this evidence.");return;}
                    await Repair(token);return;
                }
                catch(ArgumentException e){Current.invalidProbes++;feedback="Invalid scenario: "+e.Message;Log(feedback);}
            }
            Finish(Current.validProbes>0?"No failure reproduced in tested cases":"Needs developer review","No failing reproduction established in three probes. "+Current.checksExecuted+" checks executed, "+Current.checksPassed+" passed across "+Current.validProbes+" valid probes; "+Current.invalidProbes+" invalid or execution-error probes. Coverage is limited to these tested cases. No source edit attempted.");
        }
        static async Task ReviewSpecification(ArenaRun before,CancellationToken token)
        {
            Stage("Reviewing specification");string reviewer=Path.Combine(Current.directory,"specification-workspace");Directory.CreateDirectory(reviewer);File.WriteAllText(Path.Combine(reviewer,"TEST_API.md"),ProjectProtocol.Tools);
            // Separate context sees the report, trace and immutable pre-repair source,
            // so it can check authored per-hit values/durations. No candidate fix exists.
            Current.specification=JsonUtility.FromJson<SpecificationReview>(await Ask("You are the independent specification reviewer in a separate context. No repair has been applied. Judge whether the proposed scenario's observable assertions follow from the original developer report and supplied original source evidence. Reject ambiguous intent, tautologies, state manufactured after the action under test, assertions that accept the broken behavior, and tests unrelated to the request. Return clearExpectedBehavior plus rationale, and a nonempty array of report-derived observable checks to use as a separate frozen witness. You may retain sound proposed checks or strengthen them. Numeric expectations are justified when derived from the original per-hit values, initial conditions and formulas below; distinguish per-hit balance from the faulty behavior being investigated. Do not invent a desired balance or certify behavior not supported by the request. These are model-reviewed expectations, not human certification.\nORIGINAL REPORT:\n"+Current.request+"\nPROPOSED SCENARIO:\n"+JsonUtility.ToJson(Current.scenario,true)+"\nOBSERVED BEFORE TRACE:\n"+JsonUtility.ToJson(before,true)+"\nORIGINAL PRE-REPAIR SOURCE EVIDENCE:\n"+ProjectWorkspace.SnapshotText(Current.directory),ProjectProtocol.ReviewSchema,"specification-review",token,Path.GetFullPath(reviewer)));
            var spec=Current.specification;if(spec==null||!spec.clearExpectedBehavior||spec.checks==null||spec.checks.Length==0||spec.checks.Length>60){Current.reviewReason=spec==null?"No review response":spec.rationale;Finish("Needs developer review","An independent model context did not establish a sufficient task-specific expectation.");return;}
            foreach(var check in spec.checks)ArenaAssertions.ValidateCheck(check);Current.reviewReason=spec.rationale;Log("Specification review: "+spec.rationale);
            var witness=new ArenaScenario{title="Independent witness: "+Current.scenario.title,expectedBehavior=Current.scenario.expectedBehavior,player=Current.scenario.player,opponent=Current.scenario.opponent,seed=Current.scenario.seed,actions=Current.scenario.actions,checks=spec.checks};
            string json=JsonUtility.ToJson(witness,true);File.WriteAllText(Path.Combine(Current.directory,"frozen-specification.json"),json);Current.specificationHash=ProjectWorkspace.Hash(json);Save();Integrity();
            var replay=await ArenaScenarioDriver.Run(witness,Current.directory,"before-witness",token);Log(replay.ToString());
            if(!replay.Reproduced){Finish("Needs developer review","The fresh independent witness did not reproduce an assertion failure. No source repair applied.");return;}
            File.WriteAllText(Path.Combine(Current.directory,"workspace","FROZEN_SCENARIO.json"),JsonUtility.ToJson(Current.scenario,true));File.WriteAllText(Path.Combine(Current.directory,"workspace","BEFORE.json"),JsonUtility.ToJson(before,true));
            Current.feedback=before+"\nIndependent witness:\n"+replay;Save();
        }
        static async Task Repair(CancellationToken token)
        {
            if(!Current.repairAuthorized)throw new InvalidOperationException("This run did not authorize source repair.");
            if(Current.attempt>=3){await Fail("Three repair attempts exhausted. No verified solution established.");return;}
            Integrity();Current.attempt++;Stage("Repairing");int attempt=Current.attempt;
            var patch=JsonUtility.FromJson<PatchProposal>(await Ask("Repair the reproduced gameplay issue using the current source supplied below. Propose ordinary, minimal multi-file C# edits as exact find/replace operations using workspace-relative Assets paths. Do not change test/harness infrastructure, infer success, weaken frozen expectations, detect tests, or add special handling for this report. Preserve unrelated behavior. You receive actual failures; no reference implementation is supplied.\nUNTRUSTED REPORT:\n"+Current.request+"\nFROZEN SCENARIO:\n"+JsonUtility.ToJson(Current.scenario,true)+"\nACTUAL FEEDBACK:\n"+Current.feedback+"\nCURRENT WORKING COPY SOURCE:\n"+ProjectWorkspace.SnapshotText(Current.directory),ProjectProtocol.PatchSchema,"repair-"+attempt,token));
            token.ThrowIfCancellationRequested();Integrity();Current.patchExplanation=patch==null?"":patch.analysis;Log(patch==null?"No patch":patch.analysis);
            try
            {
                Current.diff=ProjectWorkspace.Stage(Current.directory,patch);
                if(string.IsNullOrWhiteSpace(Current.diff))throw new ArgumentException("No actual source change proposed.");
                File.WriteAllText(Path.Combine(Current.directory,"diff-"+attempt+".patch"),Current.diff);Log("Staged ordinary source diff:\n"+Current.diff);
            }
            catch(ArgumentException e){Current.feedback="Source proposal rejected before application: "+e.Message;Log(Current.feedback);await Repair(token);return;}
            Current.expected=ProjectWorkspace.Apply(Current.directory,Current.expected);Current.sourceChanged=true;Current.compileStarted=DateTime.UtcNow.Ticks;Current.feedback="";Stage("Compiling");Save();
            resume=false;AssetDatabase.Refresh();CompilationPipeline.RequestScriptCompilation();
        }
        static async void Watch()
        {
            if(!Busy)return;
            if(Elapsed>900&&!Current.rollingBack){Current.cancelRequested=true;cancellation?.Cancel();CodexRunner.Abort();}
            if(working)return;
            if(Current.stage=="Compiling")
            {
                if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                if(resume||EditorUtility.scriptCompilationFailed)
                {
                    resume=false;working=true;cancellation=new CancellationTokenSource();
                    try
                    {
                        if(Current.rollingBack){Current.rollingBack=false;Finish(EditorUtility.scriptCompilationFailed?"Needs developer review":"Unresolved (rolled back)",Current.rollbackReason+(EditorUtility.scriptCompilationFailed?" Original source still reports a compilation error.":" Original source restored and recompiled."));}
                        else if(Current.cancelRequested){await Fail("Cancelled by developer or overall timeout after compilation completed.");}
                        else if(EditorUtility.scriptCompilationFailed){File.WriteAllText(Path.Combine(Current.directory,"compiler-errors-"+Current.attempt+".txt"),Current.feedback);Log("Compilation failed:\n"+Current.feedback);await Repair(cancellation.Token);}
                        else await Verify(cancellation.Token);
                    }
                    catch(OperationCanceledException){if(Busy)await Fail("Cancelled during continuation.");}
                    catch(Exception e){await Fail(e.Message);}
                    finally{working=false;}
                }
                else if((DateTime.UtcNow-new DateTime(Current.compileStarted,DateTimeKind.Utc)).TotalSeconds>120){working=true;try{await Fail("Compilation/domain reload timed out after 120 seconds.");}finally{working=false;}}
            }
            else if(Current.cancelRequested)await Fail("Run cancelled or exceeded fifteen minutes. No success assumed.");
        }
        static async Task Verify(CancellationToken token)
        {
            Integrity();Stage("Verifying");var scenario=JsonUtility.FromJson<ArenaScenario>(File.ReadAllText(Path.Combine(Current.directory,"frozen-scenario.json")));
            var after=await ArenaScenarioDriver.Run(scenario,Current.directory,"after-"+Current.attempt,token);Current.afterSummary=after.ToString();Log(after.ToString());
            var witness=JsonUtility.FromJson<ArenaScenario>(File.ReadAllText(Path.Combine(Current.directory,"frozen-specification.json")));
            var independent=await ArenaScenarioDriver.Run(witness,Current.directory,"after-witness-"+Current.attempt,token);Log(independent.ToString());
            Current.repaired=after.Success&&independent.Success;
            var regressionResult=await ArenaScenarioDriver.Regressions(Current.directory,token);string regressions=regressionResult.text;Current.regressions=regressions;Log(regressions);
            bool protectedPassed=regressionResult.success;
            bool savedPassed=true;
            if(protectedPassed&&Directory.Exists("DexReports/general-regressions"))
                foreach(var path in Directory.GetFiles("DexReports/general-regressions","*.json").OrderBy(p=>p).Take(20))
                {var saved=JsonUtility.FromJson<ArenaScenario>(File.ReadAllText(path));var result=await ArenaScenarioDriver.Run(saved,Current.directory,"saved-"+Path.GetFileNameWithoutExtension(path),token);Log("Saved regression: "+result);savedPassed&=result.Success;}
            Integrity();File.WriteAllText(Path.Combine(Current.directory,"protected-after.sha256"),ProjectWorkspace.ProtectedHash());
            if(Current.repaired&&protectedPassed&&savedPassed)
            {
                Current.verified=true;Directory.CreateDirectory("DexReports/general-regressions");File.WriteAllText("DexReports/general-regressions/"+Path.GetFileName(Current.directory)+".json",File.ReadAllText(Path.Combine(Current.directory,"frozen-specification.json")));
                Finish("Repair verified","The frozen reproduction failed before source editing and passed after compilation. A separate, frozen specification witness failed before and passed in a fresh Play session afterward. Protected core/arena regressions and saved scenarios passed; protected host hashes are unchanged. Expected behavior is model-reviewed against the report, not independently human-certified.");return;
            }
            Current.feedback="After repair:\n"+after+"\nIndependent replay:\n"+independent+"\nProtected regressions:\n"+regressions+"\nSaved regressions passed: "+savedPassed;
            File.WriteAllText(Path.Combine(Current.directory,"workspace","AFTER.json"),JsonUtility.ToJson(after,true));await Repair(token);
        }
        static Task Fail(string reason)
        {
            if(Current==null)return Task.CompletedTask;
            if(Current.sourceChanged)
            {
                try{RollbackInternal(reason);return Task.CompletedTask;}
                catch(Exception e){Finish("Needs developer review",reason+"\nRollback stopped to preserve external edits: "+e.Message);return Task.CompletedTask;}
            }
            Finish("Unresolved",reason);return Task.CompletedTask;
        }
        static void RollbackInternal(string reason)
        {
            if(ArenaScenarioDriver.Busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Wait for the owned replay session to stop before rolling back.");
            Current.expected=ProjectWorkspace.Rollback(Current.directory,Current.expected,Current.baseline);Current.sourceChanged=false;Current.repaired=false;Current.verified=false;Current.rollingBack=true;Current.rollbackReason=reason;Current.running=true;Current.compileStarted=DateTime.UtcNow.Ticks;Stage("Compiling");resume=false;AssetDatabase.Refresh();CompilationPipeline.RequestScriptCompilation();
        }
        public static async void Cancel()
        {
            if(!Busy)return;Current.cancelRequested=true;Save();cancellation?.Cancel();CodexRunner.Abort();
            DateTime deadline=DateTime.UtcNow.AddSeconds(15);while((ArenaScenarioDriver.Busy||working)&&DateTime.UtcNow<deadline)await Task.Delay(50);
            if(Busy&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)await Fail("Cancelled by developer; source backups and actual results retained.");
        }
        public static void Rollback()
        {
            if(Current==null||Busy||CodexRunner.IsBusy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop active work before rollback.");
            if(!Current.sourceChanged)throw new InvalidOperationException("This run has no applied source changes.");
            RollbackInternal("Explicit developer rollback of this run. This restores its original source, which may include the intentionally introduced evaluation bug.");
        }
        public static async void ReplayFrozen()
        {
            if(Current==null||Busy||CodexRunner.IsBusy||EditorApplication.isPlayingOrWillChangePlaymode)return;
            try{var s=JsonUtility.FromJson<ArenaScenario>(File.ReadAllText(Path.Combine(Current.directory,"frozen-scenario.json")));var r=await ArenaScenarioDriver.Run(s,Current.directory,"manual-replay-"+DateTime.UtcNow.ToString("HHmmss"),CancellationToken.None,true);Log("Developer replay of current source:\n"+r);}
            catch(Exception e){Log("Replay unavailable: "+e.Message);}
        }
    }
}
