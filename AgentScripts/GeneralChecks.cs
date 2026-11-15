using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Dex.Editor;

public static class GeneralChecks
{
    public static string Refresh(){AssetDatabase.Refresh();return "Asset refresh requested.";}
    public static async Task<string> Driver()
    {
        const string json="{\"title\":\"Generic driver projectile accounting\",\"expectedBehavior\":\"One Ezreal Q deals one hit and is removed\",\"player\":\"Ezreal\",\"opponent\":\"Morgana\",\"seed\":817,\"actions\":[{\"op\":\"place\",\"unit\":\"player\",\"x\":-2,\"y\":0},{\"op\":\"place\",\"unit\":\"enemy\",\"x\":2,\"y\":0},{\"op\":\"cast\",\"unit\":\"player\",\"slot\":\"Q\",\"x\":2,\"y\":0,\"label\":\"fired\"},{\"op\":\"advance\",\"seconds\":0.5,\"label\":\"impact\"}],\"checks\":[{\"label\":\"Q damages target\",\"at\":\"impact\",\"unit\":\"enemy\",\"field\":\"hp\",\"relation\":\"lt\",\"compareAt\":\"fired\"},{\"label\":\"Nonpiercing missile is removed\",\"at\":\"impact\",\"unit\":\"world\",\"field\":\"shots\",\"relation\":\"eq\",\"value\":0}]}";
        var s=JsonUtility.FromJson<ArenaScenario>(json);Directory.CreateDirectory("DexReports/evidence/general");var a=await ArenaScenarioDriver.Run(s,"DexReports/evidence/general","driver-a",CancellationToken.None);var b=await ArenaScenarioDriver.Run(s,"DexReports/evidence/general","driver-b",CancellationToken.None);
        bool same=JsonUtility.ToJson(a.observations)==JsonUtility.ToJson(b.observations);string result=a+"\n"+b+"\nSeeded observations identical: "+same;File.WriteAllText("DexReports/evidence/general/driver-check.txt",result);return result;
    }
}
