using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Dex.Editor
{
    [Serializable] public sealed class ReplayProof
    {public string caseId,phase,result,capturedUtc,screenshot;public bool passed;public double seconds;}
    public static class ReplayAutomation
    {
        public static bool Busy {get;private set;}
        const string Owned="Dex.Automation.PlayOwned";
        [InitializeOnLoadMethod] static void RecoverInterruptedReplay()
        {if(SessionState.GetBool(Owned,false)){SessionState.SetBool(Owned,false);EditorApplication.delayCall+=()=>{if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;};}}
        public static async Task<ReplayProof> Run(string caseId,string phase,string directory,CancellationToken token)
        {
            if(Busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Automatic replay requires an idle Editor. Stop the existing Play session first.");
            if(!EditorSettings.enterPlayModeOptionsEnabled||(EditorSettings.enterPlayModeOptions&EnterPlayModeOptions.DisableDomainReload)==0)
                throw new InvalidOperationException("Automatic replay uses this project's Disable Domain Reload Play setting. Enable it in Editor settings before using auto evidence; manual replay remains available.");
            Busy=true;bool background=Application.runInBackground;DateTime start=DateTime.UtcNow;
            try
            {
                SessionState.SetBool(Owned,true);EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;
                DexArena arena=null;
                while(arena==null)
                {token.ThrowIfCancellationRequested();if((DateTime.UtcNow-start).TotalSeconds>25)throw new TimeoutException("Arena did not start for automated replay.");if(EditorApplication.isPlaying)arena=UnityEngine.Object.FindAnyObjectByType<DexArena>();await Task.Delay(50,token);}
                Application.runInBackground=true;arena.StartInteractionReplay(caseId);
                string frames=Path.Combine(directory,phase+"-frames");Directory.CreateDirectory(frames);int frame=0;DateTime nextFrame=DateTime.UtcNow;
                while(!arena.ReplayCompleted)
                {
                    token.ThrowIfCancellationRequested();if((DateTime.UtcNow-start).TotalSeconds>40)throw new TimeoutException("Replay did not finish. No success assumed.");if(!EditorApplication.isPlaying)throw new InvalidOperationException("Replay interrupted by leaving Play mode.");
                    if(DateTime.UtcNow>=nextFrame){ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(frames,(frame++).ToString("D3")+".png")));nextFrame=DateTime.UtcNow.AddSeconds(.25);}
                    await Task.Delay(50,token);
                }
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(frames,(frame++).ToString("D3")+".png")));await Task.Delay(100,token);
                string image=Path.GetFullPath(Path.Combine(directory,phase+"-replay.png"));ScreenCapture.CaptureScreenshot(image);await Task.Delay(180,token);
                var proof=new ReplayProof{caseId=caseId,phase=phase,result=arena.ReplayResult,passed=arena.ReplayPassed,capturedUtc=DateTime.UtcNow.ToString("O"),screenshot=image,seconds=(DateTime.UtcNow-start).TotalSeconds};
                File.WriteAllText(Path.Combine(directory,phase+"-replay.json"),JsonUtility.ToJson(proof,true));return proof;
            }
            finally
            {
                if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
                DateTime stop=DateTime.UtcNow;
                while(EditorApplication.isPlayingOrWillChangePlaymode&&(DateTime.UtcNow-stop).TotalSeconds<15)await Task.Delay(50);
                Application.runInBackground=background;Busy=false;SessionState.SetBool(Owned,false);
            }
        }
    }
}
