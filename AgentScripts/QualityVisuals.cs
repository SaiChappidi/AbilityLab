using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Dex;
using Dex.Editor;

// Presentation setup only. These captures are not reported as agent or verifier runs.
public static class QualityVisuals
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n){return o.GetType().GetField(n,Flags).GetValue(o);}
    static void Set(object o,string n,object v){o.GetType().GetField(n,Flags).SetValue(o,v);}
    static object Call(object o,string n,params object[] args){return o.GetType().GetMethod(n,Flags).Invoke(o,args);}
    public static async Task<string> Start()
    {
        if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;
        DateTime deadline=DateTime.UtcNow.AddSeconds(20);
        while(UnityEngine.Object.FindAnyObjectByType<DexArena>()==null){if(DateTime.UtcNow>deadline)throw new Exception("Play did not start");await Task.Delay(50);}
        Application.runInBackground=true;EditorApplication.isPaused=false;int frame=Time.frameCount;
        while(Time.frameCount==frame){if(DateTime.UtcNow>deadline)throw new Exception("No live frame progression");await Task.Delay(50);}
        return "Play is live; frameCount advanced from "+frame+" to "+Time.frameCount;
    }
    static DexArena Setup()
    {
        var a=UnityEngine.Object.FindAnyObjectByType<DexArena>();if(a==null)throw new Exception("Play required");
        EditorApplication.isPaused=false;Set(a,"champion",3);Set(a,"opponent",1);Set(a,"bot",false);Set(a,"waves",false);Set(a,"practice",false);Set(a,"menu",false);Call(a,"ResetWorld");
        object p=Get(a,"player"),e=Get(a,"enemy");
        Set(p,"p",new Vector2(-2,0));Set(p,"goal",new Vector2(-2,0));Set(e,"p",new Vector2(3,0));Set(e,"goal",new Vector2(3,0));Camera.main.orthographicSize=8.2f;
        return a;
    }
    static async Task Capture(string name)
    {
        Directory.CreateDirectory("DexReports/evidence/v2");
        // Discard warm-up rendering before the scored frame.
        await Task.Delay(150);ScreenCapture.CaptureScreenshot(Path.GetFullPath("DexReports/evidence/v2/"+name+".png"));await Task.Delay(180);
    }
    public static async Task<string> Standard(){Setup();await Capture("arena-standard");return "Standard Mel chamber captured.";}
    public static async Task<string> Closeup(){Setup();Camera.main.orthographicSize=4.4f;await Capture("arena-closeup");return "Close-up captured.";}
    public static async Task<string> Busy()
    {
        var a=Setup();var p=Get(a,"player");var e=Get(a,"enemy");
        Call(a,"Cast",p,0,new Vector2(3,0));Call(a,"Cast",p,2,new Vector2(3,0));Call(a,"Cast",e,0,new Vector2(-2,0));
        await Task.Delay(120);Call(a,"Cast",p,1,new Vector2(3,0));await Capture("arena-busy");EditorApplication.isPaused=true;return "Actual Q/E missiles and W nova captured; paused for review.";
    }
    public static async Task<string> Menu(){var a=Setup();Set(a,"menu",true);await Capture("arena-menu");return "Matchup menu captured.";}
    public static string Narrow(){var w=EditorWindow.GetWindow<AgentWorkbench>();w.position=new Rect(100,80,420,900);w.Repaint();return "420px workbench.";}
    public static string Wide(){var w=EditorWindow.GetWindow<AgentWorkbench>();w.position=new Rect(100,80,906,962);w.Repaint();return "906px workbench.";}
    public static string PaletteAudit()
    {
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>();
        int defaults=sprites.Count(r=>r.sharedMaterial==null||r.sharedMaterial.name=="Default-Material");
        string result="Sprite renderers="+sprites.Length+"; missing sprites="+sprites.Count(r=>r.sprite==null)+"; default 3D materials="+defaults+"; particle systems="+UnityEngine.Object.FindObjectsByType<ParticleSystem>().Length+"\n";
        result+="Intentional white texture tint preserves official portrait artwork and procedural sprite palettes. Runtime IMGUI panels are drawn in the art-direction palette. This 2D build has no skybox lighting or TMP UI.\n";
        File.WriteAllText("DexReports/evidence/v2/palette-audit.txt",result);return result;
    }
}
