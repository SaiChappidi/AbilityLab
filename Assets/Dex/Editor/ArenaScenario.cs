using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Dex.Editor
{
    // Declarative tests are data. No model-generated C# test is executed by the host.
    [Serializable] public sealed class ArenaScenario
    {
        public string title,expectedBehavior,player="Ezreal",opponent="Morgana";
        public int seed=12345;
        public ArenaAction[] actions;
        public ArenaCheck[] checks;
    }
    [Serializable] public sealed class ArenaAction
    {
        public string op,unit,target,field,text,label,champion,slot;
        public float x,y,value,seconds,dt;
        public bool red;
    }
    [Serializable] public sealed class ArenaCheck
    {
        public string label,at,unit,field,relation,compareAt,compareUnit,compareField;
        public float value,tolerance=.001f;
    }
    [Serializable] public sealed class ArenaValue {public string name;public float value;}
    [Serializable] public sealed class ArenaUnitState {public string id,champion,control;public ArenaValue[] values;}
    [Serializable] public sealed class ArenaObservation
    {
        public string label,action;public float time;public ArenaValue[] world;public ArenaUnitState[] units;public string[] events;
        public float Read(string unit,string field)
        {
            ArenaValue found;
            if(unit=="world")found=world.FirstOrDefault(v=>v.name==field);
            else {var actor=units.FirstOrDefault(u=>u.id==unit);if(actor==null)throw new ArgumentException("Unknown observed unit: "+unit);found=actor.values.FirstOrDefault(v=>v.name==field);}
            if(found==null)throw new ArgumentException("Unknown observed field: "+unit+"."+field);return found.value;
        }
    }
    [Serializable] public sealed class ArenaRun
    {
        public string title,error,screenshot;public int seed,passed,total;public ArenaObservation[] observations;public string[] assertions;
        public bool Success {get{return string.IsNullOrEmpty(error)&&total>0&&passed==total;}}
        public bool Reproduced {get{return string.IsNullOrEmpty(error)&&total>0&&passed<total;}}
        public override string ToString(){return title+": "+passed+"/"+total+(string.IsNullOrEmpty(error)?"":" ERROR: "+error)+"\n"+string.Join("\n",assertions??new string[0]);}
    }
    public static class ArenaAssertions
    {
        public static void Validate(ArenaScenario s)
        {
            if(s==null||s.actions==null||s.actions.Length<1||s.actions.Length>100||s.checks==null||s.checks.Length<1||s.checks.Length>60)throw new ArgumentException("A scenario needs 1..100 actions and 1..60 observable assertions.");
            if(!ChampionKit.Roster.Contains(s.player)||!ChampionKit.Roster.Contains(s.opponent))throw new ArgumentException("Unknown initial champion.");
            var labels=new HashSet<string>{"initial"};int steps=0;float seconds=0;
            var ops=new[]{"reset","spawn","place","set","move","attack","stop","cast","control","damage","advance","observe"};
            foreach(var a in s.actions)
            {
                if(a==null||!ops.Contains(a.op))throw new ArgumentException("Unsupported action operation.");
                if(!string.IsNullOrEmpty(a.label)&&!labels.Add(a.label))throw new ArgumentException("Duplicate observation label: "+a.label);
                if(float.IsNaN(a.value)||float.IsInfinity(a.value)||Math.Abs(a.value)>1000000||float.IsNaN(a.x)||float.IsInfinity(a.x)||Math.Abs(a.x)>100||float.IsNaN(a.y)||float.IsInfinity(a.y)||Math.Abs(a.y)>100||float.IsNaN(a.seconds)||float.IsInfinity(a.seconds)||float.IsNaN(a.dt)||float.IsInfinity(a.dt))throw new ArgumentException("Non-finite or excessive action argument.");
                if(a.op=="advance")
                {if(float.IsNaN(a.seconds)||float.IsInfinity(a.seconds)||a.seconds<0||a.seconds>30||float.IsNaN(a.dt)||float.IsInfinity(a.dt))throw new ArgumentException("Advance must be 0..30 simulated seconds.");float dt=a.dt>0?a.dt:1f/60;if(dt<.005f||dt>.1f)throw new ArgumentException("dt must be .005..0.1 seconds.");seconds+=a.seconds;steps+=(int)Math.Ceiling(a.seconds/dt);}
            }
            if(seconds>120||steps>15000)throw new ArgumentException("Scenario exceeds simulation budget.");
            foreach(var c in s.checks)ValidateCheck(c);
        }
        public static void ValidateCheck(ArenaCheck c)
        {
            if(c==null||string.IsNullOrEmpty(c.at)||string.IsNullOrEmpty(c.unit)||string.IsNullOrEmpty(c.field)||!new[]{"eq","ne","lt","lte","gt","gte"}.Contains(c.relation)||float.IsNaN(c.value)||float.IsInfinity(c.value)||float.IsNaN(c.tolerance)||float.IsInfinity(c.tolerance)||c.tolerance<0||c.tolerance>.1f)throw new ArgumentException("Invalid observable assertion.");
            if(c.compareAt==c.at&&(string.IsNullOrEmpty(c.compareUnit)||c.compareUnit==c.unit)&&(string.IsNullOrEmpty(c.compareField)||c.compareField==c.field))throw new ArgumentException("An assertion cannot compare an observation with itself.");
        }
        public static void Evaluate(ArenaRun run,ArenaCheck[] checks)
        {
            var lines=new List<string>();int passed=0;
            foreach(var c in checks)
            {
                ValidateCheck(c);var observation=run.observations.First(o=>o.label==c.at);float actual=observation.Read(c.unit,c.field),expected=c.value;
                if(!string.IsNullOrEmpty(c.compareAt))expected+=run.observations.First(o=>o.label==c.compareAt).Read(string.IsNullOrEmpty(c.compareUnit)?c.unit:c.compareUnit,string.IsNullOrEmpty(c.compareField)?c.field:c.compareField);
                float t=c.tolerance;bool ok=c.relation=="eq"?Math.Abs(actual-expected)<=t:c.relation=="ne"?Math.Abs(actual-expected)>t:c.relation=="lt"?actual<expected-t:c.relation=="lte"?actual<=expected+t:c.relation=="gt"?actual>expected+t:actual>=expected-t;
                if(ok)passed++;lines.Add((ok?"PASS ":"FAIL ")+(string.IsNullOrEmpty(c.label)?c.unit+"."+c.field:c.label)+": observed "+actual.ToString("0.#####",CultureInfo.InvariantCulture)+" "+c.relation+" expected "+expected.ToString("0.#####",CultureInfo.InvariantCulture)+" @ "+c.at);
            }
            run.passed=passed;run.total=checks.Length;run.assertions=lines.ToArray();
        }
        public static string Describe(ArenaScenario s)
        {
            if(s==null)return "No scenario.";
            var text=new System.Text.StringBuilder();
            text.AppendLine(s.title);text.AppendLine("Seed: "+s.seed+" / "+s.player+" vs "+s.opponent);text.AppendLine("Expected: "+s.expectedBehavior);text.AppendLine();
            for(int i=0;i<s.actions.Length;i++)text.AppendLine((i+1)+". "+DescribeAction(s.actions[i]));
            text.AppendLine("\nAssertions:");
            foreach(var c in s.checks)
            {
                string expected=c.value.ToString(CultureInfo.InvariantCulture);
                if(!string.IsNullOrEmpty(c.compareAt))expected+=" + "+c.compareAt+"."+(string.IsNullOrEmpty(c.compareUnit)?c.unit:c.compareUnit)+"."+(string.IsNullOrEmpty(c.compareField)?c.field:c.compareField);
                text.AppendLine(c.label+" @ "+c.at+": "+c.unit+"."+c.field+" "+c.relation+" "+expected);
            }
            return text.ToString();
        }
        static string DescribeAction(ArenaAction a)
        {
            string tail=string.IsNullOrEmpty(a.label)?"":" → observe ["+a.label+"]";
            string point="("+a.x.ToString("0.###",CultureInfo.InvariantCulture)+", "+a.y.ToString("0.###",CultureInfo.InvariantCulture)+")";
            return (a.op=="cast"?"Cast "+a.unit+" "+a.slot+" toward "+point:a.op=="advance"?"Advance "+a.seconds.ToString("0.###",CultureInfo.InvariantCulture)+"s at dt="+(a.dt>0?a.dt:1f/60).ToString("0.###",CultureInfo.InvariantCulture):a.op=="place"?"Place "+a.unit+" at "+point:a.op=="set"?"Set initial "+a.unit+"."+a.field+" = "+a.value:a.op=="spawn"?"Spawn "+a.unit+" ("+a.champion+", "+(a.red?"red":"blue")+") at "+point:a.op=="move"?"Move "+a.unit+" toward "+point:a.op=="attack"?"Order "+a.unit+" to attack "+a.target:a.op=="control"?"Apply "+a.text+" to "+a.unit+" for "+a.seconds+"s":a.op=="damage"?"Damage "+a.target+" from "+a.unit+" for "+a.value+" "+a.text:a.op=="stop"?"Stop "+a.unit:a.op=="reset"?"Reset the actual arena":"Observe state")+tail;
        }
    }
}
