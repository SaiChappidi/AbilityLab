using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dex.Editor
{
    public sealed class CheckReport
    {
        public int passed,total;
        public readonly List<string> lines=new List<string>();
        public bool Success {get{return total>0&&passed==total;}}
        public void Check(string name,bool expected,bool actual,string detail="")
        {total++;bool ok=expected==actual;if(ok)passed++;lines.Add((ok?"PASS ":"FAIL ")+name+" | expected="+expected+" actual="+actual+" "+detail);}
        public override string ToString(){return "Independent verifier: "+passed+"/"+total+" passed. "+(Success?"VERIFIED":"REJECTED")+"\n"+string.Join("\n",lines);}
    }
    public static class Verification
    {
        public static readonly string[] Scenarios={"root-blink","root-dash","mel-nova","shield-charm","free-blink","stun-blink","expired-root"};
        public static string Run(string id)
        {
            if(id=="root-dash"||id=="mel-nova")return ScenarioLab.Run(id);
            var s=new Combat();bool actual,expected;
            switch(id)
            {
                case "root-blink": s.Cast(s.a,s.b,0);actual=s.Cast(s.b,s.a,2);expected=false;break;
                case "shield-charm": s=new Combat("Ahri","Morgana");s.Cast(s.b,s.a,2);s.Cast(s.a,s.b,2);actual=s.b.ActiveControl(s.time);expected=false;break;
                case "free-blink": actual=s.Cast(s.b,s.a,2);expected=true;break;
                case "stun-blink": s.CC(s.b,"Stun",2);actual=s.Cast(s.b,s.a,2);expected=false;break;
                case "expired-root": s.CC(s.b,"Root",2);s.Tick(2.01f);actual=s.Cast(s.b,s.a,2);expected=true;break;
                default: throw new ArgumentException("Unknown scenario: "+id);
            }
            return id+": "+(actual==expected?"PASS":"FAIL")+" | expected="+expected+" actual="+actual+" | "+string.Join("; ",s.events);
        }
        public static string Visible(){var r=new List<string>();foreach(var id in Scenarios)r.Add(Run(id));return string.Join("\n",r);}
        // Held outside model-editable tools. Expected values are independent constants/specification.
        public static CheckReport Evaluate()
        {
            var r=new CheckReport();
            for(int mask=0;mask<16;mask++)r.Check("mobility control mask "+mask,mask==0,CombatRules.CanMove((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0));
            for(int m=0;m<4;m++)r.Check("immunity mask "+m,(m&1)!=0,CombatRules.BlocksControl((m&1)!=0,(m&2)!=0));
            foreach(bool items in new[]{false,true})
            {
                foreach(string kind in new[]{"Root","Stun","Charm","Ground"})
                foreach(float at in new[]{1.999f,2f,2.001f})
                {
                    var s=new Combat("Morgana","Ezreal",items);s.CC(s.b,kind,2);s.Tick(at);
                    float x=s.b.x,ready=s.b.ready[2],mana=s.b.mana;bool allowed=at>=2;
                    bool cast=s.Cast(s.b,s.a,2);
                    string label=kind+" at "+at.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"s items="+items;
                    r.Check(label,allowed,cast);r.Check(label+" displacement",allowed,s.b.x!=x);r.Check(label+" cooldown consumed",allowed,s.b.ready[2]!=ready);
                    r.Check(label+" mana consumed",allowed,s.b.mana!=mana);
                }
                var shield=new Combat("Ahri","Morgana",items);shield.b.shield=5000;shield.b.shieldUntil=10;shield.Cast(shield.a,shield.b,2);
                r.Check("active shield resists charm items="+items,false,shield.b.ActiveControl(shield.time));
                foreach(float at in new[]{.999f,1f,1.001f})
                {var s=new Combat();s.b.shield=200;s.b.shieldUntil=1;s.Tick(at);s.CC(s.b,"Root",2);r.Check("shield expiry at "+at+" items="+items,at>=1,s.b.ActiveControl(s.time));}
            }
            var normal=new Combat();r.Check("free blink",true,normal.Cast(normal.b,normal.a,2));r.Check("immediate recast cooldown",false,normal.Cast(normal.b,normal.a,2));
            normal=new Combat();normal.b.hp=0;r.Check("dead caster",false,normal.Cast(normal.b,normal.a,2));
            normal=new Combat();r.Check("invalid slot",false,normal.BeginCast(normal.b,normal.a,-1));
            for(int mask=0;mask<8;mask++)r.Check("nova predicate mask "+mask,mask==7,CombatRules.DestroysProjectile((mask&1)!=0,(mask&2)!=0,(mask&4)!=0));
            foreach(bool items in new[]{false,true})
            {
                foreach(string control in new[]{"Root","Stun","Charm","Ground"})foreach(float at in new[]{1.999f,2f,2.001f})
                {var s=new Combat("Mel","Ahri",items);s.CC(s.b,control,2);s.Tick(at);float castMana=s.b.mana;bool allowed=s.Cast(s.b,s.a,3);r.Check("Ahri dash "+control+" at "+at+" items="+items,at>=2,allowed);r.Check("Ahri dash resource "+control+" at "+at+" items="+items,at>=2,s.b.mana!=castMana);}
                foreach(float at in new[]{.749f,.75f,.751f})
                {var s=new Combat("Mel","Morgana",items);s.Cast(s.a,s.b,1);s.Tick(at);bool hit=s.Projectile(s.b,s.a,100,false,"Root",2);r.Check("nova boundary "+at+" items="+items,at>=.75f,hit);r.Check("nova root leakage "+at+" items="+items,at>=.75f,s.a.ActiveControl(s.time));}
                var mel=new Combat("Mel","Ezreal",items);mel.Cast(mel.a,mel.b,1);r.Check("Mel W grants barrier items="+items,true,mel.a.barrier>0);float hp=mel.a.hp;mel.Damage(mel.b,mel.a,50,true);r.Check("barrier absorbs physical damage items="+items,true,mel.a.hp==hp);
                mel.a.ready[1]=0;mel.Cast(mel.a,mel.b,1);mel.a.ready[1]=0;mel.Cast(mel.a,mel.b,1);r.Check("Searing Brilliance cap items="+items,true,mel.a.searing==9);mel.Attack(mel.a,mel.b);r.Check("attack consumes banked bolts items="+items,true,mel.a.searing==0);r.Check("attack applies Overwhelm items="+items,true,mel.b.MarkedBy(mel.a,mel.time));
                mel=new Combat("Mel","Ezreal",items);float ready=mel.a.ready[3],mana=mel.a.mana;r.Check("unmarked R rejected items="+items,false,mel.Cast(mel.a,mel.b,3));r.Check("unmarked R resource preservation items="+items,true,mel.a.ready[3]==ready&&mel.a.mana==mana);
                mel.Damage(mel.a,mel.b,20,false);hp=mel.b.hp;mel.Cast(mel.a,mel.b,3);r.Check("marked R detonates items="+items,true,mel.b.hp<hp&&mel.b.overwhelm==0);
                mel=new Combat("Mel","Morgana",items);mel.Cast(mel.a,mel.b,1);mel.Projectile(mel.b,mel.a,100,false);hp=mel.b.hp;mel.Tick(.75f);r.Check("nova retaliates at owner items="+items,true,mel.b.hp<hp);hp=mel.b.hp;mel.Tick(.1f);r.Check("retaliation occurs once items="+items,true,mel.b.hp==hp);
                mel=new Combat("Mel","Ezreal",items);mel.a.mana=0;ready=mel.a.ready[0];r.Check("Mel mana gate items="+items,false,mel.Cast(mel.a,mel.b,0));r.Check("Mel mana gate keeps cooldown items="+items,true,mel.a.ready[0]==ready);
                mel=new Combat("Mel","Ezreal",items);mel.Cast(mel.a,mel.b,2);r.Check("Solar Snare roots items="+items,true,mel.b.ActiveControl(mel.time));mel=new Combat("Mel","Ezreal",items);mel.Cast(mel.a,mel.b,0);r.Check("Volley builds multiple marks items="+items,true,mel.b.overwhelm>=6);
            }
            return r;
        }
        public static string Holdout(){return Evaluate().ToString();}
        public static void Batch()
        {Directory.CreateDirectory("DexReports");string result=Visible()+"\n"+Holdout();File.WriteAllText("DexReports/verification.txt",result);Debug.Log(result);}
        [UnityEditor.MenuItem("Dex/Run independent verification")]
        static void MenuCheck(){Batch();}
    }
}
