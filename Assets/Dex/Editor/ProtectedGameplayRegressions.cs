namespace Dex.Editor
{
    // Independent invariants for authored temporary-state lifetimes. Not supplied
    // as repair source, not editable through the model's source writer.
    public static class ProtectedGameplayRegressions
    {
        public static CheckReport Evaluate()
        {
            var report=new CheckReport();
            foreach(string champion in ChampionKit.Roster)
            foreach(float at in new[]{.5f,.999f,1f,1.001f})
            {
                var combat=new Combat(champion,"Ezreal",false);
                combat.a.barrier=100;combat.a.barrierUntil=1;combat.a.shield=100;combat.a.shieldUntil=1;
                combat.Tick(at);
                report.Check(champion+" temporary barrier at "+at,at<1,combat.a.barrier>0);
                report.Check(champion+" temporary magic shield at "+at,at<1,combat.a.shield>0);
            }
            return report;
        }
    }
    public sealed class ProtectedRegressionRun
    {
        public bool success;public string text;public override string ToString(){return text;}
    }
}
