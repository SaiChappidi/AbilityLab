using System;
using System.IO;
using System.Linq;
using Dex.Editor;
using UnityEngine;

public static class GeneralGuardChecks
{
    public static string Run()
    {
        int total=0,passed=0;var log=new System.Text.StringBuilder();
        Action<string,Action,bool> check=(name,action,reject)=>{total++;bool rejected=false;try{action();}catch{rejected=true;}bool ok=rejected==reject;if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+name);};
        string dir="DexReports/evidence/general/guard-workspace";Directory.CreateDirectory(dir);ProjectWorkspace.Capture(dir);string protectedBefore=ProjectWorkspace.ProtectedHash();
        foreach(string path in new[]{"../Assets/Dex/Runtime/Combat.cs","Assets/../Assets/Dex/Runtime/Combat.cs","Assets/Dex/Editor/Verification.cs","Assets/Dex/eDiToR/New.cs","Assets/Dex/Runtime/ArenaDIAGNOSTICS.cs","Assets/Dex/Runtime/Verification.cs","C:/outside.cs","Assets\\Dex\\Runtime\\Combat.cs"})
            check("deny protected/traversing path "+path,()=>{if(!ProjectWorkspace.Editable(path))throw new Exception();},true);
        string source=File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs"),anchor="return magicShield;";
        check("ordinary multi-line body change stages in working copy only",()=>{ProjectWorkspace.Stage(dir,new PatchProposal{analysis="guard probe",edits=new[]{new SourceEdit{path="Assets/Dex/Runtime/CombatRules.cs",find=anchor,replace="bool active = magicShield;\n            return active;"}}});if(File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs")!=source)throw new Exception("Live source changed");},false);
        check("multi-file proposals stage ordinary C# changes",()=>{ProjectWorkspace.Stage(dir,new PatchProposal{edits=new[]{new SourceEdit{path="Assets/Dex/Runtime/CombatRules.cs",find="bool active = magicShield;",replace="bool active = !!magicShield;"},new SourceEdit{path="Assets/Dex/Runtime/ChampionKit.cs",find="//NONEXISTENT",replace=""}}});},true);
        check("failed multi-file proposal commits no partial working edit",()=>{if(File.ReadAllText(Path.Combine(dir,"workspace/Assets/Dex/Runtime/CombatRules.cs")).Contains("!!magicShield"))throw new Exception();},false);
        check("deny arbitrary host API in gameplay patch",()=>ProjectWorkspace.Stage(dir,new PatchProposal{edits=new[]{new SourceEdit{path="Assets/Dex/Runtime/Injected.cs",create=true,find="",replace="using System.IO; class Injected {}"}}}),true);
        check("source snapshot detects stale live manifest",()=>{var stamps=ProjectWorkspace.Capture(dir);stamps[0].hash="stale";ProjectWorkspace.AssertLive(stamps);},true);
        check("frozen check rejects self-comparison",()=>ArenaAssertions.ValidateCheck(new ArenaCheck{at="a",unit="player",field="hp",relation="eq",compareAt="a"}),true);
        check("unknown operations rejected before Play",()=>ArenaAssertions.Validate(new ArenaScenario{actions=new[]{new ArenaAction{op="shell"}},checks=new[]{new ArenaCheck{at="initial",unit="player",field="hp",relation="gt"}}}),true);
        check("excessive simulation budget rejected",()=>ArenaAssertions.Validate(new ArenaScenario{actions=new[]{new ArenaAction{op="advance",seconds=31}},checks=new[]{new ArenaCheck{at="initial",unit="player",field="hp",relation="gt"}}}),true);
        check("nonfinite action rejected",()=>ArenaAssertions.Validate(new ArenaScenario{actions=new[]{new ArenaAction{op="set",value=float.NaN}},checks=new[]{new ArenaCheck{at="initial",unit="player",field="hp",relation="gt"}}}),true);
        check("protected source remains unchanged",()=>{if(ProjectWorkspace.ProtectedHash()!=protectedBefore)throw new Exception();},false);
        string result="General host guard checks: "+passed+"/"+total+"\n"+log;Directory.CreateDirectory("DexReports/evidence/general");File.WriteAllText("DexReports/evidence/general/guards.txt",result);return result;
    }
}
