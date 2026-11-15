using System;
using System.Collections.Generic;

namespace Dex
{
    [Serializable] public class Fighter
    {
        public string champion, build;
        public float hp = 1800, maxHp = 1800, ap, ad = 110, armor = 60, mr = 40, haste;
        public float x, controlUntil, shield, shieldUntil, markUntil, poolUntil, tetherUntil;
        public string control = "";
        public int stacks, rushCharges = 3;
        public float[] ready = new float[4];
        public Fighter(string name, bool equipped, float position)
        {
            champion = name; x = position;
            build = equipped ? (name == "Ezreal" ? "Trinity Force / Muramana" : name == "Ahri" ? "Malignance / Rabadon's Deathcap" : "Liandry's Torment / Zhonya's Hourglass") : "No items";
            // Demonstration aggregate stats; item procs and exact live balance are not modeled.
            if(equipped) { ap = name == "Ezreal" ? 0 : 230; ad += name == "Ezreal" ? 100 : 0; haste = 30; }
        }
        public bool ActiveControl(float t) { return hp > 0 && controlUntil > t; }
        public bool Shielded(float t) { return shield > 0 && shieldUntil > t; }
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
                if(f.hp <= 0) continue;
                if(f.control == "Charm") f.x = Approach(f.x, enemy.x, dt * 0.025f);
                if(f.poolUntil > time) Damage(enemy, f, (35 + enemy.ap * 0.1f) * dt, false);
                if(f.tetherUntil > 0 && time >= f.tetherUntil)
                { f.tetherUntil = 0; if(Math.Abs(f.x - enemy.x) < 0.55f) { Damage(enemy,f,200+enemy.ap*0.7f,false); CC(f,"Stun",1.5f); Log("Soul Shackles detonates"); } else Log("Soul Shackles tether broken"); }
            }
        }
        static float Approach(float v,float target,float step) { return v < target ? Math.Min(v+step,target) : Math.Max(v-step,target); }
        public void Damage(Fighter source, Fighter target, float amount, bool physical)
        {
            float dealt = amount * 100f / (100f + (physical ? target.armor : target.mr));
            if(!physical && target.Shielded(time)) { float absorbed = Math.Min(target.shield,dealt); target.shield -= absorbed; dealt -= absorbed; }
            target.hp = Math.Max(0,target.hp-dealt);
            if(source.champion == "Morgana") source.hp = Math.Min(source.maxHp,source.hp+dealt*0.18f);
            if(source.champion == "Ezreal") source.stacks = Math.Min(5,source.stacks+1);
        }
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
            if(slot<0 || slot>3 || source.hp<=0 || time<source.ready[slot]) return false;
            if(source.ActiveControl(time) && (source.control=="Stun" || source.control=="Charm")) { Log("Cast blocked by "+source.control); return false; }
            bool move=(source.champion=="Ezreal"&&slot==2)||(source.champion=="Ahri"&&slot==3);
            if(move&&!Mobility(source)) { Log("Mobility blocked by "+source.control); return false; }
            source.ready[slot]=time+new[]{7f,9f,14f,80f}[slot]/(1+source.haste/100);
            if(source.champion=="Ahri"&&slot==3&&source.rushCharges<=0) source.rushCharges=3;
            Log(source.champion+" casts "+Names(source.champion)[slot]);
            return true;
        }
        public void ResolveCast(Fighter source,Fighter target,int slot)
        {
            float p=source.ap;
            if(source.champion=="Morgana")
            {
                if(slot==0) { Damage(source,target,160+0.9f*p,false); CC(target,"Root",2); }
                if(slot==1) target.poolUntil=time+5;
                if(slot==2) { source.shield=210+0.7f*p; source.shieldUntil=time+5; }
                if(slot==3) { Damage(source,target,200+0.7f*p,false); target.tetherUntil=time+3; }
            }
            if(source.champion=="Ahri")
            {
                if(slot==0) { Damage(source,target,100+0.5f*p,false); target.hp=Math.Max(0,target.hp-100-0.5f*p); }
                if(slot==1) Damage(source,target,144+0.72f*p,false);
                if(slot==2) { Damage(source,target,160+0.85f*p,false); CC(target,"Charm",1.5f); }
                if(slot==3) { source.x=Approach(source.x,target.x,0.14f); Damage(source,target,125+0.35f*p,false); if(--source.rushCharges>0) source.ready[slot]=time+1; }
                if(target.hp<=0) source.hp=Math.Min(source.maxHp,source.hp+120+0.3f*p);
            }
            if(source.champion=="Ezreal")
            {
                if(slot==0) { Damage(source,target,80+1.3f*source.ad+0.15f*p,true); for(int i=0;i<4;i++) source.ready[i]=Math.Max(time,source.ready[i]-1.5f); }
                if(slot==1) target.markUntil=time+4;
                if(slot==2) { source.x=Math.Min(0.94f,source.x+0.16f); Damage(source,target,180+0.5f*source.ad+0.75f*p,false); }
                if(slot==3) Damage(source,target,500+source.ad+0.9f*p,false);
                if(slot!=1 && target.markUntil>time) { target.markUntil=0; Damage(source,target,180+0.9f*p,false); Log("Essence Flux detonates"); }
            }
        }
        public static string[] Names(string c)
        { return c=="Ahri" ? new[]{"Orb of Deception","Fox-Fire","Charm","Spirit Rush"} : c=="Morgana" ? new[]{"Dark Binding","Tormented Shadow","Black Shield","Soul Shackles"} : new[]{"Mystic Shot","Essence Flux","Arcane Shift","Trueshot Barrage"}; }
    }
}
