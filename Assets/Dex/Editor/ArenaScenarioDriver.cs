using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Dex.Editor
{
    // Protected host adapter. Operations call actual gameplay methods; this is not Combat's 1D imitation.
    public static class ArenaScenarioDriver
    {
        public static bool Busy {get;private set;}
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        const string Owned="Dex.General.OwnedPlay";
        static object Get(object o,string name){return o.GetType().GetField(name,Flags).GetValue(o);}
        static void Set(object o,string name,object value){o.GetType().GetField(name,Flags).SetValue(o,value);}
        static object Call(object o,string name,params object[] args){try{return o.GetType().GetMethod(name,Flags).Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        [InitializeOnLoadMethod] static void Recover()
        {
            if(SessionState.GetBool(Owned,false)){SessionState.SetBool(Owned,false);EditorApplication.update+=StopInterrupted;}
        }
        static void StopInterrupted(){EditorApplication.update-=StopInterrupted;if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;}
        public static async Task<ArenaRun> Run(ArenaScenario scenario,string directory,string phase,CancellationToken token,bool playback=false)
        {
            ArenaAssertions.Validate(scenario);
            if(Busy||ReplayAutomation.Busy||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Scenario execution requires an idle Editor.");
            if(!EditorSettings.enterPlayModeOptionsEnabled||(EditorSettings.enterPlayModeOptions&EnterPlayModeOptions.DisableDomainReload)==0)throw new InvalidOperationException("Automatic scenarios require Disable Domain Reload in Play settings.");
            Busy=true;bool background=Application.runInBackground;var random=UnityEngine.Random.state;DateTime deadline=DateTime.UtcNow.AddSeconds(playback?150:45);
            var run=new ArenaRun{title=scenario.title,seed=scenario.seed};var observations=new List<ArenaObservation>();
            try
            {
                Directory.CreateDirectory(directory);SessionState.SetBool(Owned,true);EditorApplication.isPaused=false;EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;
                DexArena arena=null;
                while(arena==null){token.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new TimeoutException("Arena startup timed out.");if(EditorApplication.isPlaying)arena=UnityEngine.Object.FindAnyObjectByType<DexArena>();await Task.Delay(30,token);}
                Application.runInBackground=true;Set(arena,"manualSimulation",true);Set(arena,"menu",false);Set(arena,"bot",false);Set(arena,"waves",false);Set(arena,"practice",false);Set(arena,"items",false);Set(arena,"sound",false);
                Set(arena,"champion",Array.IndexOf(ChampionKit.Roster,scenario.player));Set(arena,"opponent",Array.IndexOf(ChampionKit.Roster,scenario.opponent));
                Call(arena,"ResetWorld");UnityEngine.Random.InitState(scenario.seed);await Task.Delay(50,token);
                var actors=new Dictionary<string,object>();Bind(arena,actors);observations.Add(Observe(arena,actors,"initial","initial setup"));
                int index=0;
                foreach(var action in scenario.actions)
                {
                    token.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new TimeoutException("Scenario execution timed out.");if(!EditorApplication.isPlaying)throw new InvalidOperationException("Scenario interrupted by leaving Play.");
                    if(playback&&action.op=="advance")
                    {
                        float remaining=action.seconds,dt=action.dt>0?action.dt:1f/60,rendered=0;int frame=0;
                        string frames=Path.Combine(directory,phase+"-frames");Directory.CreateDirectory(frames);
                        while(remaining>.000001f)
                        {token.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new TimeoutException("Playback timed out.");float tick=Mathf.Min(dt,remaining);Call(arena,"TickSimulation",tick);remaining-=tick;rendered+=tick;if(rendered>=.05f){await Task.Delay((int)(rendered*1000),token);rendered=0;if(frame++%4==0)ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(frames,index.ToString("D3")+"-"+frame.ToString("D4")+".png")));}}
                    }
                    else Apply(arena,actors,action);
                    await Task.Delay(action.op=="reset"?50:playback?100:1,token);
                    observations.Add(Observe(arena,actors,string.IsNullOrEmpty(action.label)?"action-"+index:action.label,JsonUtility.ToJson(action)));index++;
                }
                run.observations=observations.ToArray();ArenaAssertions.Evaluate(run,scenario.checks);
                string image=Path.GetFullPath(Path.Combine(directory,phase+".png"));ScreenCapture.CaptureScreenshot(image);await Task.Delay(150,token);run.screenshot=image;
            }
            catch(OperationCanceledException){throw;}
            catch(Exception e){run.error=e.GetType().Name+": "+e.Message;run.observations=observations.ToArray();}
            finally
            {
                if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
                DateTime stop=DateTime.UtcNow.AddSeconds(10);while(EditorApplication.isPlayingOrWillChangePlaymode&&DateTime.UtcNow<stop)await Task.Delay(30);
                Application.runInBackground=background;UnityEngine.Random.state=random;SessionState.SetBool(Owned,false);Busy=false;
            }
            File.WriteAllText(Path.Combine(directory,phase+".json"),JsonUtility.ToJson(run,true));File.WriteAllText(Path.Combine(directory,phase+".txt"),run.ToString());return run;
        }
        static void Bind(DexArena arena,Dictionary<string,object> actors){actors.Clear();actors["player"]=Get(arena,"player");actors["enemy"]=Get(arena,"enemy");}
        static void Place(object actor,float x,float y){if(Math.Abs(x)>13||Math.Abs(y)>6.5f)throw new ArgumentException("Position outside board.");var p=new Vector2(x,y);Set(actor,"p",p);Set(actor,"goal",p);((SpriteRenderer)Get(actor,"art")).transform.position=p;}
        static void Apply(DexArena arena,Dictionary<string,object> actors,ArenaAction a)
        {
            var sim=(Combat)Get(arena,"sim");object actor=null;if(!string.IsNullOrEmpty(a.unit)&&actors.ContainsKey(a.unit))actor=actors[a.unit];
            if(a.op=="reset"){Call(arena,"ResetWorld");Bind(arena,actors);return;}
            if(a.op=="observe")return;
            if(a.op=="spawn")
            {
                if(string.IsNullOrEmpty(a.unit)||actors.ContainsKey(a.unit)||(!ChampionKit.Roster.Contains(a.champion)&&a.champion!="Minion"))throw new ArgumentException("Spawn needs a unique unit id and known champion or Minion.");
                if(Math.Abs(a.x)>13||Math.Abs(a.y)>6.5f)throw new ArgumentException("Spawn position outside board.");
                var fighter=new Fighter(a.champion,false,0);actor=Call(arena,"Spawn",fighter,new Vector2(a.x,a.y),a.red,a.champion=="Minion");actors[a.unit]=actor;return;
            }
            if(a.op=="advance")
            {
                float remaining=a.seconds,dt=a.dt>0?a.dt:1f/60;
                while(remaining>.000001f){float tick=Mathf.Min(dt,remaining);Call(arena,"TickSimulation",tick);remaining-=tick;}return;
            }
            if(actor==null)throw new ArgumentException("Unknown action unit: "+a.unit);
            var f=(Fighter)Get(actor,"f");
            if(a.op=="place"){Place(actor,a.x,a.y);return;}
            if(a.op=="move"){Set(actor,"target",null);Set(actor,"goal",new Vector2(a.x,a.y));return;}
            if(a.op=="stop"){Set(actor,"target",null);Set(actor,"goal",Get(actor,"p"));return;}
            if(a.op=="attack"){if(!actors.ContainsKey(a.target))throw new ArgumentException("Unknown attack target");Set(actor,"target",actors[a.target]);return;}
            if(a.op=="cast"){int slot="QWER".IndexOf((a.slot??"").ToUpperInvariant(),StringComparison.Ordinal);if(string.IsNullOrEmpty(a.slot)||a.slot.Length!=1||slot<0)throw new ArgumentException("Cast slot must be Q/W/E/R.");Call(arena,"Cast",actor,slot,new Vector2(a.x,a.y));return;}
            if(a.op=="control"){if(!new[]{"Root","Stun","Charm","Ground"}.Contains(a.text)||a.seconds<=0||a.seconds>30)throw new ArgumentException("Invalid control");sim.CC(f,a.text,a.seconds);return;}
            if(a.op=="damage"){if(!actors.ContainsKey(a.target)||a.value<0)throw new ArgumentException("Invalid damage");Call(arena,"Hit",actor,actors[a.target],a.value,a.text=="physical");return;}
            if(a.op=="set")
            {
                if(a.field=="attackAt"||a.field=="respawn"){Set(actor,a.field,a.value);return;}
                if(a.field!=null&&a.field.StartsWith("ready")&&a.field.Length==6){int i="QWER".IndexOf(a.field[5]);if(i<0)throw new ArgumentException("Unknown cooldown");f.ready[i]=a.value;return;}
                if(a.field=="searing"||a.field=="overwhelm"||a.field=="rushCharges"){typeof(Fighter).GetField(a.field).SetValue(f,(int)a.value);return;}
                if(!NumericFields.Contains(a.field))throw new ArgumentException("Field is not settable: "+a.field);typeof(Fighter).GetField(a.field).SetValue(f,a.value);return;
            }
            throw new ArgumentException("Unsupported operation");
        }
        static readonly string[] NumericFields={"hp","maxHp","mana","ap","ad","armor","mr","haste","controlUntil","shield","shieldUntil","barrier","barrierUntil","novaUntil","speedUntil","slowUntil","overwhelmUntil","markUntil","tetherUntil"};
        static ArenaValue V(string name,float value){return new ArenaValue{name=name,value=value};}
        static ArenaObservation Observe(DexArena arena,Dictionary<string,object> actors,string label,string action)
        {
            var sim=(Combat)Get(arena,"sim");var states=new List<ArenaUnitState>();var live=(IList)Get(arena,"units");
            foreach(var pair in actors)
            {
                var actor=pair.Value;var f=(Fighter)Get(actor,"f");var p=(Vector2)Get(actor,"p");var values=new List<ArenaValue>{V("x",p.x),V("y",p.y),V("present",live.Contains(actor)?1:0),V("alive",f.hp>0?1:0),V("controlled",f.ActiveControl(sim.time)?1:0),V("shielded",f.Shielded(sim.time)?1:0),V("searing",f.searing),V("overwhelm",f.overwhelm),V("rushCharges",f.rushCharges),V("attackAt",(float)Get(actor,"attackAt")),V("respawn",(float)Get(actor,"respawn"))};
                values.AddRange(NumericFields.Select(n=>V(n,(float)typeof(Fighter).GetField(n).GetValue(f))));for(int i=0;i<4;i++){values.Add(V("ready"+"QWER"[i],f.ready[i]));values.Add(V("cooldown"+"QWER"[i],Mathf.Max(0,f.ready[i]-sim.time)));}
                states.Add(new ArenaUnitState{id=pair.Key,champion=f.champion,control=f.control,values=values.ToArray()});
            }
            return new ArenaObservation{label=label,action=action,time=sim.time,units=states.ToArray(),events=sim.events.ToArray(),world=new[]{V("time",sim.time),V("shots",((IList)Get(arena,"shots")).Count),V("zones",((IList)Get(arena,"zones")).Count),V("effects",((IList)Get(arena,"fx")).Count),V("units",live.Count),V("kills",(int)Get(arena,"kills")),V("deaths",(int)Get(arena,"deaths")),V("cs",(int)Get(arena,"cs")),V("worldRoots",UnityEngine.Object.FindObjectsByType<Transform>().Count(t=>t.name=="Sandbox World"))}};
        }
        public static async Task<ProtectedRegressionRun> Regressions(string directory,CancellationToken token)
        {
            var core=Verification.Evaluate();File.WriteAllText(Path.Combine(directory,"protected-core.txt"),core.ToString());
            var lifetimes=ProtectedGameplayRegressions.Evaluate();File.WriteAllText(Path.Combine(directory,"protected-lifetimes.txt"),lifetimes.ToString());
            string prefix="CORE: "+core.ToString().Split('\n')[0]+"\nLIFETIMES: "+lifetimes.ToString().Split('\n')[0]+"\n";
            if(!core.Success||!lifetimes.Success)return new ProtectedRegressionRun{success=false,text=core+"\n\nADDITIONAL LIFETIME INVARIANTS\n"+lifetimes};
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Regression runner requires an idle Editor.");
            bool background=Application.runInBackground;DateTime deadline=DateTime.UtcNow.AddSeconds(25);
            try
            {
                SessionState.SetBool(Owned,true);EditorApplication.isPlaying=true;DexArena arena=null;
                while(arena==null){token.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new TimeoutException("Regression arena startup timeout");if(EditorApplication.isPlaying)arena=UnityEngine.Object.FindAnyObjectByType<DexArena>();await Task.Delay(30,token);}
                Application.runInBackground=true;Set(arena,"manualSimulation",true);string result=arena.RunSmokeChecks();File.WriteAllText(Path.Combine(directory,"protected-arena.txt"),result);
                var count=System.Text.RegularExpressions.Regex.Match(result,@"Arena integration smoke checks: (\d+)/(\d+)");
                bool passed=count.Success&&count.Groups[1].Value==count.Groups[2].Value&&!result.Contains("FAIL");
                return new ProtectedRegressionRun{success=passed,text=prefix+result};
            }
            finally{if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;while(EditorApplication.isPlayingOrWillChangePlaymode&&DateTime.UtcNow<deadline)await Task.Delay(30);Application.runInBackground=background;SessionState.SetBool(Owned,false);}
        }
    }
}
