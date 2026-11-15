using System;
using System.IO;
using UnityEditor;

namespace Dex.Editor
{
    // An opt-in human timer, never populated by an automated reference repair.
    public static class ManualBaseline
    {
        const string Prefix="Dex.Manual.";
        public static bool Active {get{return SessionState.GetString(Prefix+"started","")!="";}}
        public static string Status {get{return SessionState.GetString(Prefix+"status","No human baseline recorded. Automated wall time is not measured labor savings.");}}
        public static void Start(string caseId)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play and wait for compilation before timing a manual baseline.");
            string folder="DexReports/manual-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
            SessionState.SetString(Prefix+"directory",folder);SessionState.SetString(Prefix+"case",caseId);SessionState.SetString(Prefix+"started",DateTime.UtcNow.Ticks.ToString());SessionState.SetString(Prefix+"status","Manual timer is running. Reproduce and edit the rule yourself, let Unity compile, then stop and verify.");
            File.WriteAllText(Path.Combine(folder,"before.cs.txt"),File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs"));
        }
        public static void StopAndVerify()
        {
            if(!Active||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            double seconds=(DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString(Prefix+"started","0")),DateTimeKind.Utc)).TotalSeconds;
            var checks=Verification.Evaluate();string result="Human-observed manual interval: "+seconds.ToString("0.0")+"s\nEncounter: "+SessionState.GetString(Prefix+"case","")+"\n"+checks+"\nThis timer includes the human interval up to this button and core verification. It excludes automated arena replays. Conditions are not controlled; do not interpret it as a general speedup benchmark.";
            string folder=SessionState.GetString(Prefix+"directory","");File.WriteAllText(Path.Combine(folder,"result.txt"),result);File.WriteAllText(Path.Combine(folder,"after.cs.txt"),File.ReadAllText("Assets/Dex/Runtime/CombatRules.cs"));
            SessionState.SetString(Prefix+"started","");SessionState.SetString(Prefix+"status",seconds.ToString("0.0")+"s manual interval; "+checks.passed+"/"+checks.total+" core checks. See manual evidence folder. No automatic speedup claim.");
        }
    }
}
