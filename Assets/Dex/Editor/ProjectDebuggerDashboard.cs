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
        Vector2 projectScroll,projectDetailScroll;
        string projectUiError="";
        public void StartProjectRequest(string report){workbenchMode=0;projectRequest=report;ProjectDebugAgent.Begin(report,codexPath,codexModel);Repaint();}
        void ProjectAction(Action action){try{projectUiError="";action();}catch(Exception e){projectUiError=e.Message;}}
        void ProjectText(string value,float width,float height)
        {
            projectDetailScroll=EditorGUILayout.BeginScrollView(projectDetailScroll,GUILayout.Height(height));
            EditorGUILayout.SelectableLabel(value??"",monoStyle,GUILayout.Width(Mathf.Max(270,width-18)),GUILayout.Height(Mathf.Max(height,monoStyle.CalcHeight(new GUIContent(value??""),Mathf.Max(270,width-18)))));
            EditorGUILayout.EndScrollView();
        }
        void DrawProjectDebugger()
        {
            float width=Mathf.Max(340,position.width-38);var run=ProjectDebugAgent.Current;bool busy=ProjectDebugAgent.Busy;
            projectScroll=EditorGUILayout.BeginScrollView(projectScroll);EditorGUILayout.BeginVertical(GUILayout.Width(width));GUILayout.Space(14);
            GUILayout.Label("dex / project debugger",titleStyle);GUILayout.Label("Describe a gameplay bug. Inspect a real reproduction, source repair and fresh verification.",subStyle);GUILayout.Space(16);
            string result=run==null?"Ready":run.outcome=="Needs developer review"?"Review needed":run.outcome.StartsWith("Unresolved",StringComparison.Ordinal)?"Unresolved":run.outcome!=""?run.outcome:run.stage;
            float card=(width-16)/3;EditorGUILayout.BeginHorizontal();SmallMetric("REPRODUCTION",run!=null&&run.reproduced?"Observed failure":"Not established",card,run!=null&&run.reproduced?Success:TextInk);GUILayout.Space(8);SmallMetric("SOURCE REPAIR",run!=null&&run.repaired?"Checks pass":"Not established",card,run!=null&&run.repaired?Success:TextInk);GUILayout.Space(8);SmallMetric("TASK RESULT",result,card,run!=null&&run.verified?Success:Accent);EditorGUILayout.EndHorizontal();
            GUILayout.Space(12);GUILayout.Label((run==null?"Ready":run.stage)+"   /   "+ProjectDebugAgent.Elapsed.ToString("0.0")+" seconds",sectionStyle);GUILayout.Label("Read-only agent tools • isolated C# working copy • frozen tests • protected regression gates",mutedStyle);GUILayout.Space(16);
            GUILayout.Label("Gameplay report",sectionStyle);GUILayout.Label("Include what happens, what should happen, and any steps you know. No encounter selection is required.",subStyle);
            using(new EditorGUI.DisabledScope(busy||CodexRunner.IsBusy||ArenaScenarioDriver.Busy||Running||Pending||ManualBaseline.Active||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                projectRequest=EditorGUILayout.TextArea(projectRequest,code,GUILayout.MinHeight(100),GUILayout.Width(width));GUILayout.Space(10);
                if(GUILayout.Button("INVESTIGATE → REPRODUCE → REPAIR → VERIFY",primaryStyle,GUILayout.Height(46)))ProjectAction(()=>ProjectDebugAgent.Begin(projectRequest,codexPath,codexModel));
            }
            if(busy&&GUILayout.Button("CANCEL AND RECOVER",secondaryStyle,GUILayout.Height(32)))ProjectDebugAgent.Cancel();
            if(projectUiError!="")EditorGUILayout.HelpBox(projectUiError,MessageType.Warning);
            GUILayout.Space(12);EditorGUILayout.BeginHorizontal();var names=new[]{"Investigation","Reproduction","Source diff","Verification","Activity"};
            for(int i=0;i<names.Length;i++)if(GUILayout.Button(names[i],i==projectTab?tabSelectedStyle:tabStyle,GUILayout.Height(width<550?43:32)))projectTab=i;EditorGUILayout.EndHorizontal();GUILayout.Space(10);
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
            using(new EditorGUI.DisabledScope(run==null||busy||CodexRunner.IsBusy||ArenaScenarioDriver.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("REPLAY FROZEN TEST",secondaryStyle,GUILayout.Height(34)))ProjectAction(ProjectDebugAgent.ReplayFrozen);
                if(GUILayout.Button("OPEN EVIDENCE",secondaryStyle,GUILayout.Height(34)))ProjectAction(()=>EditorUtility.RevealInFinder(Path.GetFullPath(run.directory)));
                EditorGUILayout.EndHorizontal();GUILayout.Space(6);
                if(GUILayout.Button("ROLL BACK THIS RUN'S SOURCE CHANGES",secondaryStyle,GUILayout.Height(30)))ProjectAction(ProjectDebugAgent.Rollback);
                GUILayout.Label("Rollback restores this run's original code, including an evaluation bug if one was present.",mutedStyle);
            }
            GUILayout.Space(12);GUILayout.Label("Connection",sectionStyle);
            using(new EditorGUI.DisabledScope(busy||CodexRunner.IsBusy))
            {if(string.IsNullOrEmpty(codexPath))codexPath=CodexRunner.FindExecutable();codexPath=EditorGUILayout.TextField("Codex native .exe",codexPath);codexModel=EditorGUILayout.TextField("Model (default if blank)",codexModel);}
            GUILayout.Label("Uses saved ChatGPT sign-in. The earlier encounter demo is preserved under Guided Fixtures.",mutedStyle);GUILayout.Space(15);EditorGUILayout.EndVertical();EditorGUILayout.EndScrollView();
            if(busy)Repaint();
        }
    }
}
