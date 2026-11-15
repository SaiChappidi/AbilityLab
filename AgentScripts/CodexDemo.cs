using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dex.Editor;
using UnityEditor;

public static class CodexDemo
{
    public static async Task<string> Login()
    {string path=CodexRunner.FindExecutable();return "Executable: "+path+"\n"+await CodexRunner.CheckLogin(path,CancellationToken.None);}
    public static string Begin()
    {
        var window=EditorWindow.GetWindow<AgentWorkbench>();
        typeof(AgentWorkbench).GetField("request",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(window,"Ezreal can blink while rooted by Morgana. Reproduce it, repair the ability rule, and preserve legitimate mobility.");
        window.BeginCodex();
        return "Workbench started a real Codex run. Inspect stage and timestamped report; no success assumed.";
    }
    public static string Summary()
    {return SessionState.GetString("Dex.Agent.stage","")+" | "+SessionState.GetString("Dex.Agent.summary","")+" | running="+SessionState.GetBool("Dex.Agent.running",false)+" | pending="+SessionState.GetBool("Dex.Agent.pending",false)+" | "+SessionState.GetString("Dex.Agent.directory","");}
    public static string Recent()
    {string report=SessionState.GetString("Dex.Agent.report","");return report.Substring(Math.Max(0,report.Length-2600));}
}
