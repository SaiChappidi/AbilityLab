using UnityEngine;

namespace Dex
{
    public partial class DexArena
    {
        void CastMel(Unit u,int slot,Vector2 target)
        {
            var f=u.f;
            if(slot==0)
            {
                var volley=new Volley();Vector2 centre=u.p+Vector2.ClampMagnitude(target-u.p,8);
                for(int n=0;n<6;n++)
                {
                    Vector2 point=centre+new Vector2(Mathf.Cos(n*Mathf.PI/3),Mathf.Sin(n*Mathf.PI/3))*.4f;
                    Shoot(u,0,point,22+.16f*f.ap);var shot=shots[shots.Count-1];shot.burst=true;shot.destination=point;shot.range=Vector2.Distance(u.p,point);shot.volley=volley;
                }
                Effect(MobaArt.Ring,centre,new Color(1,.8f,.3f,.5f),.55f,2.3f);
            }
            if(slot==1)
            {
                f.novaUntil=sim.time+.75f;
                foreach(var ally in units)if(ally.red==u.red&&ally.f.hp>0&&Vector2.Distance(ally.p,u.p)<2.4f)
                {ally.f.barrier=180+.7f*f.ap;ally.f.barrierUntil=sim.time+2;ally.f.speedUntil=sim.time+2;}
                Effect(MobaArt.Ring,u.p,Gold,.75f,4.8f);sim.Log("Rebuttal nova active: missile destruction, not reflection");
            }
            if(slot==2)Shoot(u,2,target,100+.4f*f.ap);
            if(slot==3)
            {
                foreach(var victim in units)if(victim.red!=u.red&&victim.f.hp>0&&victim.f.MarkedBy(f,sim.time))
                {float old=victim.f.hp;sim.MelUltimate(f,victim.f);TrackHit(u,victim,old-victim.f.hp);Effect(MobaArt.Ring,victim.p,Gold,.65f,3);Effect(MobaArt.Disc,victim.p,new Color(1,.85f,.45f,.6f),.25f,1.7f);}
            }
        }
        float NovaRadius(Fighter f){return Mathf.Lerp(.7f,2.4f,Mathf.Clamp01(1-(f.novaUntil-sim.time)/.75f));}
        void BurstVolley(Shot shot)
        {
            Effect(MobaArt.Ring,shot.destination,Gold,.35f,2.2f);
            foreach(var victim in units)if(victim.red!=shot.owner.red&&victim.f.hp>0&&Vector2.Distance(victim.p,shot.destination)<1.15f)
            {
                Hit(shot.owner,victim,shot.damage,false);
                if(!victim.minion&&!victim.tower&&!shot.volley.refunded){shot.owner.f.ready[0]=Mathf.Max(sim.time,shot.owner.f.ready[0]-.75f);shot.volley.refunded=true;sim.Log("Radiant Volley champion hit: cooldown refund");}
            }
        }
        void SolarBurst(Unit owner,Vector2 point,float travel)
        {
            foreach(var victim in units)if(victim.red!=owner.red&&victim.f.hp>0&&!victim.tower&&Vector2.Distance(victim.p,point)<.75f)
                sim.CC(victim.f,"Root",Mathf.Lerp(1,2.2f,Mathf.Clamp01(travel/8)));
            zones.Add(new Zone{owner=owner,p=point,solar=true,until=sim.time+2.5f,art=Art("Solar field",MobaArt.Disc,point,Vector2.one*3,new Color(.9f,.65f,.12f,.23f),-45)});
            Effect(MobaArt.Ring,point,Gold,.55f,3);
        }
        void ResolveNovaEffects()
        {
            foreach(var mel in units)if(mel.f.champion=="Mel")
            {
                if(mel.f.hp>0&&mel.f.novaUntil>sim.time)
                    foreach(var victim in units)if(victim.red!=mel.red&&victim.f.hp>0&&Vector2.Distance(victim.p,mel.p)<NovaRadius(mel.f)&&(victim.f.shield>0||victim.f.barrier>0))
                    {victim.f.shield=0;victim.f.barrier=0;Effect(MobaArt.Ring,victim.p,Gold,.3f,1.5f);sim.Log("Rebuttal breaks an enemy shield");}
                sim.ResolveRetaliations(mel.f,(from,to,amount)=>
                {var victim=units.Find(v=>v.f==to&&v.f.hp>0);if(victim!=null){Shoot(mel,-3,victim.p,amount,false,false,victim);sim.Log("Rebuttal retaliates against "+to.champion);}});
            }
        }
    }
}
