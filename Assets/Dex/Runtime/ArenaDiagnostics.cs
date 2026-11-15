#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Dex
{
    public partial class DexArena
    {
        // Developer-only smoke checks execute the actual arena methods, including swept collision.
        // These are additional integration checks, not the protected independent rule oracle.
        public string RunSmokeChecks()
        {
            var lines=new List<string>();int passed=0,total=0;
            System.Action<string,bool> check=(name,ok)=>{total++;if(ok)passed++;lines.Add((ok?"PASS ":"FAIL ")+name);};
            bool oldBot=bot,oldWaves=waves,oldPractice=practice;
            bot=waves=practice=false;champion=2;opponent=1;ResetWorld();
            player.p=player.goal=new Vector2(-2,0);enemy.p=enemy.goal=new Vector2(2,0);
            player.goal=player.p+Vector2.up;Vector2 before=player.p;Step(player,.1f);check("movement approaches right-click goal",player.p.y>before.y);player.goal=player.p;
            sim.CC(player.f,"Root",2);before=player.p;player.goal+=Vector2.up;Step(player,.1f);check("root blocks walking",player.p==before);player.f.controlUntil=0;player.f.control="";
            player.target=enemy;float hp=enemy.f.hp;Step(player,.01f);check("attack creates targeted projectile",shots.Count==1&&shots[0].homing==enemy);StepShots(.5f);check("targeted attack damages enemy",enemy.f.hp<hp);player.target=null;
            ClearShots();Cast(player,0,player.p+Vector2.up*4);check("Q aims at supplied cursor",shots.Count==1&&shots[0].dir.y>.99f);float mana=player.mana,ready=player.f.ready[0];int count=shots.Count;Cast(player,0,Vector2.up);check("cooldown rejects recast without mana charge",shots.Count==count&&player.mana==mana&&player.f.ready[0]==ready);
            player.mana=0;ready=player.f.ready[1];Cast(player,1,enemy.p);check("insufficient mana preserves cooldown",player.f.ready[1]==ready);player.mana=900;
            ClearShots();var blocker=Spawn(new Fighter("Minion",false,0){hp=420,maxHp=420},new Vector2(0,0),true,true);player.p=new Vector2(-2,0);enemy.p=new Vector2(2,0);hp=enemy.f.hp;
            Shoot(player,0,enemy.p,100,true);StepShots(.5f);check("skillshot hits nearer minion before farther champion",blocker.f.hp<420&&enemy.f.hp==hp);units.Remove(blocker);Destroy(blocker.art.gameObject);Destroy(blocker.shadow.gameObject);
            ClearShots();Cast(enemy,0,player.p);StepShots(.5f);check("swept Morgana Q collision applies root",player.f.control=="Root"&&player.f.ActiveControl(sim.time));
            before=player.p;mana=player.mana;ready=player.f.ready[2];Cast(player,2,player.p+Vector2.up);check("rooted E preserves position mana and cooldown",player.p==before&&player.mana==mana&&player.f.ready[2]==ready);
            sim.time=2;player.f.control="";player.f.controlUntil=0;before=player.p;Cast(player,2,player.p+Vector2.up);check("post-expiry E blinks",player.p!=before);
            ClearShots();enemy.f.ready[2]=0;Cast(enemy,2,enemy.p);sim.CC(enemy.f,"Charm",1);check("active Black Shield rejects control",!enemy.f.ActiveControl(sim.time));
            sim.time+=6;Step(enemy,.01f);sim.CC(enemy.f,"Charm",1);check("expired shield permits control",enemy.f.ActiveControl(sim.time));
            before=enemy.p;Step(enemy,.2f);check("charm moves toward opposing champion",Vector2.Distance(enemy.p,player.p)<Vector2.Distance(before,player.p));
            var tower=units.Find(u=>u.tower);before=tower.p;Step(tower,1);check("turret remains stationary",tower.p==before);
            ClearShots();enemy.p=enemy.goal=tower.p+Vector2.right*2;hp=enemy.f.hp;Step(tower,.01f);bool fired=shots.Exists(s=>s.owner==tower);StepShots(.5f);check("turret acquires and damages opposing target",fired&&enemy.f.hp<hp);
            Wave(true);var minion=units.Find(u=>u.minion);before=minion.p;Step(minion,.2f);check("minion advances lane",minion.p!=before);ClearShots();minion.p=player.p+Vector2.right;hp=player.f.hp;Step(minion,.01f);StepShots(.2f);check("minion acquires and damages opposing target",player.f.hp<hp);Hit(player,minion,100000,true);check("minion death rewards CS",cs==1&&minion.respawn>0);
            Hit(enemy,player,100000,true);check("lethal damage starts respawn",player.f.hp==0&&player.respawn>sim.time);before=player.p;Cast(player,3,enemy.p);check("dead caster cannot cast",player.p==before);
            sim.time=player.respawn;Step(player,.01f);check("respawn resets health mana control and cooldowns",player.f.hp==player.f.maxHp&&player.mana==900&&player.f.control==""&&player.f.ready[2]==0&&player.p==player.home);
            champion=0;ResetWorld();bot=waves=practice=false;for(int i=0;i<3;i++){sim.time=i;Cast(player,3,player.p+Vector2.up);}check("Ahri R consumes exactly three charges",player.f.rushCharges==0&&player.f.ready[3]>sim.time+10);sim.time=player.f.ready[3];Cast(player,3,player.p+Vector2.up);check("Ahri R refreshes charges after ultimate cooldown",player.f.rushCharges==2);
            champion=1;ResetWorld();bot=waves=practice=false;enemy.p=Vector2.zero;hp=enemy.f.hp;Cast(player,1,Vector2.zero);check("Morgana W creates zone",zones.Count==1);StepZones(.5f);check("Morgana W damages units inside its zone",enemy.f.hp<hp);sim.time=6;StepZones(.01f);check("expired zone removed",zones.Count==0);
            champion=3;opponent=1;ResetWorld();bot=waves=practice=false;player.p=player.goal=new Vector2(-2,0);enemy.p=enemy.goal=new Vector2(2,0);
            hp=enemy.f.hp;Cast(player,0,enemy.p);check("Mel Q launches six aimed burst missiles",shots.Count==6&&shots.TrueForAll(s=>s.burst));StepShots(.6f);check("Mel Q bursts build Overwhelm",enemy.f.hp<hp&&enemy.f.overwhelm>=6);check("Mel cast banks Searing Brilliance bolts",player.f.searing==3);
            ClearShots();enemy.p=enemy.goal=player.p+Vector2.right;enemy.f.shield=300;enemy.f.shieldUntil=10;hp=enemy.f.hp;Cast(player,1,player.p);Cast(enemy,0,player.p);
            for(int i=0;i<55;i++){sim.time+=.025f;StepShots(.025f);StepZones(.025f);}
            check("Mel W destroys a real incoming binding without root",!player.f.ActiveControl(sim.time)&&sim.events.Exists(e=>e.Contains("Rebuttal destroys")));check("Mel W breaks nearby enemy shields",enemy.f.shield==0);check("Mel W retaliates with real damage",enemy.f.hp<hp);check("Mel W barrier grants temporary speed",player.f.barrier>0&&player.f.speedUntil>sim.time);
            ClearShots();Cast(player,2,enemy.p);StepShots(.2f);StepZones(.05f);check("Mel E applies root and solar slow field",enemy.f.ActiveControl(sim.time)&&zones.Exists(z=>z.solar)&&enemy.f.slowUntil>sim.time);
            hp=enemy.f.hp;Cast(player,3,enemy.p);check("Mel R detonates real arena marks",enemy.f.hp<hp&&enemy.f.overwhelm==0);
            enemy.f.hp=enemy.f.maxHp;player.f.searing=9;player.target=enemy;player.attackAt=0;ClearShots();Step(player,.01f);check("Mel empowered attack consumes nine banked bolts",shots.Count==10&&player.f.searing==0);
            champion=2;opponent=1;ResetWorld();check("reset clears projectiles zones effects scores and resources",shots.Count==0&&zones.Count==0&&fx.Count==0&&kills==0&&deaths==0&&cs==0&&player.mana==900);
            bot=oldBot;waves=oldWaves;practice=oldPractice;
            return "Arena integration smoke checks: "+passed+"/"+total+"\n"+string.Join("\n",lines);
        }
        void ClearShots(){foreach(var shot in shots)Destroy(shot.art.gameObject);shots.Clear();}
    }
}
#endif
