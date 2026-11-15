using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex;
using Dex.Editor;
using UnityEditor;

public static class QualityPass
{
    public static string State(){return UnityEditor.SessionState.GetString("Dex.Agent.stage","")+" | "+UnityEditor.SessionState.GetString("Dex.Agent.summary","")+" | running="+UnityEditor.SessionState.GetBool("Dex.Agent.running",false)+" | "+UnityEditor.SessionState.GetString("Dex.Agent.directory","");}
    public static string RootDemo(){var window=EditorWindow.GetWindow<AgentWorkbench>();window.SelectCase("root-blink");window.RunGuidedDemo();return "Started labeled root fixture -> automatic live Codex repair.";}
    public static string MelDemo(){var window=EditorWindow.GetWindow<AgentWorkbench>();window.SelectCase("mel-nova");window.RunGuidedDemo();return "Started labeled Mel nova fixture -> automatic live Codex repair.";}
    public static string Core()
    {
        Directory.CreateDirectory("DexReports/evidence/v2");var report=Verification.Evaluate();
        File.WriteAllText("DexReports/evidence/v2/current-oracle.txt",Verification.Visible()+"\n"+report);
        File.WriteAllText("DexReports/evidence/v2/exploration.txt",ScenarioLab.Explore());return report.ToString().Split('\n')[0];
    }
    public static async Task<string> AllReplays()
    {
        Directory.CreateDirectory("DexReports/evidence/v2/replays");int passed=0;var result="";
        foreach(var item in DemoCases.All)
        {
            var proof=await ReplayAutomation.Run(item.id,item.id,"DexReports/evidence/v2/replays",CancellationToken.None);
            if(proof.passed)passed++;result+=item.id+": "+proof.passed+"\n"+proof.result+"\n\n";
        }
        File.WriteAllText("DexReports/evidence/v2/replays/results.txt",result);return passed+"/"+DemoCases.All.Length+" actual arena replays passed.\n"+result;
    }
    public static string MutateNovaTrue()
    {
        const string target="Assets/Dex/Runtime/CombatRules.cs";Directory.CreateDirectory("DexReports/evidence/v2");string source=File.ReadAllText(target);
        File.WriteAllText("DexReports/evidence/v2/pre-mutation.source.txt",source);
        File.WriteAllText(target,RuleSource.Patch(source,"!stunned && !rooted && !charmed && !grounded","magicShield","true"));
        AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);return "Applied actual always-block Nova mutant. Await compilation, then capture its rejection.";
    }
    public static string CaptureMutant()
    {var report=Verification.Evaluate();File.WriteAllText("DexReports/evidence/v2/always-block-mutation.txt",report.ToString());return report.ToString().Split('\n')[0];}
    public static string CaptureBrokenNova()
    {var report=Verification.Evaluate();File.WriteAllText("DexReports/evidence/v2/never-block-mutation.txt",report.ToString());return report.ToString().Split('\n')[0];}
    public static string Recent(){string text=SessionState.GetString("Dex.Agent.report","");return text.Substring(Math.Max(0,text.Length-2000));}
    public static string AutomationState(){return State()+" | cli="+CodexRunner.IsBusy+" replay="+ReplayAutomation.Busy+" manual="+ManualBaseline.Active+" compiling="+EditorApplication.isCompiling+" compilerFailed="+EditorUtility.scriptCompilationFailed+" queued="+SessionState.GetBool("Dex.Agent.beginAfterFixture",false);}
}
