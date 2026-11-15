using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dex
{
    public partial class DexArena : MonoBehaviour
    {
        class Unit
        {
            public Fighter f; public Vector2 p,goal,home; public SpriteRenderer art,shadow;
            public bool red,minion,tower; public float mana{get{return f.mana;}set{f.mana=value;}}public float attackAt,respawn,hitAt=-10;public SpriteRenderer rim,aura; public Unit target;
            public float radius {get{return tower?.7f:minion?.22f:.38f;}}
        }
        class Shot
        {
            public Unit owner;public Vector2 p,dir;public float range=8,speed=11,damage,width=.2f;
            public int slot;public Unit homing;public bool physical,orb,back,burst,solarBloomed;public Vector2 destination;public Volley volley;public SpriteRenderer art;public HashSet<Unit> hit=new HashSet<Unit>();
        }
        class Volley {public bool refunded;}
        class Zone {public Unit owner;public Vector2 p;public float until,nextTick;public bool solar;public SpriteRenderer art;}
        class Fx {public SpriteRenderer art;public float born,life,size;}
        class Pop {public Vector2 p;public float born;public string text;public Color color;}
        Combat sim;Unit player,enemy;Transform world;Camera cam;SpriteRenderer targetRing;
        List<Unit> units=new List<Unit>();List<Shot> shots=new List<Shot>();List<Zone> zones=new List<Zone>();List<Fx> fx=new List<Fx>();List<Pop> pops=new List<Pop>();
        readonly List<Unit> collisionOrder=new List<Unit>();
        readonly string[] champions=ChampionKit.Roster;int champion=3,opponent=2,kills,deaths,cs,gold=500;
        bool bot=true,waves=true,items=true,practice=true,menu;
        // The Editor's deterministic driver calls the same gameplay tick as Update.
        bool manualSimulation;float nextWave,botAt;Vector2 aim,lastArenaAim;
        string notice="Left-click an enemy to auto-attack. Right-click to move or attack. Aim Q / W / E / R.";float noticeUntil=8;
        static readonly Color Cyan=new Color(.27f,.85f,.94f),Gold=new Color(.83f,.7f,.38f),Pink=new Color(.96f,.39f,.55f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindAnyObjectByType<DexArena>()==null)new GameObject("Dex MOBA Sandbox").AddComponent<DexArena>();}
        void Awake()
        {
            cam=Camera.main;if(cam==null)cam=new GameObject("Sandbox Camera").AddComponent<Camera>();
            cam.transform.position=new Vector3(0,0,-10);cam.transform.rotation=Quaternion.identity;cam.orthographic=true;cam.orthographicSize=8.2f;cam.backgroundColor=new Color(.018f,.028f,.05f);SetupFeel();ResetWorld();
        }
        void ResetWorld()
        {
            notice="Left-click enemy: auto-attack. Aim Q / W / E / R. Esc opens the sandbox.";noticeUntil=4;
            replayStep=-1;replayResult="F6: replay Morgana root > Ezreal E";
            if(world!=null)Destroy(world.gameObject);world=new GameObject("Sandbox World").transform;
            units.Clear();shots.Clear();zones.Clear();fx.Clear();pops.Clear();sim=new Combat(champions[champion],champions[opponent],items);
            Map();player=Spawn(sim.a,new Vector2(-6,-2),false);enemy=Spawn(sim.b,new Vector2(6,2),true);
            Tower(new Vector2(-10,-3.5f),false);Tower(new Vector2(10,3.5f),true);nextWave=2;botAt=3;kills=deaths=cs=0;gold=500;
            targetRing=Art("Auto-attack target",MobaArt.Ring,Vector2.zero,Vector2.one*1.9f,Pink,75);targetRing.enabled=false;
        }
        SpriteRenderer Art(string name,Sprite sprite,Vector2 p,Vector2 scale,Color color,int layer)
        {var go=new GameObject(name);go.transform.SetParent(world);go.transform.position=p;go.transform.localScale=scale;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=layer;return r;}
        Unit Spawn(Fighter f,Vector2 p,bool red,bool minion=false)
        {
            var u=new Unit{f=f,p=p,goal=p,home=p,red=red,minion=minion};u.shadow=Art("Shadow",MobaArt.Disc,p+Vector2.down*.28f,new Vector2(.9f,.38f),new Color(0,0,0,.4f),10);
            u.art=Art(f.champion,minion?MobaArt.Minion:MobaArt.Champion(f.champion),p,Vector2.one*(minion?.65f:1.5f),Color.white,100);AddUnitVisuals(u);units.Add(u);return u;
        }
        void Tower(Vector2 p,bool red){var u=Spawn(new Fighter("Turret",false,0){hp=4000,maxHp=4000,armor=90,mr=90},p,red);u.tower=true;u.art.sprite=MobaArt.Tower;u.art.transform.localScale=Vector3.one*2.4f;}
        void Map()
        {
            Art("Test chamber",MobaArt.Square,Vector2.zero,new Vector2(32,19),new Color(.024f,.04f,.065f),-100);
            Art("Inner board",MobaArt.Square,Vector2.zero,new Vector2(27.5f,14.2f),new Color(.035f,.065f,.095f),-99);
            for(int x=-13;x<=13;x++)Art("Grid",MobaArt.Square,new Vector2(x,0),new Vector2(x%5==0?.018f:.008f,13.5f),new Color(.12f,.24f,.3f,x%5==0?.45f:.28f),-95);
            for(int y=-6;y<=6;y++)Art("Grid",MobaArt.Square,new Vector2(0,y),new Vector2(26.5f,y%3==0?.018f:.008f),new Color(.12f,.24f,.3f,y%3==0?.45f:.28f),-95);
            var lane=Art("Interaction lane",MobaArt.Square,Vector2.zero,new Vector2(27,3),new Color(.045f,.095f,.13f),-94);lane.transform.rotation=Quaternion.Euler(0,0,18);
            for(int side=-1;side<=1;side+=2)
            {
                Color team=side<0?Cyan:Pink;Vector2 p=new Vector2(side*11,side*3.7f);
                Art("Base pad",MobaArt.Disc,p,Vector2.one*4.8f,new Color(team.r,team.g,team.b,.08f),-88);
                Art("Base perimeter",MobaArt.Ring,p,Vector2.one*4.8f,new Color(team.r,team.g,team.b,.4f),-87);
                Art("Core",MobaArt.Crystal,p,Vector2.one*1.45f,team,25);
                Art("Chamber border",MobaArt.Square,new Vector2(0,side*6.9f),new Vector2(27.5f,.035f),new Color(.25f,.45f,.53f,.6f),-85);
            }
            Art("Centre marker",MobaArt.Ring,Vector2.zero,Vector2.one*3.3f,new Color(.4f,.7f,.8f,.12f),-87);
            Art("Centre marker inner",MobaArt.Ring,Vector2.zero,Vector2.one*3,new Color(.4f,.7f,.8f,.12f),-87);
        }
        void Update()
        {
            if(manualSimulation)return;
            var kb=Keyboard.current;var mouse=Mouse.current;if(kb!=null&&kb.f6Key.wasPressedThisFrame)StartInteractionReplay();if(kb!=null&&kb.escapeKey.wasPressedThisFrame)menu=!menu;if(menu)return;
            float dt=Mathf.Min(Time.deltaTime,.04f);Vector2 mp=mouse!=null?mouse.position.ReadValue():new Vector2(Screen.width/2,Screen.height/2);aim=cam.ScreenToWorldPoint(mp);
            bool hud=mp.y<Screen.height*.19f||mp.y>Screen.height*.92f;
            // GUI overlays must not issue arena orders when clicked.
            Vector2 ui=new Vector2(mp.x/Screen.width*1600,(1-mp.y/Screen.height)*900);
            hud|=new Rect(25,88,620,100).Contains(ui)||(trace&&new Rect(1240,85,335,375).Contains(ui));
            if(!hud)lastArenaAim=aim;
            if(mouse!=null&&!hud&&player.f.hp>0&&!ReplayRunning)
            {
                if(mouse.rightButton.wasPressedThisFrame)IssuePointerOrder(aim,true);
                else if(mouse.leftButton.wasPressedThisFrame)IssuePointerOrder(aim,false);
            }
            if(kb!=null&&player.f.hp>0&&!hud&&!ReplayRunning)
            {
                if(kb.qKey.wasPressedThisFrame)Cast(player,0,aim);if(kb.wKey.wasPressedThisFrame)Cast(player,1,aim);if(kb.eKey.wasPressedThisFrame)Cast(player,2,aim);if(kb.rKey.wasPressedThisFrame)Cast(player,3,aim);
                if(kb.aKey.wasPressedThisFrame)player.target=Nearest(player.p,true,6);
                if(kb.sKey.wasPressedThisFrame){player.goal=player.p;player.target=null;}
            }
            TickSimulation(dt);
        }
        void TickSimulation(float dt)
        {
            sim.time+=dt;
            if(waves&&sim.time>nextWave){nextWave=sim.time+18;Wave(false);Wave(true);}
            foreach(var u in units.ToArray())Step(u,dt);StepShots(dt);StepZones(dt);
            targetRing.enabled=player.target!=null&&player.target.f.hp>0;
            if(targetRing.enabled){targetRing.transform.position=player.target.p;targetRing.transform.localScale=Vector3.one*(player.target.tower?2.6f:player.target.minion?1:1.9f);}
            StepReplay();
            for(int i=units.Count-1;i>=0;i--){var u=units[i];if(u.minion&&u.f.hp<=0&&sim.time>u.respawn){Destroy(u.art.gameObject);Destroy(u.shadow.gameObject);units.RemoveAt(i);}}
            for(int i=fx.Count-1;i>=0;i--){var f=fx[i];float t=(sim.time-f.born)/f.life;if(t>=1){Destroy(f.art.gameObject);fx.RemoveAt(i);}else {f.art.transform.localScale=Vector3.one*f.size*(.5f+t);var c=f.art.color;c.a=1-t;f.art.color=c;}}
            pops.RemoveAll(p=>sim.time-p.born>1.1f);
        }
        Vector2 Clamp(Vector2 p){return new Vector2(Mathf.Clamp(p.x,-13,13),Mathf.Clamp(p.y,-6.5f,6.5f));}
        Unit Nearest(Vector2 p,bool red,float range){Unit best=null;foreach(var u in units)if(u.red==red&&u.f.hp>0&&Vector2.Distance(p,u.p)<range){best=u;range=Vector2.Distance(p,u.p);}return best;}
        void IssuePointerOrder(Vector2 point,bool moveOnGround)
        {
            Unit selected=null;float closest=float.MaxValue;
            foreach(var u in units)
            {
                if(u.red==player.red||u.f.hp<=0)continue;
                // Match the visible token/body instead of requiring a click on a tiny centre point.
                Vector2 visual=(Vector2)u.art.bounds.ClosestPoint(new Vector3(point.x,point.y,0));
                float distance=Vector2.Distance(point,visual);
                if(distance<=.18f&&Vector2.Distance(point,u.p)<closest){selected=u;closest=Vector2.Distance(point,u.p);}
            }
            if(selected!=null){player.target=selected;player.goal=selected.p;Effect(MobaArt.Ring,selected.p,Pink,.4f,selected.tower?2.6f:1.8f);Say("Auto-attacking "+selected.f.champion+". S to stop.");}
            else if(moveOnGround){player.target=null;player.goal=Clamp(point);Effect(MobaArt.Ring,player.goal,Cyan,.4f,.5f);}
        }
        void Wave(bool red){for(int i=0;i<4;i++)Spawn(new Fighter("Minion",false,0){hp=420,maxHp=420,ad=32,armor=15,mr=15},new Vector2(red?11:-11,(red?3.5f:-3.5f)+(i-1.5f)*.43f),red,true);}
        void Step(Unit u,float dt)
        {
            if(u.f.hp<=0){u.art.enabled=u.shadow.enabled=false;if(u.rim!=null)u.rim.enabled=false;if(u.aura!=null)u.aura.enabled=false;if(!u.minion&&!u.tower&&u.respawn>0&&sim.time>=u.respawn){u.f=new Fighter(u.f.champion,items,0);if(u==player)sim.a=u.f;else sim.b=u.f;u.mana=900;u.p=u.home;u.goal=u.p;u.target=null;u.attackAt=sim.time;u.respawn=0;u.art.enabled=u.shadow.enabled=true;if(u.rim!=null)u.rim.enabled=true;}return;}
            if(u.f.controlUntil<=sim.time)u.f.control="";if(u.f.shieldUntil<=sim.time)u.f.shield=0;
            if(u.f.barrierUntil<=sim.time)u.f.barrier=0;if(u.f.overwhelmUntil<=sim.time)u.f.overwhelm=0;
            if(u==enemy&&bot){float d=Vector2.Distance(u.p,player.p);u.goal=d>5?player.p:d<2.6f?u.p+(u.p-player.p).normalized*2:u.p;u.target=d<5&&player.f.hp>0?player:null;if(player.f.hp>0&&sim.time>botAt&&d<8){botAt=sim.time+2;Cast(u,Random.Range(0,4),player.p);}}
            if(u.minion){u.target=Nearest(u.p,!u.red,3.4f);u.goal=u.target!=null?u.target.p:new Vector2(u.red?-12:12,u.red?-4:4);}
            if(u.tower)u.target=Nearest(u.p,!u.red,5.8f);
            if(u.target!=null&&u.target.f.hp<=0){u.target=null;u.goal=u.p;}
            if(u.target!=null&&u.target.f.hp>0)
            {
                float d=Vector2.Distance(u.p,u.target.p),range=u.tower?5.8f:u.minion?2.2f:4.8f;
                if(d>range&&!u.tower)u.goal=u.target.p;
                else if(d<=range){u.goal=u.p;bool disabled=u.f.ActiveControl(sim.time)&&(u.f.control=="Stun"||u.f.control=="Charm");if(sim.time>=u.attackAt&&!disabled){u.attackAt=sim.time+(u.tower?1.2f:u.minion?1.4f:.9f);Shoot(u,-1,u.target.p,u.tower?175:u.f.ad,true,false,u.target);if(u.f.champion=="Mel"){int bolts=u.f.searing;u.f.searing=0;for(int n=0;n<bolts;n++)Shoot(u,-2,u.target.p,20+.04f*u.f.ap,false,false,u.target);}if(u==player)PlayCue(0);}}
            }
            if(!u.tower&&(!u.f.ActiveControl(sim.time)||u.f.control=="Ground"))
            {
                Vector2 travel=u.goal-u.p;
                if(!u.minion&&travel.sqrMagnitude>.02f){u.art.sprite=MobaArt.Frame(u.f.champion,sim.time);u.art.flipX=travel.x<0&&u.art.sprite!=MobaArt.Portrait(u.f.champion);}
                u.p=Vector2.MoveTowards(u.p,u.goal,dt*(u.minion?1.7f:3.3f)*(u.f.speedUntil>sim.time?1.2f:1)*(u.f.slowUntil>sim.time?.7f:1));
                foreach(var v in units)if(v!=u&&v.f.hp>0){Vector2 away=u.p-v.p;float min=(u.radius+v.radius)*.8f;if(away.sqrMagnitude<min*min&&away.sqrMagnitude>.0001f)u.p+=away.normalized*dt*1.5f;}
            }
            if(u.f.control=="Charm")u.p=Vector2.MoveTowards(u.p,u==player?enemy.p:player.p,dt*.8f);
            u.p=Clamp(u.p);u.f.x=u.p.x/25;u.art.transform.position=u.p+Vector2.up*(u.minion?0:.025f*Mathf.Sin(sim.time*5));u.shadow.transform.position=u.p+Vector2.down*.3f;u.art.sortingOrder=100-(int)(u.p.y*10);
            u.art.color=sim.time-u.hitAt<.1f?new Color(1.6f,1.6f,1.6f):u.red?new Color(1,.85f,.9f):Color.white;
            UpdateUnitVisuals(u);
            if(!u.minion&&!u.tower){u.mana=Mathf.Min(900,u.mana+dt*(practice?35:6));if(practice)u.f.hp=Mathf.Min(u.f.maxHp,u.f.hp+dt*12);}
        }
        void Say(string s){notice=s;noticeUntil=sim.time+3;}
        void Cast(Unit u,int slot,Vector2 target)
        {
            if(slot<0||slot>3)return;var f=u.f;float cost=ChampionKit.Mana(f.champion,slot);if(u.mana<cost){if(u==player)Say("Not enough mana");return;}
            Unit castTarget=u==player?enemy:player;
            if(f.champion=="Mel"&&slot==3){var marked=units.Find(v=>v.red!=u.red&&v.f.hp>0&&v.f.MarkedBy(f,sim.time));if(marked!=null)castTarget=marked;}
            if(!sim.BeginCast(f,castTarget.f,slot)){if(u==player)Say(f.ActiveControl(sim.time)?"Cannot cast while "+f.control:f.champion=="Mel"&&slot==3?"Golden Eclipse needs an Overwhelm mark":"Ability is on cooldown");return;}
            if(u==player)PlayCue(1);
            Vector2 dir=(target-u.p).normalized;if(dir.sqrMagnitude<.1f)dir=Vector2.right;
            if(f.champion=="Ahri")
            {
                if(slot==0)Shoot(u,slot,target,100+.5f*f.ap,false,true);
                if(slot==1)for(int i=0;i<3;i++){var v=Nearest(u.p,!u.red,6);if(v!=null)Shoot(u,slot,v.p,(i==0?80:32)+f.ap*(i==0?.4f:.16f));}
                if(slot==2)Shoot(u,slot,target,160+.85f*f.ap);
                if(slot==3){Dash(u,dir);var v=Nearest(u.p,!u.red,6);if(v!=null)Shoot(u,slot,v.p,125+.35f*f.ap);if(--f.rushCharges>0)f.ready[3]=sim.time+1;}
            }
            else if(f.champion=="Morgana")
            {
                if(slot==0)Shoot(u,slot,target,160+.9f*f.ap);
                if(slot==1){Vector2 p=u.p+Vector2.ClampMagnitude(target-u.p,7);zones.Add(new Zone{owner=u,p=p,until=sim.time+5,art=Art("Tormented Shadow",MobaArt.Disc,p,Vector2.one*3,new Color(.35f,.1f,.55f,.65f),-50)});}
                if(slot==2){f.shield=210+.7f*f.ap;f.shieldUntil=sim.time+5;Effect(MobaArt.Ring,u.p,new Color(.7f,.3f,1),1,2);}
                if(slot==3)foreach(var v in units)if(v.red!=u.red&&v.f.hp>0&&Vector2.Distance(u.p,v.p)<5){Hit(u,v,200+.7f*f.ap,false);v.f.tetherUntil=sim.time+3;Effect(MobaArt.Ring,v.p,new Color(.8f,.35f,1),3,2);}
            }
            else if(f.champion=="Ezreal")
            {
                if(slot==0)Shoot(u,slot,target,80+1.3f*f.ad+.15f*f.ap,true);
                if(slot==1)Shoot(u,slot,target,0);
                if(slot==2){Dash(u,dir);var v=Nearest(u.p,!u.red,6);if(v!=null)Shoot(u,slot,v.p,180+.5f*f.ad+.75f*f.ap);}
                if(slot==3)Shoot(u,slot,target,500+f.ad+.9f*f.ap);
            }
            else if(f.champion=="Mel")CastMel(u,slot,target);
        }
        void Dash(Unit u,Vector2 dir){Vector2 from=u.p;u.p=Clamp(u.p+dir*2.8f);u.goal=u.p;u.target=null;for(int i=0;i<6;i++)Effect(MobaArt.Disc,Vector2.Lerp(from,u.p,i/5f),Cyan,.3f,.55f);}
        void Shoot(Unit owner,int slot,Vector2 target,float damage,bool physical=false,bool orb=false,Unit homing=null)
        {
            Vector2 dir=(target-owner.p).normalized;if(dir.sqrMagnitude<.01f)dir=Vector2.right;
            Color color=owner.f.champion=="Morgana"?new Color(.72f,.32f,1):owner.f.champion=="Ahri"?slot==2?Pink:Cyan:owner.f.champion=="Mel"?new Color(1,.82f,.38f):Gold;
            var m=new Shot{owner=owner,slot=slot,p=owner.p,dir=dir,damage=damage,physical=physical,range=slot==3?25:8,width=slot==3?.8f:.2f,orb=orb,homing=homing};
            m.art=Art("Skillshot",slot==2&&owner.f.champion=="Ahri"?MobaArt.Heart:MobaArt.Bolt,m.p,new Vector2(slot==3?2:.7f,slot==3?.8f:.35f),color,300);shots.Add(m);
        }
        static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){Vector2 d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude)));}
        void StepShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var m=shots[i];if(m.homing!=null&&m.homing.f.hp<=0){Destroy(m.art.gameObject);shots.RemoveAt(i);continue;}Vector2 prev=m.p;if(m.back)m.dir=(m.owner.p-m.p).normalized;else if(m.homing!=null)m.dir=(m.homing.p-m.p).normalized;m.p+=m.dir*m.speed*dt;m.range-=m.speed*dt;bool remove=false;
                foreach(var defender in units)if(defender.red!=m.owner.red&&defender.f.hp>0&&defender.f.champion=="Mel"&&defender.f.novaUntil>sim.time)
                {float radius=NovaRadius(defender.f);if(SegmentDistance(defender.p,prev,m.p)<radius&&sim.InterceptProjectile(defender.f,m.owner.f,true)){Effect(MobaArt.Ring,m.p,Gold,.35f,.7f);remove=true;break;}}
                if(remove){Destroy(m.art.gameObject);shots.RemoveAt(i);continue;}
                if(m.burst)
                {
                    if(m.range<=0||SegmentDistance(m.destination,prev,m.p)<.12f){BurstVolley(m);Destroy(m.art.gameObject);shots.RemoveAt(i);}else{m.art.transform.position=m.p;m.art.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(m.dir.y,m.dir.x)*Mathf.Rad2Deg);}continue;
                }
                collisionOrder.Clear();collisionOrder.AddRange(units);
                collisionOrder.Sort((a,b)=>Vector2.Dot(a.p-prev,m.dir).CompareTo(Vector2.Dot(b.p-prev,m.dir)));
                foreach(var u in collisionOrder)
                {
                    if(u.red==m.owner.red||u.f.hp<=0||m.hit.Contains(u)||m.homing!=null&&u!=m.homing||SegmentDistance(u.p,prev,m.p)>m.width+u.radius)continue;
                    if(m.owner.f.champion=="Ezreal"&&m.slot==1&&u.minion)continue;m.hit.Add(u);
                    if(m.owner.f.champion=="Ezreal"&&m.slot==1)u.f.markUntil=sim.time+4;
                    else
                    {
                        if(m.back){float old=u.f.hp;u.f.hp=Mathf.Max(0,u.f.hp-m.damage);TrackHit(m.owner,u,old-u.f.hp);}else Hit(m.owner,u,m.damage,m.physical);
                        if(m.owner.f.champion=="Morgana"&&m.slot==0)sim.CC(u.f,"Root",2);
                        if(m.owner.f.champion=="Ahri"&&m.slot==2)sim.CC(u.f,"Charm",1.5f);
                        if(m.owner.f.champion=="Mel"&&m.slot==2){SolarBurst(m.owner,u.p,8-m.range);m.solarBloomed=true;}
                        if(m.owner.f.champion=="Ezreal"&&m.slot==0)for(int k=0;k<4;k++)m.owner.f.ready[k]=Mathf.Max(sim.time,m.owner.f.ready[k]-1.5f);
                        if(m.owner.f.champion=="Ezreal"&&u.f.markUntil>sim.time&&m.slot!=1){u.f.markUntil=0;Hit(m.owner,u,180+.9f*m.owner.f.ap,false);}
                    }
                    Effect(MobaArt.Ring,u.p,m.art.color,.3f,1);if(!m.orb&&m.slot!=3){remove=true;break;}
                }
                if(m.orb&&!m.back&&m.range<=0){m.back=true;m.hit.Clear();m.range=20;}else if(m.range<=0||m.back&&Vector2.Distance(m.p,m.owner.p)<.4f)remove=true;
                if(m.owner.f.champion=="Mel"&&m.slot==2&&m.range<=0&&!m.solarBloomed)SolarBurst(m.owner,m.p,8);
                if(remove){Destroy(m.art.gameObject);shots.RemoveAt(i);}else {m.art.transform.position=m.p;m.art.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(m.dir.y,m.dir.x)*Mathf.Rad2Deg);}
            }
        }
        void Hit(Unit source,Unit target,float amount,bool physical){float old=target.f.hp;sim.Damage(source.f,target.f,amount,physical);TrackHit(source,target,old-target.f.hp);}
        void TrackHit(Unit source,Unit target,float amount)
        {
            target.hitAt=sim.time;if(amount>3)pops.Add(new Pop{p=target.p,born=sim.time,text=Mathf.RoundToInt(amount).ToString(),color=source==player?Gold:Pink});
            if(amount>3&&(source==player||target==player))PlayCue(target.f.hp<=0?3:2);
            if(target.f.hp<=0&&target.respawn==0)
            {
                target.respawn=sim.time+7;target.target=null;
                if(source==player){gold+=target.minion?21:target.tower?250:300;if(target.minion)cs++;else if(!target.tower){kills++;Say("CHAMPION TAKEDOWN  +300 gold");}}
                if(target==player){deaths++;Say("You have been slain. Respawning in 7 seconds.");}Effect(MobaArt.Ring,target.p,target.red?Pink:Cyan,1,3);
                if(source.f.champion=="Ahri"&&!target.minion&&!target.tower)source.f.hp=Mathf.Min(source.f.maxHp,source.f.hp+120+.3f*source.f.ap);
            }
        }
        void StepZones(float dt)
        {
            for(int i=zones.Count-1;i>=0;i--)
            {
                var z=zones[i];if(sim.time>z.until){Destroy(z.art.gameObject);zones.RemoveAt(i);continue;}
                z.art.transform.Rotate(0,0,dt*12);bool pulse=!z.solar||sim.time>=z.nextTick;
                foreach(var u in units)if(u.red!=z.owner.red&&u.f.hp>0&&Vector2.Distance(z.p,u.p)<1.5f)
                {if(pulse)Hit(z.owner,u,(z.solar?18+.05f*z.owner.f.ap:45+.14f*z.owner.f.ap)*(z.solar?.25f:dt),false);if(z.solar)u.f.slowUntil=sim.time+.2f;}
                if(z.solar&&pulse)z.nextTick=sim.time+.25f;
            }
            foreach(var u in units)if(u.f.tetherUntil>0&&sim.time>=u.f.tetherUntil){u.f.tetherUntil=0;Unit source=u.red?player:enemy;if(source.f.champion=="Morgana"&&source.f.hp>0&&Vector2.Distance(source.p,u.p)<6){Hit(source,u,200+.7f*source.f.ap,false);sim.CC(u.f,"Stun",1.5f);}}
            ResolveNovaEffects();
        }
        void Effect(Sprite sprite,Vector2 p,Color color,float life,float size){fx.Add(new Fx{art=Art("VFX",sprite,p,Vector2.one*size,color,250),born=sim.time,life=life,size=size});}
        void OnDestroy(){if(world!=null)Destroy(world.gameObject);ReleaseFeel();if(buttonTexture!=null)Destroy(buttonTexture);if(hoverTexture!=null)Destroy(hoverTexture);}
    }
}
