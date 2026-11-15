using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class RecordReplay
{
    public static Task<string> Before(){return Capture("before");}
    public static Task<string> After(){return Capture("after");}
    static async Task<string> Capture(string phase)
    {
        var arena=UnityEngine.Object.FindAnyObjectByType<Dex.DexArena>();if(arena==null)throw new Exception("Enter Play mode first.");
        string folder="DexReports/codex-demo-backup/"+phase+"-frames";Directory.CreateDirectory(folder);
        arena.StartInteractionReplay();
        for(int frame=0;frame<24;frame++)
        {ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(folder,frame.ToString("D3")+".png")));await Task.Delay(250);}
        int captured=Directory.GetFiles(folder,"*.png").Length;
        string result=phase+" replay: "+captured+"/24 actual Game view frames\n"+arena.ReplayResult;
        File.WriteAllText("DexReports/codex-demo-backup/"+phase+".txt",result);return result;
    }
}
