using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dex.Editor
{
    public partial class AgentWorkbench
    {
        [SerializeField] string projectRequest="";
        [SerializeField] int projectTab;
        [SerializeField] bool projectDetails;
        [SerializeField] bool projectRepairMode;
        Vector2 projectScroll,projectDetailScroll;
        string projectUiError="";
        public void StartProjectRequest(string report,bool repairRequested=true){workbenchMode=0;projectRequest=report;projectRepairMode=repairRequested;ProjectDebugAgent.Begin(report,codexPath,codexModel,repairRequested);Repaint();}
        void ProjectAction(Action action){try{projectUiError="";action();}catch(Exception e){projectUiError=e.Message;}}
        void ProjectText(string value,float width,float height)
        {
            projectDetailScroll=EditorGUILayout.BeginScrollView(projectDetailScroll,GUILayout.Height(height));
            EditorGUILayout.SelectableLabel(value??"",monoStyle,GUILayout.Width(Mathf.Max(270,width-18)),GUILayout.Height(Mathf.Max(height,monoStyle.CalcHeight(new GUIContent(value??""),Mathf.Max(270,width-18)))));
            EditorGUILayout.EndScrollView();
        }
        void DrawProjectDebugger()
        {
            float width=Mathf.Max(280,position.width-38);var run=ProjectDebugAgent.Current;bool busy=ProjectDebugAgent.Busy;
            projectScroll=EditorGUILayout.BeginScrollView(projectScroll);EditorGUILayout.BeginVertical(GUILayout.Width(width));GUILayout.Space(14);
            GUILayout.Label("dex / project debugger",titleStyle);GUILayout.Label("Investigate gameplay first. Repair is a separate choice.",subStyle);GUILayout.Space(16);
            GUILayout.Label("CODEX CLI  /  "+(run==null?"Ready":run.stage)+"  /  "+ProjectDebugAgent.Elapsed.ToString("0.0")+"s",mutedStyle);GUILayout.Space(13);
            GUILayout.Label("Gameplay report",sectionStyle);GUILayout.Label("Include what happens, what should happen, and any steps you know. No encounter selection is required.",subStyle);
            using(new EditorGUI.DisabledScope(busy||CodexRunner.IsBusy||ArenaScenarioDriver.Busy||Running||Pending||ManualBaseline.Active||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                GUILayout.Label("Run mode",sectionStyle);
                projectRepairMode=GUILayout.Toolbar(projectRepairMode?1:0,new[]{"INVESTIGATE & TEST","INVESTIGATE & REPAIR"},tabStyle,GUILayout.Height(36))==1;
                GUILayout.Label(projectRepairMode?"Investigate, reproduce, review expectations, then repair only if the bug is established.":"Inspect and run scenarios. Save findings without changing source. A repair can be authorized afterward.",mutedStyle);
                GUILayout.Space(8);
                projectRequest=EditorGUILayout.TextArea(projectRequest,code,GUILayout.MinHeight(90),GUILayout.Width(width));GUILayout.Space(10);
                if(GUILayout.Button(projectRepairMode?"INVESTIGATE & REPAIR":"INVESTIGATE & TEST",primaryStyle,GUILayout.Height(46)))ProjectAction(()=>ProjectDebugAgent.Begin(projectRequest,codexPath,codexModel,projectRepairMode));
            }
            if(busy&&GUILayout.Button("CANCEL RUN",secondaryStyle,GUILayout.Height(32)))ProjectDebugAgent.Cancel();
            if(projectUiError!="")EditorGUILayout.HelpBox(projectUiError,MessageType.Warning);
            GUILayout.Space(8);
            DrawProjectProgress(run,width);
            if(run!=null)DrawProjectSummary(run);
            if(run!=null&&run.readyForRepair&&!busy)
            {using(new EditorGUI.DisabledScope(CodexRunner.IsBusy||ArenaScenarioDriver.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
                if(GUILayout.Button("ATTEMPT REPAIR USING THIS EVIDENCE",primaryStyle,GUILayout.Height(42)))ProjectAction(ProjectDebugAgent.AttemptRepair);}
            projectDetails=EditorGUILayout.Foldout(projectDetails,"Evidence and advanced details",true);
            if(projectDetails)
            {
            GUILayout.Space(12);var names=new[]{"Investigation","Reproduction","Source diff","Verification","Activity"};
            for(int row=0;row<(width<570?3:1);row++){EditorGUILayout.BeginHorizontal();for(int col=0;col<(width<570?2:5);col++){int i=row*(width<570?2:5)+col;if(i<names.Length&&GUILayout.Button(names[i],i==projectTab?tabSelectedStyle:tabStyle,GUILayout.Height(32)))projectTab=i;}EditorGUILayout.EndHorizontal();}GUILayout.Space(10);
            CardStart();
            if(run==null){GUILayout.Label("A project-aware investigation",sectionStyle);GUILayout.Label("The agent can read gameplay source, scene/package information, current Console context and test results. It composes new action scenarios, then proposes ordinary multi-file C# changes. The host runs and checks them. Ambiguous expectations or insufficient proof produce a review/unresolved result.",bodyStyle);}
            else if(projectTab==0)
            {
                GUILayout.Label("Investigation",sectionStyle);GUILayout.Label(run.analysis==""?"Waiting for project investigation.":run.analysis,bodyStyle);GUILayout.Space(10);
                GUILayout.Label("Specification witness",sectionStyle);GUILayout.Label(run.reviewReason==""?"A separate context will review the report-derived observable expectations before source editing.":run.reviewReason,bodyStyle);
                GUILayout.Space(9);GUILayout.Label("Expected behavior is model-reviewed, not human-certified. Verification applies to the recorded task and regressions.",mutedStyle);
            }
            else if(projectTab==1)
            {
                GUILayout.Label("Frozen reproduction",sectionStyle);
                if(run.scenario!=null)
                {
                    GUILayout.Label(run.scenario.title+" / seed "+run.scenario.seed,bodyStyle);GUILayout.Label(run.scenario.expectedBehavior,bodyStyle);GUILayout.Space(8);
                    ProjectText(ArenaAssertions.Describe(run.scenario)+"\n\nBEFORE\n"+run.beforeSummary+"\n\nAFTER\n"+run.afterSummary,width-32,320);
                }
                else GUILayout.Label("The host will execute the agent's proposed actions before accepting a repair.",bodyStyle);
            }
            else if(projectTab==2)
            {
                GUILayout.Label("Actual multi-file source diff",sectionStyle);
                if(run.diff=="")GUILayout.Label("No source patch applied.",bodyStyle);
                else
                {
                    projectDetailScroll=EditorGUILayout.BeginScrollView(projectDetailScroll,GUILayout.Height(340));
                    foreach(var line in run.diff.Replace("\r","").Split('\n'))GUILayout.Label(line,line.StartsWith("+ ",StringComparison.Ordinal)?diffAdded:line.StartsWith("- ",StringComparison.Ordinal)?diffRemoved:diffHeader,GUILayout.Width(Mathf.Max(270,width-50)));
                    EditorGUILayout.EndScrollView();
                    GUILayout.Label("Originals, working copy, patches and file hashes are saved in the run folder.",mutedStyle);
                }
            }
            else if(projectTab==3)
            {
                GUILayout.Label("Task-specific evidence and protected regressions",sectionStyle);
                ProjectText("OUTCOME: "+run.outcome+"\n\nBEFORE\n"+run.beforeSummary+"\n\nAFTER\n"+run.afterSummary+"\n\nPROTECTED REGRESSIONS\n"+run.regressions+"\n\nCURRENT FEEDBACK\n"+run.feedback,width-32,360);
            }
            else ProjectText(run.log,width-32,380);
            EditorGUILayout.EndVertical();GUILayout.Space(10);
            }
            using(new EditorGUI.DisabledScope(run==null||busy||CodexRunner.IsBusy||ArenaScenarioDriver.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(run==null||!File.Exists(Path.Combine(run==null?"":run.directory,"frozen-scenario.json"))))
                    if(GUILayout.Button("REPLAY TEST",secondaryStyle,GUILayout.Height(34)))ProjectAction(ProjectDebugAgent.ReplayFrozen);
                using(new EditorGUI.DisabledScope(run==null||string.IsNullOrEmpty(run.diff)))
                    if(GUILayout.Button("VIEW CODE CHANGES",secondaryStyle,GUILayout.Height(34))){projectDetails=true;projectTab=2;}
                EditorGUILayout.EndHorizontal();GUILayout.Space(6);
                if(GUILayout.Button("OPEN EVIDENCE",secondaryStyle,GUILayout.Height(30)))ProjectAction(()=>EditorUtility.RevealInFinder(Path.GetFullPath(run.directory)));
                if(projectDetails&&run!=null&&run.sourceChanged&&GUILayout.Button("ROLL BACK THIS RUN'S SOURCE CHANGES",secondaryStyle,GUILayout.Height(30))&&EditorUtility.DisplayDialog("Roll back source repair?","Restore the gameplay source from before this run? This also restores an intentionally injected bug if one was present. Run evidence remains saved.","Roll back","Keep source"))ProjectAction(ProjectDebugAgent.Rollback);
            }
            if(projectDetails){GUILayout.Space(12);GUILayout.Label("Connection and developer settings",sectionStyle);
            using(new EditorGUI.DisabledScope(busy||CodexRunner.IsBusy))
            {if(string.IsNullOrEmpty(codexPath))codexPath=CodexRunner.FindExecutable();codexPath=EditorGUILayout.TextField("Codex native .exe",codexPath);codexModel=EditorGUILayout.TextField("Model (default if blank)",codexModel);}
            GUILayout.Label("Uses saved ChatGPT sign-in. Guided Fixtures remain available as a separate demo.",mutedStyle);}
            GUILayout.Space(15);EditorGUILayout.EndVertical();EditorGUILayout.EndScrollView();
            if(busy)Repaint();
        }
        void DrawProjectProgress(ProjectDebugRun run,float width)
        {
            string[] phases={"Investigate","Reproduce","Repair","Verify"};int active=run==null?-1:run.stage=="Investigating"?0:run.stage=="Reproducing"||run.stage=="Reproduced"||run.stage=="Reviewing specification"?1:run.stage=="Repairing"||run.stage=="Compiling"?2:run.stage=="Verifying"||run.verified?3:-1;
            int columns=width<490?2:4;
            for(int row=0;row<4/columns;row++){EditorGUILayout.BeginHorizontal();for(int col=0;col<columns;col++){int i=row*columns+col;GUILayout.Label(phases[i],i<active?phaseDone:i==active?phaseNow:phaseIdle,GUILayout.ExpandWidth(true));}EditorGUILayout.EndHorizontal();}
        }
        void DrawProjectSummary(ProjectDebugRun run)
        {
            CardStart();string status=ResultTitle(run);
            GUILayout.Label(status,run.verified?metricGood:run.running?metricLive:metricBad);
            GUILayout.Label("Mode: "+(run.mode=="test"?"Investigate & test":"Investigate & repair")+(run.repairAuthorized&&run.mode=="test"?" / repair later authorized":""),mutedStyle);
            GUILayout.Label(run.request,bodyStyle);GUILayout.Space(5);
            GUILayout.Label("Before  /  "+(run.reproduced?FirstLine(run.beforeSummary):"No failing reproduction established."),bodyStyle);
            GUILayout.Label("Code  /  "+(string.IsNullOrEmpty(run.diff)?"No source change applied.":string.IsNullOrEmpty(run.patchExplanation)?"Source changed; open the recorded diff.":run.patchExplanation),bodyStyle);
            GUILayout.Label("After  /  "+(run.verified?"Fresh replay, specification witness, and protected regressions passed.":run.readyForRepair?"Frozen witness reproduced the bug; no repair was attempted.":string.IsNullOrEmpty(run.afterSummary)?"Repair verification has not completed.":FirstLine(run.afterSummary)),bodyStyle);
            if(run.outcome=="No failure reproduced in tested cases")GUILayout.Label("Tested "+run.checksExecuted+" checks across "+run.validProbes+" valid probes; "+run.invalidProbes+" invalid or error probes.",mutedStyle);
            if(!run.running&&!run.verified)GUILayout.Label(string.IsNullOrEmpty(run.completionReason)?run.outcome:run.completionReason,mutedStyle);
            EditorGUILayout.EndVertical();GUILayout.Space(8);
        }
        static string FirstLine(string value){return string.IsNullOrEmpty(value)?"":value.Replace("\r","").Split('\n')[0];}
        static string ResultTitle(ProjectDebugRun run)
        {
            if(run.verified)return "REPAIR VERIFIED";if(run.running)return run.stage.ToUpperInvariant();
            if(run.outcome=="Bug reproduced — no repair attempted")return "BUG REPRODUCED / NO REPAIR";
            if(run.outcome=="No failure reproduced in tested cases")return "NO FAILURE REPRODUCED";
            if(run.stage=="Interrupted")return "INTERRUPTED / REVIEW REQUIRED";
            if(run.outcome=="Needs developer review")return "NEEDS DEVELOPER REVIEW";
            string reason=run.completionReason??"";if(reason.IndexOf("cancel",StringComparison.OrdinalIgnoreCase)>=0)return "STOPPED / CANCELLED";
            if(run.stage=="Interrupted"||reason.IndexOf("interrupted",StringComparison.OrdinalIgnoreCase)>=0)return "INTERRUPTED";
            if(run.outcome.StartsWith("Unresolved",StringComparison.Ordinal))return "FAILED / UNRESOLVED";
            return string.IsNullOrEmpty(run.outcome)?"STOPPED":run.outcome.ToUpperInvariant();
        }
    }
}
