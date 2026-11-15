using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;

namespace Dex.Editor.Evaluation
{
    // Explicit developer menus, separate from the report-driven agent. These
    // inject labeled mutants; no case identifier is passed to ProjectDebugAgent.
    public static class DeveloperEvaluationFixtures
    {
        [MenuItem("Dex/Developer evaluation/Inject projectile accounting bug")]
        public static void Projectile(){Inject("projectile-accounting","Ahri's Orb of Deception damages a stationary enemy repeatedly while passing through it. It should hit that enemy once outward and once returning, independent of simulation step size.",new SourceEdit{path="Assets/Dex/Runtime/DexArena.cs",find="continue;m.hit.Add(u);",replace="continue;m.hit.Clear();"});}
        [MenuItem("Dex/Developer evaluation/Inject reset cleanup bug")]
        public static void Reset(){Inject("reset-cleanup","Resetting while projectiles are in flight leaves stale projectiles in the new match. Reset should remove old projectiles, zones and effects and restore fresh resources.",new SourceEdit{path="Assets/Dex/Runtime/DexArena.cs",find="units.Clear();shots.Clear();zones.Clear();fx.Clear();pops.Clear();",replace="units.Clear();zones.Clear();fx.Clear();pops.Clear();"});}
        [MenuItem("Dex/Developer evaluation/Inject mana accounting bug")]
        public static void Mana(){Inject("mana-accounting","An ability takes twice its listed mana cost. A successful cast should charge the listed cost once; a rejected cooldown recast must preserve mana and cooldown.",new SourceEdit{path="Assets/Dex/Runtime/Combat.cs",find="source.mana-=ChampionKit.Mana(source.champion,slot);",replace="source.mana-=2*ChampionKit.Mana(source.champion,slot);"});}
        [MenuItem("Dex/Developer evaluation/Inject two-file expiration bug")]
        public static void Expiration(){Inject("status-expiration","Mel's Rebuttal barrier disappears on the next tick while its duration is active. Without incoming damage it should remain during its duration and disappear at expiry. Preserve consistent combat behavior.",new SourceEdit{path="Assets/Dex/Runtime/DexArena.cs",find="if(u.f.barrierUntil<=sim.time)u.f.barrier=0;",replace="if(u.f.barrierUntil>=sim.time)u.f.barrier=0;"},new SourceEdit{path="Assets/Dex/Runtime/Combat.cs",find="if(f.barrierUntil<=time)f.barrier=0;",replace="if(f.barrierUntil>=time)f.barrier=0;"});}
        static void Inject(string id,string report,params SourceEdit[] changes)
        {
            if(ProjectDebugAgent.Busy||CodexRunner.IsBusy||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SessionState.GetBool("Dex.Agent.running",false))throw new InvalidOperationException("Stop active work and Play before injecting a developer fixture.");
            string dir="DexReports/evaluation-fixtures/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+id;Directory.CreateDirectory(dir);
            // Stage and validate the entire explicit mutation before touching live code.
            var baseline=ProjectWorkspace.Capture(dir);ProjectWorkspace.Stage(dir,new PatchProposal{analysis="INTENTIONAL DEVELOPER FIXTURE",edits=changes});
            File.WriteAllText(Path.Combine(dir,"INTENTIONAL_FIXTURE.txt"),id+"\n"+report+"\nThis is injected by a developer menu, not discovered by the agent. Baseline and mutant are saved separately.");
            ProjectWorkspace.Apply(dir,baseline);AssetDatabase.Refresh();CompilationPipeline.RequestScriptCompilation();
            UnityEngine.Debug.Log("Dex intentional evaluation fixture injected: "+id+". After compilation, enter this ordinary report in Project Debugger:\n"+report+"\nOriginal source is backed up in "+dir);
        }
    }
}
