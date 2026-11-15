using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dex.Editor
{
    public partial class AgentWorkbench
    {
        [SerializeField] int dashboardTab;[SerializeField] bool showConnection;Vector2 detailScroll;
        [SerializeField] int workbenchMode;
        GUIStyle titleStyle,subStyle,sectionStyle,bodyStyle,mutedStyle,primaryStyle,secondaryStyle,caseStyle,caseSelectedStyle,metricStyle,monoStyle,tabStyle,tabSelectedStyle;
        Texture2D[] skinTextures;
        bool skinWasPlaying;
        GUIStyle panelStyle,metricNormal,metricGood,metricBad,metricLive,metricTime,phaseDone,phaseNow,phaseIdle,phaseBad,diffAdded,diffRemoved,diffHeader;
        static readonly Color Bg=new Color(.025f,.04f,.065f),Card=new Color(.045f,.07f,.105f),TextInk=new Color(.88f,.94f,.98f),Secondary=new Color(.5f,.64f,.74f),Accent=new Color(.3f,.86f,.85f),Success=new Color(.37f,.86f,.65f),Danger=new Color(.98f,.43f,.52f),Warm=new Color(.93f,.75f,.43f);
        Texture2D Flat(Color c){var t=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};t.SetPixel(0,0,c);t.Apply();return t;}
        GUIStyle LabelStyle(int size,Color color,bool bold=false){var s=new GUIStyle(EditorStyles.label){fontSize=size,wordWrap=true,fontStyle=bold?FontStyle.Bold:FontStyle.Normal};s.normal.textColor=color;return s;}
        void PrepareDashboard()
        {
            if(titleStyle!=null&&skinTextures!=null&&skinTextures.Length>0&&skinTextures[0]!=null&&skinWasPlaying==EditorApplication.isPlaying)return;
            if(skinTextures!=null)foreach(var texture in skinTextures)if(texture!=null)DestroyImmediate(texture);
            skinWasPlaying=EditorApplication.isPlaying;
            skinTextures=new[]{Flat(Card),Flat(new Color(.075f,.13f,.18f)),Flat(Accent),Flat(new Color(.1f,.25f,.28f)),Flat(new Color(.09f,.16f,.19f))};
            titleStyle=LabelStyle(31,TextInk,true);subStyle=LabelStyle(12,Secondary);sectionStyle=LabelStyle(15,TextInk,true);bodyStyle=LabelStyle(13,TextInk);mutedStyle=LabelStyle(11,Secondary);metricStyle=LabelStyle(23,TextInk,true);
            secondaryStyle=new GUIStyle(GUI.skin.button){fontSize=12,fontStyle=FontStyle.Bold,padding=new RectOffset(12,12,8,8),wordWrap=true};secondaryStyle.normal.background=skinTextures[1];secondaryStyle.normal.textColor=TextInk;secondaryStyle.hover.background=skinTextures[3];secondaryStyle.hover.textColor=Accent;secondaryStyle.active.background=skinTextures[3];secondaryStyle.active.textColor=Warm;
            primaryStyle=new GUIStyle(secondaryStyle){fontSize=13};primaryStyle.normal.background=skinTextures[2];primaryStyle.normal.textColor=Bg;primaryStyle.hover.background=skinTextures[3];primaryStyle.hover.textColor=TextInk;
            caseStyle=new GUIStyle(secondaryStyle){alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,10,10),fontSize=12};caseStyle.normal.background=skinTextures[0];caseSelectedStyle=new GUIStyle(caseStyle);caseSelectedStyle.normal.background=skinTextures[4];caseSelectedStyle.normal.textColor=Accent;
            tabStyle=new GUIStyle(secondaryStyle){fontSize=11,padding=new RectOffset(6,6,7,7)};tabStyle.normal.background=skinTextures[0];tabSelectedStyle=new GUIStyle(tabStyle);tabSelectedStyle.normal.textColor=Accent;tabSelectedStyle.normal.background=skinTextures[3];
            code=new GUIStyle(EditorStyles.textArea){wordWrap=true,fontSize=13,padding=new RectOffset(12,12,10,10)};code.normal.background=skinTextures[0];code.normal.textColor=TextInk;
            monoStyle=new GUIStyle(bodyStyle){fontSize=12,wordWrap=true};
            panelStyle=new GUIStyle{normal={background=skinTextures[0]},padding=new RectOffset(16,16,13,13)};
            metricNormal=new GUIStyle(metricStyle);metricGood=new GUIStyle(metricStyle);metricGood.normal.textColor=Success;metricBad=new GUIStyle(metricStyle);metricBad.normal.textColor=Danger;metricLive=new GUIStyle(metricStyle);metricLive.normal.textColor=Accent;metricTime=new GUIStyle(metricStyle);metricTime.normal.textColor=Warm;
            phaseDone=new GUIStyle(mutedStyle){alignment=TextAnchor.MiddleCenter};phaseDone.normal.textColor=Success;phaseNow=new GUIStyle(phaseDone);phaseNow.normal.textColor=Accent;phaseIdle=new GUIStyle(phaseDone);phaseIdle.normal.textColor=Secondary;phaseBad=new GUIStyle(phaseDone);phaseBad.normal.textColor=Danger;
            diffAdded=new GUIStyle(monoStyle);diffAdded.normal.textColor=Success;diffRemoved=new GUIStyle(monoStyle);diffRemoved.normal.textColor=Danger;diffHeader=new GUIStyle(monoStyle);diffHeader.normal.textColor=Secondary;
        }
        void CardStart(float width=0){if(width>0)EditorGUILayout.BeginVertical(panelStyle,GUILayout.Width(width));else EditorGUILayout.BeginVertical(panelStyle);}
        void SmallMetric(string label,string value,float width,Color color)
        {CardStart(width);GUILayout.Label(label,mutedStyle);var style=color==Success?metricGood:color==Danger?metricBad:color==Accent?metricLive:color==Warm?metricTime:metricNormal;style.fontSize=width<150?16:23;GUILayout.Label(value,style,GUILayout.Height(width<150?38:30));EditorGUILayout.EndVertical();}
        void OnGUI()
        {
            PrepareDashboard();EditorGUI.DrawRect(new Rect(0,0,position.width,position.height),Bg);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("PROJECT DEBUGGER",workbenchMode==0?tabSelectedStyle:tabStyle,GUILayout.Height(30)))workbenchMode=0;
            if(GUILayout.Button("GUIDED FIXTURES",workbenchMode==1?tabSelectedStyle:tabStyle,GUILayout.Height(30)))workbenchMode=1;
            EditorGUILayout.EndHorizontal();
            if(workbenchMode==0){DrawProjectDebugger();return;}
            float width=Mathf.Max(340,position.width-38);scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.BeginVertical(GUILayout.Width(width));GUILayout.Space(13);
            EditorGUILayout.BeginHorizontal();GUILayout.Label("dex",titleStyle,GUILayout.Width(70));EditorGUILayout.BeginVertical();GUILayout.Space(7);GUILayout.Label("ABILITY REGRESSION WORKBENCH",sectionStyle);GUILayout.Label("A bug report becomes a compiled change with independent evidence.",subStyle);EditorGUILayout.EndVertical();EditorGUILayout.EndHorizontal();GUILayout.Space(16);
            string stage=Get("stage","Ready");Color state=stage=="Passed"?Success:stage=="Failed"?Danger:Accent;float metricWidth=(width-16)/3;
            EditorGUILayout.BeginHorizontal();SmallMetric("RUN STATUS",stage,metricWidth,state);GUILayout.Space(8);SmallMetric("INDEPENDENT GATE",Get("summary","").Contains(" / ")?Get("summary").Split(new[]{" independent"},StringSplitOptions.None)[0]:"Awaiting run",metricWidth,TextInk);GUILayout.Space(8);SmallMetric("WALL TIME",(Running?Elapsed.ToString("0.0"):Get("elapsed","0"))+" s",metricWidth,Warm);EditorGUILayout.EndHorizontal();GUILayout.Space(12);
            DrawStageStrip(width,stage);GUILayout.Space(17);
            GUILayout.Label("Choose an interaction",sectionStyle);GUILayout.Label("Start with a known fixture, or write your own report within the supported rule families.",subStyle);GUILayout.Space(9);
            using(new EditorGUI.DisabledScope(Running||Pending||ManualBaseline.Active||ReplayAutomation.Busy||CodexRunner.IsBusy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode))
            {
                int columns=width>780?4:width>500?2:1;float cardWidth=(width-(columns-1)*8)/columns;
                for(int row=0;row<Mathf.CeilToInt(DemoCases.All.Length/(float)columns);row++)
                {
                    EditorGUILayout.BeginHorizontal();for(int col=0;col<columns;col++){int n=row*columns+col;if(n>=DemoCases.All.Length)break;var item=DemoCases.All[n];if(col>0)GUILayout.Space(8);if(GUILayout.Button(item.title+"\n"+item.opponent+"  →  "+item.player,n==selectedCase?caseSelectedStyle:caseStyle,GUILayout.Width(cardWidth),GUILayout.Height(68)))SelectCase(item.id);}EditorGUILayout.EndHorizontal();GUILayout.Space(8);
                }
                GUILayout.Space(5);GUILayout.Label("Engineering request",sectionStyle);request=EditorGUILayout.TextArea(request,code,GUILayout.Width(width),GUILayout.MinHeight(72));GUILayout.Space(12);
                EditorGUILayout.BeginHorizontal();if(GUILayout.Button("RUN AUTOMATED REPAIR",primaryStyle,GUILayout.Height(44)))BeginCodex();GUILayout.Space(8);if(GUILayout.Button("DEMO: INJECT + RUN",secondaryStyle,GUILayout.Width(width*.34f),GUILayout.Height(44)))RunGuidedDemo();EditorGUILayout.EndHorizontal();
                GUILayout.Label("Demo injects a labeled regression first. Automatic repair captures before/after arena evidence.",mutedStyle);GUILayout.Space(7);
                autoEvidence=EditorGUILayout.ToggleLeft("Automatically reproduce, capture and replay the verified result",autoEvidence,bodyStyle);
            }
            if(Running&&GUILayout.Button("CANCEL RUN",secondaryStyle,GUILayout.Height(32))){cancellation?.Cancel();CodexRunner.Abort();Finish(false,"Cancelled by developer. Source snapshots retained for review.");}
            GUILayout.Space(13);EditorGUILayout.BeginHorizontal();string[] tabs={"Overview","Source diff","Verification","Activity"};for(int n=0;n<tabs.Length;n++)if(GUILayout.Button(tabs[n],n==dashboardTab?tabSelectedStyle:tabStyle,GUILayout.Height(31)))dashboardTab=n;EditorGUILayout.EndHorizontal();GUILayout.Space(10);
            CardStart();
            if(dashboardTab==0)DrawOverview(width-32);
            else if(dashboardTab==1)DrawDiff(width-32);
            else if(dashboardTab==2){GUILayout.Label("Independent compiled assertions",sectionStyle);DrawScrollable(Get("verdict","Run the protected checks to see individual assertions. The agent cannot edit this verifier."),width-32,290);}
            else{GUILayout.Label("Actual run activity",sectionStyle);DrawScrollable(Get("report","No run yet. Your signed-in Codex CLI is used when you start a repair."),width-32,310);}
            EditorGUILayout.EndVertical();GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(Running||Pending||ReplayAutomation.Busy||EditorApplication.isCompiling)){if(GUILayout.Button("REPLAY IN ARENA",secondaryStyle,GUILayout.Height(32)))ReplayInArena();}if(GUILayout.Button("OPEN RUN EVIDENCE",secondaryStyle,GUILayout.Height(32))){string directory=Get("directory","DexReports");Directory.CreateDirectory(directory);EditorUtility.RevealInFinder(Path.GetFullPath(directory));}EditorGUILayout.EndHorizontal();
            GUILayout.Space(12);if(GUILayout.Button((showConnection?"HIDE":"SHOW")+" CONNECTION & DEVELOPER TOOLS",secondaryStyle,GUILayout.Height(30)))showConnection=!showConnection;
            if(showConnection)DrawConnection(width);
            GUILayout.Space(14);GUILayout.Label("SIGNED-IN CODEX  /  READ-ONLY AGENT TOOLS  /  PROTECTED VERIFIER",mutedStyle);GUILayout.Label("Simplified kits. Recorded elapsed time is not a claim of measured labor savings.",mutedStyle);GUILayout.Space(14);EditorGUILayout.EndVertical();EditorGUILayout.EndScrollView();
            if(Running)Repaint();
        }
        void DrawStageStrip(float width,string stage)
        {
            string[] names={"Investigate","Reproduce","Repair","Compile","Verify","Replay","Passed"};int active=stage=="Investigating"?0:stage=="Reproduced"||stage=="Capturing before"?1:stage=="Repairing"?2:stage=="Compiling"?3:stage=="Verifying"?4:stage=="Replaying"?5:stage=="Passed"||stage=="Failed"?6:-1;
            int columns=width<560?4:7;
            for(int row=0;row<Mathf.CeilToInt(names.Length/(float)columns);row++)
            {EditorGUILayout.BeginHorizontal();for(int col=0;col<columns;col++){int n=row*columns+col;if(n>=names.Length)break;var rect=GUILayoutUtility.GetRect((width-6*(columns-1))/columns,30,GUILayout.ExpandWidth(false));EditorGUI.DrawRect(rect,n==active?new Color(.08f,.2f,.23f):Card);var style=n<active?phaseDone:n==active?stage=="Failed"?phaseBad:phaseNow:phaseIdle;GUI.Label(rect,(n+1)+"  "+(n==6&&stage=="Failed"?"Failed":names[n]),style);if(col<columns-1)GUILayout.Space(6);}EditorGUILayout.EndHorizontal();GUILayout.Space(4);}
        }
        void DrawOverview(float width)
        {
            var item=DemoCases.Find(Get("caseId",DemoCases.All[Mathf.Clamp(selectedCase,0,3)].id));GUILayout.Label(item.title,sectionStyle);GUILayout.Label(item.expected,bodyStyle);GUILayout.Space(10);
            string before=Get("beforeReplay"),after=Get("afterReplay");
            if(!string.IsNullOrEmpty(before)){var proof=JsonUtility.FromJson<ReplayProof>(before);GUILayout.Label("BEFORE  /  "+(proof.passed?"No failure observed":"Failure observed"),proof.passed?mutedStyle:sectionStyle);GUILayout.Label(proof.result,bodyStyle);GUILayout.Space(9);}
            if(!string.IsNullOrEmpty(after)){var proof=JsonUtility.FromJson<ReplayProof>(after);GUILayout.Label("AFTER  /  "+(proof.passed?"Arena replay passed":"Arena replay failed"),sectionStyle);GUILayout.Label(proof.result,bodyStyle);GUILayout.Space(9);}
            if(before==""&&after=="")GUILayout.Label("The agent selects reproductions. The host executes them, explores timing/matchup combinations, reduces a failing sequence, compiles a constrained repair, then checks the result independently.",bodyStyle);
            if(Get("exploration")!=""){GUILayout.Space(5);GUILayout.Label("Generated investigation",sectionStyle);DrawScrollable(Get("exploration"),width,116);}
            if(Get("diff")!=""){GUILayout.Space(10);DrawDiff(width);}
        }
        void DrawDiff(float width)
        {
            GUILayout.Label("Actual source change",sectionStyle);GUILayout.Label("CombatRules.cs  /  Only allowlisted expressions can be replaced",mutedStyle);GUILayout.Space(7);
            string diff=Get("diff");if(diff==""){GUILayout.Label("A real applied source diff will appear here.",bodyStyle);return;}
            foreach(string line in diff.Replace("\r","").Split('\n'))
            {if(line=="")continue;var style=line.StartsWith("+ ",StringComparison.Ordinal)?diffAdded:line.StartsWith("- ",StringComparison.Ordinal)?diffRemoved:diffHeader;GUILayout.Label(line,style,GUILayout.Width(Mathf.Max(280,width)));}
        }
        void DrawScrollable(string text,float width,float height)
        {detailScroll=EditorGUILayout.BeginScrollView(detailScroll,GUILayout.Height(height));EditorGUILayout.SelectableLabel(text,monoStyle,GUILayout.Width(Mathf.Max(280,width-16)),GUILayout.Height(Mathf.Max(height,monoStyle.CalcHeight(new GUIContent(text),Mathf.Max(280,width-16)))));EditorGUILayout.EndScrollView();}
        void DrawConnection(float width)
        {
            using(new EditorGUI.DisabledScope(Running||Pending||CodexRunner.IsBusy||ReplayAutomation.Busy||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling))
            {
                if(string.IsNullOrEmpty(codexPath))codexPath=CodexRunner.FindExecutable();codexPath=EditorGUILayout.TextField("Codex native .exe",codexPath);codexModel=EditorGUILayout.TextField("Model (blank = default)",codexModel);
                if(GUILayout.Button("CHECK SIGN-IN",secondaryStyle,GUILayout.Height(30)))CheckCodex();GUILayout.Label(Get("cliStatus","Uses your saved ChatGPT sign-in; no separate API key."),bodyStyle);
                EditorGUILayout.BeginHorizontal();if(GUILayout.Button("RUN PROTECTED CHECKS",secondaryStyle,GUILayout.Height(30)))Checks();if(GUILayout.Button("RESET SELECTED FIXTURE",secondaryStyle,GUILayout.Height(30)))FixtureFor(DemoCases.All[Mathf.Clamp(selectedCase,0,3)].id);EditorGUILayout.EndHorizontal();
                if(GUILayout.Button("APPLY OFFLINE REFERENCE REPAIR (NOT AI)",secondaryStyle,GUILayout.Height(30)))ReferenceRepair();
                GUILayout.Label("The reference button is a deterministic developer action. Subscription usage limits still apply to Codex runs.",mutedStyle);
                GUILayout.Space(9);GUILayout.Label("Measure a real manual baseline",sectionStyle);GUILayout.Label(ManualBaseline.Status,bodyStyle);
                if(GUILayout.Button(ManualBaseline.Active?"STOP HUMAN TIMER & VERIFY":"START HUMAN BASELINE TIMER",secondaryStyle,GUILayout.Height(30))){if(ManualBaseline.Active)ManualBaseline.StopAndVerify();else ManualBaseline.Start(DemoCases.All[Mathf.Clamp(selectedCase,0,3)].id);}
            }
        }
        void OnDisable(){if(skinTextures!=null)foreach(var texture in skinTextures)if(texture!=null)DestroyImmediate(texture);titleStyle=null;}
    }
}
