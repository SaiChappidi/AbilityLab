using System;
using System.Collections.Generic;

namespace Dex
{
    [Serializable] public class Fighter
    {
        public string champion, build;
        public float hp = 1800, maxHp = 1800, ap, ad = 110, armor = 60, mr = 40, haste;
        public float x, controlUntil, shield, shieldUntil, markUntil, poolUntil, tetherUntil;
        public float mana=900,barrier,barrierUntil,novaUntil,speedUntil,slowUntil,overwhelmUntil;
        public int searing,overwhelm;public Fighter overwhelmOwner;
        public readonly Dictionary<Fighter,int> retaliations=new Dictionary<Fighter,int>();
        public string control = "";
        public int stacks, rushCharges = 3;
        public float[] ready = new float[4];
        public Fighter(string name, bool equipped, float position)
        {
            champion = name; x = position;
            build = equipped ? (name == "Ezreal" ? "Trinity Force / Muramana" : name == "Ahri" ? "Malignance / Rabadon's Deathcap" : name=="Mel"?"Luden's Companion / Shadowflame":"Liandry's Torment / Zhonya's Hourglass") : "No items";
            // Demonstration aggregate stats; item procs and exact live balance are not modeled.
            if(equipped) { ap = name == "Ezreal" ? 0 : 230; ad += name == "Ezreal" ? 100 : 0; haste = 30; }
        }
        public bool ActiveControl(float t) { return hp > 0 && controlUntil > t; }
        public bool Shielded(float t) { return shield > 0 && shieldUntil > t; }
        public bool MarkedBy(Fighter owner,float t){return overwhelm>0&&overwhelmUntil>t&&overwhelmOwner==owner;}
    }
    public class Combat
    {
        public Fighter a, b;
        public float time;
        public readonly List<string> events = new List<string>();
        public Combat(string left = "Morgana", string right = "Ezreal", bool items = true)
        { a = new Fighter(left, items, 0.28f); b = new Fighter(right, items, 0.7f); }
        public void Log(string message) { events.Insert(0, time.ToString("0.00") + "s  " + message); if(events.Count > 30) events.RemoveAt(30); }
        public void Tick(float dt)
        {
            time += dt;
            foreach(var f in new[]{a,b})
            {
                var enemy = f == a ? b : a;
                if(f.controlUntil <= time) f.control = "";
                if(f.shieldUntil <= time) f.shield = 0;
                if(f.barrierUntil<=time)f.barrier=0;
                if(f.overwhelmUntil<=time)f.overwhelm=0;
                if(f.hp <= 0) continue;
                if(f.control == "Charm") f.x = Approach(f.x, enemy.x, dt * 0.025f);
                if(f.poolUntil > time) Damage(enemy, f, (35 + enemy.ap * 0.1f) * dt, false);
                if(f.tetherUntil > 0 && time >= f.tetherUntil)
                { f.tetherUntil = 0; if(Math.Abs(f.x - enemy.x) < 0.55f) { Damage(enemy,f,200+enemy.ap*0.7f,false); CC(f,"Stun",1.5f); Log("Soul Shackles detonates"); } else Log("Soul Shackles tether broken"); }
            }
            ResolveRetaliations(a,(from,to,amount)=>Damage(from,to,amount,false));ResolveRetaliations(b,(from,to,amount)=>Damage(from,to,amount,false));
        }
        static float Approach(float v,float target,float step) { return v < target ? Math.Min(v+step,target) : Math.Max(v-step,target); }
        public void Damage(Fighter source, Fighter target, float amount, bool physical,bool applyMark=true)
        {
            if(target.hp<=0||amount<=0)return;
            float dealt = amount * 100f / (100f + (physical ? target.armor : target.mr));
            if(target.barrierUntil>time&&target.barrier>0){float absorbed=Math.Min(target.barrier,dealt);target.barrier-=absorbed;dealt-=absorbed;}
            if(!physical && target.Shielded(time)) { float absorbed = Math.Min(target.shield,dealt); target.shield -= absorbed; dealt -= absorbed; }
            target.hp = Math.Max(0,target.hp-dealt);
            if(source.champion == "Morgana"&&source.hp>0) source.hp = Math.Min(source.maxHp,source.hp+dealt*0.18f);
            if(source.champion == "Ezreal") source.stacks = Math.Min(5,source.stacks+1);
            if(source.champion=="Mel"&&applyMark&&dealt>0&&target.champion!="Turret")
            {
                if(!target.MarkedBy(source,time))target.overwhelm=0;
                if(target.overwhelm<int.MaxValue)target.overwhelm++;
                target.overwhelmOwner=source;target.overwhelmUntil=time+5;
                if(target.hp>0&&target.hp<40+.1f*source.ap+target.overwhelm*6){target.hp=0;target.overwhelm=0;Log("Overwhelm executes "+target.champion);}
            }
        }
        public bool InterceptProjectile(Fighter defender,Fighter owner,bool insideNova)
        {
            if(defender.champion!="Mel"||!CombatRules.DestroysProjectile(defender.novaUntil>time,defender!=owner,insideNova))return false;
            int n;defender.retaliations.TryGetValue(owner,out n);defender.retaliations[owner]=Math.Min(7,n+1);Log("Rebuttal destroys "+owner.champion+" missile");return true;
        }
        public bool Projectile(Fighter source,Fighter target,float damage,bool physical,string control="",float duration=0)
        {if(InterceptProjectile(target,source,true))return false;Damage(source,target,damage,physical);if(control!=""&&target.hp>0)CC(target,control,duration);return true;}
        public void ResolveRetaliations(Fighter defender,Action<Fighter,Fighter,float> hit)
        {
            if(defender.retaliations.Count==0||time<defender.novaUntil)return;
            if(defender.hp>0)foreach(var pair in defender.retaliations)if(pair.Key.hp>0)hit(defender,pair.Key,45+.1f*defender.ap+(pair.Value-1)*(15+.05f*defender.ap));
            defender.retaliations.Clear();
        }
        public void Attack(Fighter source,Fighter target)
        {Damage(source,target,source.ad,true);if(source.champion=="Mel"){int bolts=source.searing;source.searing=0;for(int i=0;i<bolts;i++)Damage(source,target,20+.04f*source.ap,false);}}
        public void MelUltimate(Fighter source,Fighter target)
        {if(!target.MarkedBy(source,time))return;int count=target.overwhelm;target.overwhelm=0;Damage(source,target,150+.5f*source.ap+count*(8+.02f*source.ap),false,false);Log("Golden Eclipse detonates "+count+" stacks");}
        public void CC(Fighter target,string kind,float seconds)
        { if(CombatRules.BlocksControl(target.Shielded(time),false)) { Log("Black Shield resists " + kind); return; } target.control = kind; target.controlUntil = time+seconds; Log(target.champion+" receives "+kind); }
        public bool Mobility(Fighter f)
        { bool active=f.ActiveControl(time); return CombatRules.CanMove(active&&f.control=="Stun",active&&f.control=="Root",active&&f.control=="Charm",active&&f.control=="Ground"); }
        public bool Cast(Fighter source,Fighter target,int slot)
        {
            if(!BeginCast(source,target,slot)) return false;
            ResolveCast(source,target,slot);
            return true;
        }
        public bool BeginCast(Fighter source,Fighter target,int slot)
        {
            if(slot<0 || slot>3 || source.hp<=0 || time<source.ready[slot]||source.mana<ChampionKit.Mana(source.champion,slot)) return false;
            if(source.champion=="Mel"&&slot==3&&!target.MarkedBy(source,time)){Log("Golden Eclipse needs an active Overwhelm mark");return false;}
            if(source.ActiveControl(time) && (source.control=="Stun" || source.control=="Charm")) { Log("Cast blocked by "+source.control); return false; }
            bool move=(source.champion=="Ezreal"&&slot==2)||(source.champion=="Ahri"&&slot==3);
            if(move&&!Mobility(source)) { Log("Mobility blocked by "+source.control); return false; }
            source.mana-=ChampionKit.Mana(source.champion,slot);
            source.ready[slot]=time+ChampionKit.Cooldown(source.champion,slot)/(1+source.haste/100);
            if(source.champion=="Ahri"&&slot==3&&source.rushCharges<=0) source.rushCharges=3;
            if(source.champion=="Mel")source.searing=Math.Min(9,source.searing+3);
            Log(source.champion+" casts "+Names(source.champion)[slot]);
            return true;
        }
        public void ResolveCast(Fighter source,Fighter target,int slot)
        {
            float p=source.ap;
            if(source.champion=="Morgana")
            {
                if(slot==0) Projectile(source,target,160+.9f*p,false,"Root",2);
                if(slot==1) target.poolUntil=time+5;
                if(slot==2) { source.shield=210+0.7f*p; source.shieldUntil=time+5; }
                if(slot==3) { Damage(source,target,200+0.7f*p,false); target.tetherUntil=time+3; }
            }
            if(source.champion=="Ahri")
            {
                if(slot==0) { Damage(source,target,100+0.5f*p,false); target.hp=Math.Max(0,target.hp-100-0.5f*p); }
                if(slot==1) Damage(source,target,144+0.72f*p,false);
                if(slot==2) Projectile(source,target,160+.85f*p,false,"Charm",1.5f);
                if(slot==3) { source.x=Approach(source.x,target.x,0.14f); Damage(source,target,125+0.35f*p,false); if(--source.rushCharges>0) source.ready[slot]=time+1; }
                if(target.hp<=0) source.hp=Math.Min(source.maxHp,source.hp+120+0.3f*p);
            }
            if(source.champion=="Ezreal")
            {
                if(slot==0&&Projectile(source,target,80+1.3f*source.ad+.15f*p,true))for(int i=0;i<4;i++)source.ready[i]=Math.Max(time,source.ready[i]-1.5f);
                if(slot==1) target.markUntil=time+4;
                if(slot==2) { source.x=Math.Min(0.94f,source.x+0.16f); Damage(source,target,180+0.5f*source.ad+0.75f*p,false); }
                if(slot==3) Damage(source,target,500+source.ad+0.9f*p,false);
                if(slot!=1 && target.markUntil>time) { target.markUntil=0; Damage(source,target,180+0.9f*p,false); Log("Essence Flux detonates"); }
            }
            if(source.champion=="Mel")
            {
                if(slot==0){for(int i=0;i<6;i++)Projectile(source,target,22+.16f*p,false);source.ready[0]=Math.Max(time,source.ready[0]-.75f);}
                if(slot==1){source.novaUntil=time+.75f;source.barrier=180+.7f*p;source.barrierUntil=time+2;source.speedUntil=time+2;}
                if(slot==2){if(Projectile(source,target,100+.4f*p,false,"Root",1.5f))target.slowUntil=time+3;}
                if(slot==3)MelUltimate(source,target);
            }
        }
        public static string[] Names(string c)
        {return ChampionKit.Names(c);}
    }
}
