using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dex.Editor
{
    public static class ScenarioLab
    {
        public static string Run(string id)
        {
            if(id=="root-dash")
            {var s=new Combat("Mel","Ahri");s.Cast(s.a,s.b,2);bool allowed=s.Cast(s.b,s.a,3);return "root-dash: "+(!allowed?"PASS":"FAIL")+" | expected dash=False actual="+allowed+" | "+string.Join("; ",s.events);}
            if(id=="mel-nova")
            {var s=new Combat("Mel","Morgana");s.Cast(s.a,s.b,1);bool hit=s.Projectile(s.b,s.a,200,false,"Root",2);return "mel-nova: "+(!hit&&!s.a.ActiveControl(s.time)?"PASS":"FAIL")+" | expected missile hit=False actual="+hit+" | "+string.Join("; ",s.events);}
            throw new ArgumentException("Unknown extension scenario.");
        }
        // Deterministic exploration and action deletion are host tools, not model assertions.
        public static string Explore(string encounter="root-blink")
        {
            var lines=new List<string>();int total=0,failures=0;
            foreach(string caster in new[]{"Morgana","Mel"})foreach(string mover in new[]{"Ezreal","Ahri"})
            foreach(float delay in new[]{0f,.5f,1.499f,1.5f,1.999f,2f,2.001f})
            {
                var s=new Combat(caster,mover);s.Cast(s.a,s.b,caster=="Mel"?2:0);float expiry=s.b.controlUntil;s.Tick(delay);
                bool expected=delay>=expiry,actual=s.Cast(s.b,s.a,mover=="Ahri"?3:2);total++;
                if(expected!=actual){failures++;lines.Add(caster+" -> "+mover+" at "+delay.ToString("F3")+"s: expected cast="+expected+", actual="+actual);}
            }
            foreach(string missileOwner in new[]{"Morgana","Ezreal","Ahri"})foreach(float at in new[]{0f,.25f,.749f,.75f,.751f})
            {var s=new Combat("Mel",missileOwner);s.Cast(s.a,s.b,1);s.Tick(at);bool actual=s.Projectile(s.b,s.a,100,false,"Root",2),expected=at>=.75f;total++;if(actual!=expected){failures++;lines.Add("Rebuttal vs "+missileOwner+" at "+at.ToString("F3")+"s: expected hit="+expected+", actual="+actual);}}
            string setup=encounter=="mel-nova"?"nova":encounter=="shield-charm"?"shield":"root";
            var sequence=new List<string>{"idle","equip",setup,"wait","cast","observe"};
            bool Broken(List<string> actions)
            {
                var s=encounter=="mel-nova"?new Combat("Mel","Morgana"):encounter=="shield-charm"?new Combat("Ahri","Morgana"):encounter=="root-dash"?new Combat("Mel","Ahri"):new Combat();
                foreach(string action in actions)
                {
                    if(action=="root")s.Cast(s.a,s.b,encounter=="root-dash"?2:0);if(action=="nova")s.Cast(s.a,s.b,1);if(action=="shield")s.Cast(s.b,s.a,2);if(action=="wait")s.Tick(.25f);
                    if(action=="cast")
                    {
                        if(encounter=="mel-nova"){bool active=s.a.novaUntil>s.time;bool hit=s.Projectile(s.b,s.a,100,false,"Root",2);if(active&&hit)return true;}
                        else if(encounter=="shield-charm"){bool active=s.b.Shielded(s.time);s.Cast(s.a,s.b,2);if(active&&s.b.ActiveControl(s.time))return true;}
                        else{bool rooted=s.b.ActiveControl(s.time);bool allowed=s.Cast(s.b,s.a,encounter=="root-dash"?3:2);if(rooted&&allowed)return true;}
                    }
                }
                return false;
            }
            if(Broken(sequence))for(int index=0;index<sequence.Count;){var shorter=sequence.Where((a,i)=>i!=index).ToList();if(Broken(shorter))sequence=shorter;else index++;}
            string reduced=Broken(sequence)?string.Join(" -> ",sequence):"No "+encounter+" failure to minimize in the loaded build.";
            return "Deterministic exploration: "+total+" generated timing/matchup cases; "+failures+" failures.\n"+string.Join("\n",lines.Take(12))+"\nAction deletion: 6 original actions -> "+(Broken(sequence)?sequence.Count+" required actions ("+reduced+")":reduced)+"\nScope: finite generated simplified scenarios, not arbitrary gameplay discovery.";
        }
    }
}
