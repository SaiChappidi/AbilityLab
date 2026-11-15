using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinishQualityPass
{
    public static string RestoreAndRecord()
    {
        if(UnityEditor.SessionState.GetBool("Dex.Agent.running",false)||Dex.Editor.ReplayAutomation.Busy)throw new Exception("An automated run is still active.");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop the owned test Play session before finalizing.");
        Application.runInBackground=false;EditorApplication.isPaused=false;
        string state="Captured UTC: "+DateTime.UtcNow.ToString("O")+"\nUnity: "+Application.unityVersion+"\nScene: "+SceneManager.GetActiveScene().path+"\nBuild target: "+EditorUserBuildSettings.activeBuildTarget+"\nCompiling: "+EditorApplication.isCompiling+"\nCompilation failed: "+EditorUtility.scriptCompilationFailed+"\nPlay: "+EditorApplication.isPlaying+"\nBackground override: "+Application.runInBackground+"\nPlay options enabled: "+EditorSettings.enterPlayModeOptionsEnabled+" / "+EditorSettings.enterPlayModeOptions+"\n";
        File.WriteAllText("DexReports/evidence/v2/final-editor-state.txt",state);
        File.Copy("Assets/Dex/Runtime/CombatRules.cs","DexReports/evidence/v2/delivered-rules.cs.txt",true);
        return state;
    }
}
