using UnityEngine;

namespace Dex
{
    public partial class DexArena
    {
        GUIStyle header,label,tiny,center,big,button,keyStyle;Texture2D pixel,buttonTexture,hoverTexture;bool trace;
        static readonly Color Panel=new Color(.028f,.049f,.08f,.97f),Muted=new Color(.52f,.65f,.73f),Ink=new Color(.88f,.94f,.97f);
        void RectFill(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=Color.white;}
        void Text(float x,float y,float w,float h,string value,GUIStyle style,Color color){var old=GUI.color;GUI.color=color;GUI.Label(new Rect(x,y,w,h),value,style);GUI.color=old;}
        void Frame(float x,float y,float w,float h){RectFill(x,y,w,h,Panel);RectFill(x,y,w,1,new Color(.18f,.31f,.39f));}
        void Border(float x,float y,float w,float h,Color c){RectFill(x,y,w,1,c);RectFill(x,y+h-1,w,1,c);RectFill(x,y,1,h,c);RectFill(x+w-1,y,1,h,c);}
        void Line(Vector2 a,Vector2 b,Color c,float width=1){Matrix4x4 before=GUI.matrix;Vector2 d=b-a;GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,a);RectFill(a.x,a.y,d.magnitude,width,c);GUI.matrix=before;}
        Vector2 ScreenPoint(Vector2 p){Vector3 s=cam.WorldToScreenPoint(p);return new Vector2(s.x/Screen.width*1600,(1-s.y/Screen.height)*900);}
        bool Click(float x,float y,float w,float h,string name){return !manualSimulation&&GUI.Button(new Rect(x,y,w,h),name,button);}
        Texture2D TintTexture(Color color){var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;}
        void InitHud()
        {
            pixel=Texture2D.whiteTexture;label=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};label.normal.textColor=Ink;
            tiny=new GUIStyle(label){fontSize=12};header=new GUIStyle(label){fontSize=20,fontStyle=FontStyle.Bold};big=new GUIStyle(header){fontSize=34};
            center=new GUIStyle(label){alignment=TextAnchor.MiddleCenter};keyStyle=new GUIStyle(header){alignment=TextAnchor.MiddleCenter};
            buttonTexture=TintTexture(new Color(.065f,.12f,.17f));hoverTexture=TintTexture(new Color(.1f,.23f,.29f));
            button=new GUIStyle(GUI.skin.button){fontSize=14,fontStyle=FontStyle.Bold,padding=new RectOffset(12,12,7,7)};
            button.normal.background=buttonTexture;button.normal.textColor=Ink;button.hover.background=hoverTexture;button.hover.textColor=Cyan;button.active.background=hoverTexture;button.active.textColor=Gold;
        }
        void OnGUI()
        {
            if(player==null)return;if(header==null)InitHud();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));
            foreach(var u in units)
            {
                if(u.f.hp<=0)continue;Vector2 p=ScreenPoint(u.p+Vector2.up*(u.tower?1.35f:.95f));float w=u.minion?42:u.tower?110:112;
                RectFill(p.x-w/2-2,p.y-2,w+4,11,new Color(.014f,.025f,.04f));RectFill(p.x-w/2,p.y,w*Mathf.Clamp01(u.f.hp/u.f.maxHp),6,u.red?Pink:Cyan);
                if(!u.minion){Text(p.x-105,p.y-29,210,25,u.f.champion+(u.f.ActiveControl(sim.time)?"  /  "+u.f.control.ToUpper():""),center,Ink);if(!u.tower)RectFill(p.x-w/2,p.y+8,w*Mathf.Clamp01(u.mana/900),2,new Color(.3f,.52f,.95f));}
                if(u.f.Shielded(sim.time)||u.f.barrier>0)Text(p.x-90,p.y+16,180,23,"BARRIER  "+(u.f.shield+u.f.barrier).ToString("0"),center,Gold);
                if(u.f.overwhelm>0&&u.f.overwhelmUntil>sim.time)Text(p.x-90,p.y+37,180,22,"OVERWHELM  "+u.f.overwhelm,center,Gold);
                else if(u.f.markUntil>sim.time)Text(p.x-70,p.y+16,140,22,"FLUX MARK",center,Gold);
            }
            foreach(var pop in pops){Vector2 p=ScreenPoint(pop.p);Text(p.x-30,p.y-24-(sim.time-pop.born)*48,90,32,pop.text,header,pop.color);}
            if(!menu&&player.f.hp>0&&!ReplayRunning){Vector2 to=ScreenPoint(aim);Line(to+Vector2.left*6,to+Vector2.right*6,Cyan);Line(to+Vector2.up*6,to+Vector2.down*6,Cyan);}
            Frame(0,0,1600,66);Text(28,12,320,36,"DEX  /  INTERACTION LAB",header,Ink);Text(366,22,335,22,manualSimulation?"AUTOMATED TEST  /  CONTROLS LOCKED":ReplayRunning?"RECORDED REPLAY  /  CONTROLS LOCKED":"MANUAL PRACTICE  /  SIMPLIFIED KITS",tiny,Cyan);
