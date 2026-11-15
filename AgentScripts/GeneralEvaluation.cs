using System;
using System.IO;
using Dex.Editor;
using UnityEditor;
using UnityEngine;

// Explicit developer evaluation fixtures. This file is outside the agent snapshot
// and is not used for report routing, reproductions, repair prompts or verification.
public static class GeneralEvaluation
{
    static string Inject(string id,string path,string from,string to,string report)
    {
        if(ProjectDebugAgent.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle.");
        string source=File.ReadAllText(path);int at=source.IndexOf(from,StringComparison.Ordinal);
        if(at<0||source.IndexOf(from,at+from.Length,StringComparison.Ordinal)>=0)throw new Exception("Fixture anchor must occur once.");
        string directory="DexReports/evaluation-fixtures/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+id;Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"original.cs.txt"),source);File.WriteAllText(Path.Combine(directory,"REPORT.txt"),"INTENTIONAL DEVELOPER EVALUATION FIXTURE: "+id+"\n"+report+"\nTarget: "+path);
        File.WriteAllText(path,source.Substring(0,at)+to+source.Substring(at+from.Length));File.WriteAllText(Path.Combine(directory,"mutant.cs.txt"),File.ReadAllText(path));
        File.WriteAllText("DexReports/evaluation-report.txt",report);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        return "Labeled intentional fixture injected: "+id+". Await compilation, then use the general agent with the ordinary report. No fixture id or solution is sent to it.";
    }
    public static string Projectile(){return Inject("projectile-hit-accounting","Assets/Dex/Runtime/DexArena.cs","continue;m.hit.Add(u);","continue;m.hit.Clear();","Ahri's Orb of Deception damages the same stationary enemy repeatedly while passing through it. It should damage that enemy once going out and once coming back, regardless of simulation step size. Reproduce this with the actual projectile and fix the hit accounting.");}
    public static string Reset(){return Inject("reset-cleanup","Assets/Dex/Runtime/DexArena.cs","units.Clear();shots.Clear();zones.Clear();fx.Clear();pops.Clear();","units.Clear();zones.Clear();fx.Clear();pops.Clear();","Resetting the practice arena while projectiles are in flight leaves stale projectiles in the new match. Reset should remove old projectiles, zones and effects and begin with fresh resources. Reproduce the problem and repair the cleanup.");}
    public static string Mana(){return Inject("mana-accounting","Assets/Dex/Runtime/Combat.cs","source.mana-=ChampionKit.Mana(source.champion,slot);","source.mana-=2*ChampionKit.Mana(source.champion,slot);","Casting an ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must not spend more mana or extend its cooldown. Reproduce and fix this using actual arena casts.");}
    public static string Expiration()
    {
        if(ProjectDebugAgent.Busy||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle.");
        string[] paths={"Assets/Dex/Runtime/DexArena.cs","Assets/Dex/Runtime/Combat.cs"};
        string[] from={"if(u.f.barrierUntil<=sim.time)u.f.barrier=0;","if(f.barrierUntil<=time)f.barrier=0;"};
        string[] to={"if(u.f.barrierUntil>=sim.time)u.f.barrier=0;","if(f.barrierUntil>=time)f.barrier=0;"};
        string report="Mel's Rebuttal barrier disappears on the next simulation tick even though its duration is still active. With no incoming damage, it should remain during its active duration, then disappear when the duration expires. Investigate expiration handling and preserve consistent combat behavior.";
        string dir="DexReports/evaluation-fixtures/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-status-expiration";Directory.CreateDirectory(dir);
        var source=new string[paths.Length];for(int i=0;i<paths.Length;i++){source[i]=File.ReadAllText(paths[i]);if(!source[i].Contains(from[i]))throw new Exception("Fixture anchor missing");File.WriteAllText(Path.Combine(dir,Path.GetFileName(paths[i])+".original.txt"),source[i]);}
        for(int i=0;i<paths.Length;i++){File.WriteAllText(paths[i],source[i].Replace(from[i],to[i]));File.WriteAllText(Path.Combine(dir,Path.GetFileName(paths[i])+".mutant.txt"),File.ReadAllText(paths[i]));}
        File.WriteAllText(Path.Combine(dir,"REPORT.txt"),"INTENTIONAL DEVELOPER EVALUATION: two-file status expiration regression\n"+report);File.WriteAllText("DexReports/evaluation-report.txt",report);AssetDatabase.Refresh();return "Labeled two-file expiration fixture injected; no fixture identifier or implementation solution is sent to the agent.";
    }
    public static string Start()
    {
        string report=File.ReadAllText("DexReports/evaluation-report.txt");var window=EditorWindow.GetWindow<AgentWorkbench>();window.StartProjectRequest(report);return "Started a real general Codex investigation. Report was supplied as ordinary task data.";
    }
    public static string State(){var s=ProjectDebugAgent.Current;return s==null?"No run":s.directory+" | "+s.stage+" | "+s.outcome+" | running="+s.running+" | reproduced="+s.reproduced+" repaired="+s.repaired+" verified="+s.verified+" | "+ProjectDebugAgent.Elapsed.ToString("0.0")+"s";}
    public static string Recent(){var s=ProjectDebugAgent.Current;return s==null?"No run":s.log.Substring(Math.Max(0,s.log.Length-4000));}
}
