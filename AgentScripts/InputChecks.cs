using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InputChecks
{
    static object Get(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(o);}
    static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(o,value);}
    public static async Task<string> Run()
    {
        var arena=UnityEngine.Object.FindAnyObjectByType<Dex.DexArena>();if(arena==null)throw new Exception("Play mode required.");
        Set(arena,"bot",false);Set(arena,"waves",false);Set(arena,"menu",false);
        Set(arena,"champion",2);Set(arena,"opponent",1);
        arena.GetType().GetMethod("ResetWorld",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(arena,null);
        object player=Get(arena,"player"),enemy=Get(arena,"enemy");
        Set(player,"p",new Vector2(-2,0));Set(player,"goal",new Vector2(-2,0));Set(enemy,"p",new Vector2(2,0));Set(enemy,"goal",new Vector2(2,0));
        var mouse=InputSystem.AddDevice<Mouse>("DexDiagnosticsMouse");var keyboard=InputSystem.AddDevice<Keyboard>("DexDiagnosticsKeyboard");
        int passed=0,total=0;var log=new System.Text.StringBuilder();Action<string,bool> check=(n,ok)=>{total++;if(ok)passed++;log.AppendLine((ok?"PASS ":"FAIL ")+n);};
        try
        {
            var cam=Camera.main;Vector2 screen=cam.WorldToScreenPoint(new Vector2(-2,2));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Right));await Task.Delay(120);
            check("right mouse button sets movement goal",Vector2.Distance((Vector2)Get(player,"goal"),new Vector2(-2,2))<.1f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});await Task.Delay(80);
            Vector2 previousGoal=(Vector2)Get(player,"goal");InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));await Task.Delay(100);
            check("left-click empty ground does not replace movement order",(Vector2)Get(player,"goal")==previousGoal&&Get(player,"target")==null);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});await Task.Delay(80);
            screen=cam.WorldToScreenPoint((Vector2)Get(enemy,"p"));
            float healthBefore=((Dex.Fighter)Get(enemy,"f")).hp;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));await Task.Delay(100);
            check("left-click enemy starts auto-attack",Get(player,"target")==enemy);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});await Task.Delay(1800);
            check("one click continues repeated attacks after release",Get(player,"target")==enemy&&((Dex.Fighter)Get(enemy,"f")).hp<healthBefore-150);
            screen=cam.WorldToScreenPoint((Vector2)Get(enemy,"p"));InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Right));await Task.Delay(120);
            check("right mouse button selects enemy attack target",Get(player,"target")==enemy);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));await Task.Delay(100);
            check("S clears attack target",Get(player,"target")==null);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);
            var fighter=(Dex.Fighter)Get(player,"f");
            foreach(var key in new[]{Key.Q,Key.W,Key.E,Key.R})
            {
                int slot=key==Key.Q?0:key==Key.W?1:key==Key.E?2:3;fighter.ready[slot]=0;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Task.Delay(100);
                var sim=(Dex.Combat)Get(arena,"sim");check(key+" input reaches cast and consumes cooldown",fighter.ready[slot]>sim.time);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Task.Delay(100);check("Escape opens sandbox menu",(bool)Get(arena,"menu"));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Task.Delay(100);check("Escape closes sandbox menu",!(bool)Get(arena,"menu"));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F6));await Task.Delay(100);check("F6 starts interaction replay",(int)Get(arena,"replayStep")>=0);
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);}
        string result="Input integration checks: "+passed+"/"+total+"\n"+log;Directory.CreateDirectory("DexReports/evidence");File.WriteAllText("DexReports/evidence/input-checks.txt",result);return result;
    }
}