#if UNITY_EDITOR
            if(!ReplayRunning&&Click(714,15,220,34,"OPEN DEX WORKSPACE"))UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorApplication.ExecuteMenuItem("Dex/Agent Workbench");
#endif
            Text(950,15,275,35,kills+" K   "+deaths+" D   "+cs+" CS    "+((int)sim.time/60).ToString("00")+":"+((int)sim.time%60).ToString("00"),center,Muted);
            if(Click(1250,15,145,34,trace?"CLOSE TRACE":"COMBAT TRACE"))trace=!trace;if(Click(1407,15,165,34,"SANDBOX  /  ESC"))menu=!menu;
            Frame(25,88,620,100);RectFill(25,88,3,100,replayResult.Contains("FAIL")?Pink:Cyan);
            string statusText=manualSimulation&&!string.IsNullOrEmpty(scenarioStatus)?scenarioStatus:replayResult;
            Text(43,96,580,82,statusText,label,statusText.Contains("FAIL")?Pink:Cyan);
            if(player.f.hp<=0){Frame(610,285,380,115);Text(635,305,330,40,"RESPAWNING",big,Pink);Text(635,355,330,30,Mathf.CeilToInt(player.respawn-sim.time)+" seconds  /  fresh resources on return",center,Ink);}
            else if(sim.time<noticeUntil&&!ReplayRunning&&!manualSimulation)Text(660,90,540,55,notice,center,Gold);
            DrawHud();if(trace)DrawTrace();if(menu)DrawMenu();GUI.matrix=Matrix4x4.identity;
        }
        void DrawTrace()
        {
            Frame(1240,85,335,340);Text(1258,99,300,27,"INTERACTION TRACE",header,Ink);Text(1258,129,300,25,"Actual combat events / newest first",tiny,Muted);
            for(int i=0;i<Mathf.Min(8,sim.events.Count);i++)Text(1258,165+i*29,300,28,sim.events[i],tiny,i==0?Cyan:Ink);
            Text(1258,405,300,55,"Open Dex Workspace for reports and evidence.",tiny,Muted);
        }
        void DrawHud()
        {
            Frame(0,744,1600,156);RectFill(0,744,1600,2,new Color(.18f,.32f,.4f));
            GUI.DrawTexture(new Rect(27,770,96,96),MobaArt.Portrait(player.f.champion).texture,ScaleMode.ScaleToFit);Border(26,769,98,98,Gold);
            Text(143,770,174,30,player.f.champion.ToUpper(),header,Ink);Text(143,809,174,26,"LEVEL 11  /  "+gold+" G",tiny,Gold);Text(143,843,174,42,"AP "+player.f.ap+"  AD "+player.f.ad+"\nSIMPLIFIED PRESET",tiny,Muted);
            var names=Combat.Names(player.f.champion);
            for(int slot=0;slot<4;slot++)
            {
                float x=330+slot*103,cd=Mathf.Max(0,player.f.ready[slot]-sim.time);bool available=cd<=0&&player.mana>=ChampionKit.Mana(player.f.champion,slot);
                RectFill(x,758,94,88,new Color(.045f,.08f,.12f));var icon=MobaArt.Ability(player.f.champion,slot);
                if(icon!=null){GUI.color=available?Color.white:new Color(.4f,.45f,.5f);GUI.DrawTexture(new Rect(x+14,769,64,64),icon);GUI.color=Color.white;}
                Border(x,758,94,88,available?Cyan:new Color(.14f,.22f,.29f));RectFill(x+3,761,23,23,Panel);Text(x+3,759,23,25,"QWER"[slot].ToString(),keyStyle,Ink);
                Text(x+62,820,28,22,ChampionKit.Mana(player.f.champion,slot).ToString("0"),tiny,new Color(.45f,.66f,1));
                if(cd>0){RectFill(x+1,759,92,86,new Color(.015f,.025f,.04f,.65f));Text(x,779,94,44,cd.ToString("0.0"),keyStyle,Gold);}
                if(!manualSimulation&&GUI.Button(new Rect(x,758,94,88),GUIContent.none,GUIStyle.none)&&!menu&&!ReplayRunning)Cast(player,slot,lastArenaAim);
                if(new Rect(x,758,94,88).Contains(Event.current.mousePosition))
                {Frame(x-40,574,360,150);Text(x-22,590,324,32,names[slot],header,Gold);Text(x-22,628,324,80,ChampionKit.Description(player.f.champion,slot),label,Ink);}
            }
            RectFill(330,858,403,13,new Color(.05f,.13f,.14f));RectFill(330,858,403*Mathf.Clamp01(player.f.hp/player.f.maxHp),13,Cyan);Text(330,853,403,24,Mathf.CeilToInt(player.f.hp)+" / "+player.f.maxHp,center,Ink);
            RectFill(330,879,403,7,new Color(.06f,.1f,.2f));RectFill(330,879,403*player.mana/900,7,new Color(.33f,.51f,.95f));
            Text(765,765,470,23,player.f.champion=="Mel"?"SEARING BRILLIANCE  /  "+player.f.searing+" BANKED BOLTS":"COMBAT STATUS",tiny,Gold);
            Text(765,796,450,47,player.f.champion=="Mel"?ChampionKit.Passive("Mel"):player.target!=null?"AUTO-ATTACKING  /  "+player.target.f.champion:ChampionKit.Passive(player.f.champion),tiny,Ink);
            Text(765,856,480,34,"LMB enemy: auto-attack   RMB: move / attack\nQ W E R: cast   S: stop   F6: replay   ESC: sandbox",tiny,Muted);
            Frame(1275,764,298,118);Text(1291,771,265,20,"TEST CHAMBER  /  TEAM POSITIONS",tiny,Muted);
            for(int i=0;i<5;i++)RectFill(1294+i*60,801,1,65,new Color(.09f,.19f,.24f));
            foreach(var u in units){if(u.f.hp<=0)continue;float x=1295+(u.p.x+13)/26*260,y=863-(u.p.y+6.5f)/13*62;RectFill(x,y,u==player?7:4,u==player?7:4,u.red?Pink:Cyan);}
        }
        bool Toggle(float x,float y,float w,bool value,string title)
        {if(Click(x,y,w,34,(value?"ON  /  ":"OFF  /  ")+title))value=!value;return value;}
        void ChampionCards(float x,float y,bool yours)
        {
            for(int index=0;index<champions.Length;index++)
            {
                float left=x+index*138;bool selected=(yours?champion:opponent)==index;
                if(Click(left,y,126,150,"")){if(yours)champion=index;else opponent=index;}
                GUI.DrawTexture(new Rect(left+20,y+12,86,86),MobaArt.Portrait(champions[index]).texture,ScaleMode.ScaleToFit);
                Text(left+3,y+107,120,28,champions[index],center,selected?Gold:Ink);Border(left,y,126,150,selected?Gold:new Color(.14f,.24f,.31f));
            }
        }
        void DrawMenu()
        {
            RectFill(0,66,1600,678,new Color(.008f,.015f,.028f,.95f));Frame(155,95,1290,625);
            Text(185,112,730,45,"THE INTERACTION SANDBOX",big,Ink);Text(185,161,1040,27,"Four simplified kits. Reproduce a failure, then let the development agent repair the shared rule.",label,Muted);
            items=Toggle(1135,117,264,items,"Item preset on reset");
            Text(185,207,560,25,"YOUR CHAMPION",tiny,Cyan);Text(855,207,560,25,"OPPONENT",tiny,Pink);ChampionCards(185,241,true);ChampionCards(855,241,false);
            bot=Toggle(185,412,264,bot,"Opponent AI");waves=Toggle(465,412,264,waves,"Minion waves");practice=Toggle(855,412,264,practice,"Practice regeneration");sound=Toggle(1135,412,264,sound,"Sound cues");
            if(Click(185,462,264,42,"APPLY MATCHUP / RESET")){ResetWorld();menu=false;}
            if(Click(465,462,264,42,"REFILL HEALTH / MANA")){player.f.hp=player.f.maxHp;player.mana=900;}
            if(Click(855,462,264,42,"RESET COOLDOWNS")){for(int i=0;i<4;i++)player.f.ready[i]=0;player.f.rushCharges=3;}
            if(Click(1135,462,264,42,"RETURN TO ARENA"))menu=false;
            Text(185,531,1100,23,"REPEATABLE REGRESSION REPLAYS",tiny,Gold);
            for(int n=0;n<DemoCases.All.Length;n++){var item=DemoCases.All[n];if(Click(185+n*306,564,292,48,item.title))StartInteractionReplay(item.id);}
            Text(185,642,1210,48,"Current Mel kit: projectile-destroying Rebuttal nova with retaliation. Official portrait / ability art: Riot Games. Original chamber art. All balance and geometry are demo tuning.",tiny,Muted);
        }
    }
}
