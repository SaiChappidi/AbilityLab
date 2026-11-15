using System;
using System.IO;
using System.Reflection;
using Dex.Editor;
using UnityEditor;
using UnityEngine;

// Developer-only workflow probes. Never copied into the repair agent workspace.
public static class ProjectModeChecks
{
    public static string StartTest()
    {
        ProjectDebugAgent.Begin(File.ReadAllText("DexReports/evaluation-report.txt"),CodexRunner.FindExecutable(),"",false);
        return "Started test-only investigation of labeled evaluation fixture.";
    }
    public static string StartDirect()
    {
        ProjectDebugAgent.Begin(File.ReadAllText("DexReports/evaluation-report.txt"),CodexRunner.FindExecutable(),"",true);
        return "Started direct investigate-and-repair mode.";
    }
    public static string Continue()
    {
        ProjectDebugAgent.AttemptRepair();return "Requested repair continuation from frozen investigation evidence.";
    }
    public static string State()
    {
        var r=ProjectDebugAgent.Current;
        return r==null?"No run":r.directory+" | "+r.stage+" | "+r.outcome+" | mode="+r.mode+" authorized="+r.repairAuthorized+" ready="+r.readyForRepair+" sourceChanged="+r.sourceChanged+" checks="+r.checksPassed+"/"+r.checksExecuted;
    }
    static void CopyTree(string source,string target)
    {
        Directory.CreateDirectory(target);
        foreach(string file in Directory.GetFiles(source))File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true);
        foreach(string directory in Directory.GetDirectories(source))CopyTree(directory,Path.Combine(target,Path.GetFileName(directory)));
    }
    public static string StaleGuard()
    {
        var field=typeof(ProjectDebugAgent).GetField("current",BindingFlags.Static|BindingFlags.NonPublic);var previous=field.GetValue(null);
        string saved="DexReports/general-20261115-041141-b3e37b",clone="DexReports/general-stale-guard-probe-"+Guid.NewGuid().ToString("N").Substring(0,6);
        CopyTree(saved,clone);
        var run=JsonUtility.FromJson<ProjectDebugRun>(File.ReadAllText(Path.Combine(clone,"state.json")));run.directory=clone;run.outcome="Bug reproduced — no repair attempted";run.readyForRepair=true;run.repairAuthorized=false;run.sourceChanged=false;
        File.WriteAllText(Path.Combine(clone,"state.json"),JsonUtility.ToJson(run,true));field.SetValue(null,run);
        string before=ProjectWorkspaceHash();ProjectDebugAgent.AttemptRepair();
        bool rejected=run.outcome=="Needs developer review"&&!run.repairAuthorized&&!run.sourceChanged&&ProjectWorkspaceHash()==before;
        Directory.CreateDirectory("DexReports/evidence/general");string result=(rejected?"PASS":"FAIL")+" stale investigation is rejected without source application\nOutcome: "+run.outcome+"\nReason: "+run.completionReason+"\n";File.WriteAllText("DexReports/evidence/general/stale-guard.txt",result);field.SetValue(null,previous);return result;
    }
    static string ProjectWorkspaceHash(){return Dex.Editor.ProjectWorkspace.Hash(File.ReadAllText("Assets/Dex/Runtime/Combat.cs"));}
}
