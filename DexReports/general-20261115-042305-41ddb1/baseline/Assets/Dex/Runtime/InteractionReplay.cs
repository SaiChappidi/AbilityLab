using UnityEngine;

namespace Dex
{
    public partial class DexArena
    {
        int replayStep=-1;string replayCase="root-blink";bool replayWasBlocked,replayResourcesKept,replayPassed,replayControlLaunched;float replayOwnerHp;
        string replayResult="F6  /  deterministic interaction replay";
        public string ReplayResult {get{return replayResult;}}
        public bool ReplayRunning {get{return replayStep>=0&&replayStep<2;}}
        public bool ReplayCompleted {get{return replayStep==2;}}
        public bool ReplayPassed {get{return ReplayCompleted&&replayPassed;}}
        public string ReplayCase {get{return replayCase;}}
        public void StartInteractionReplay(){StartInteractionReplay("root-blink");}
        public void StartInteractionReplay(string id)
        {
            var item=DemoCases.Find(id);champion=System.Array.IndexOf(champions,item.player);opponent=System.Array.IndexOf(champions,item.opponent);
            bot=false;waves=false;practice=false;menu=false;ResetWorld();replayCase=item.id;replayPassed=false;replayControlLaunched=false;
            player.p=player.goal=new Vector2(-2,-.6f);enemy.p=enemy.goal=new Vector2(2,.6f);player.target=enemy.target=null;
            replayStep=0;replayResult="REPLAY  /  "+item.title+"\nExpected: "+item.expected;replayOwnerHp=enemy.f.hp;
            if(id=="mel-nova"){Cast(player,1,player.p);Cast(enemy,0,player.p);}
            else if(id=="shield-charm"){Cast(player,2,player.p);Cast(enemy,2,player.p);}
            else Cast(enemy,id=="root-dash"?2:0,player.p);
            Say("Replay controls the encounter. Press F6 to restart.");
        }
        void StepReplay()
        {
            if(replayCase=="mel-nova")
            {
                if(replayStep==0&&sim.time>=1.6f)
                {
                    bool destroyed=sim.events.Exists(e=>e.Contains("Rebuttal destroys"));bool control=player.f.ActiveControl(sim.time);bool retaliated=enemy.f.hp<replayOwnerHp;
                    replayWasBlocked=destroyed&&!control&&retaliated;
                    replayResult=(replayWasBlocked?"PASS":"FAIL")+" / active nova: missile destroyed="+destroyed+", root leaked="+control+", retaliation="+retaliated;
                    sim.Log(replayResult);replayStep=1;
                }
                if(replayStep==1&&sim.time>=2.8f&&!replayControlLaunched){replayControlLaunched=true;enemy.f.ready[0]=0;Cast(enemy,0,player.p);}
                if(replayStep==1&&sim.time>=3.6f)
                {bool expiredAllows=player.f.ActiveControl(sim.time);CompleteReplay(replayWasBlocked&&expiredAllows,"After nova expiry: expected incoming root; actual="+expiredAllows);}
                return;
            }
            if(replayCase=="shield-charm")
            {
                if(replayStep==0&&sim.time>=1){replayWasBlocked=!player.f.ActiveControl(sim.time);replayResult=(replayWasBlocked?"PASS":"FAIL")+" / active Black Shield: expected no Charm; actual controlled="+player.f.ActiveControl(sim.time);sim.Log(replayResult);replayStep=1;}
                if(replayStep==1&&sim.time>=5.6f&&!replayControlLaunched){replayControlLaunched=true;enemy.f.ready[2]=0;Cast(enemy,2,player.p);}
                if(replayStep==1&&sim.time>=6.5f)CompleteReplay(replayWasBlocked&&player.f.ActiveControl(sim.time),"After shield expiry: expected Charm; actual="+player.f.ActiveControl(sim.time));
                return;
            }
            int slot=replayCase=="root-dash"?3:2;
            if(replayStep==0&&sim.time>=1)
            {
                Vector2 before=player.p;float mana=player.mana,ready=player.f.ready[slot];bool rooted=player.f.ActiveControl(sim.time)&&player.f.control=="Root";
                Cast(player,slot,player.p+Vector2.up*3);replayWasBlocked=rooted&&Vector2.Distance(before,player.p)<.01f;replayResourcesKept=player.mana==mana&&player.f.ready[slot]==ready;
                replayResult=(replayWasBlocked&&replayResourcesKept?"PASS":"FAIL")+" / rooted mobility: expected blocked; actual "+(Vector2.Distance(before,player.p)>.01f?"MOVED":"blocked")+".\nMana / cooldown "+(replayResourcesKept?"preserved":"consumed")+". Root observed="+rooted;sim.Log(replayResult);replayStep=1;
            }
            if(replayStep==1&&sim.time>=3)
            {Vector2 before=player.p;Cast(player,slot,player.p+Vector2.down*3);bool allowed=Vector2.Distance(before,player.p)>.01f;CompleteReplay(replayWasBlocked&&replayResourcesKept&&allowed,"After root expiry: expected mobility; actual="+allowed);}
        }
        void CompleteReplay(bool passed,string detail)
        {replayPassed=passed;replayResult+="\n"+detail+". "+(passed?"REPLAY PASSED":"REPLAY FAILED");replayStep=2;sim.Log(detail);PlayCue(passed?3:2);}
    }
}
