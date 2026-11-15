using UnityEngine;

namespace Dex
{
    public partial class DexArena
    {
        readonly System.Collections.Generic.Dictionary<Texture,Material> tokenMaterials=new System.Collections.Generic.Dictionary<Texture,Material>();AudioSource feedback;AudioClip[] cues;bool sound=true;
        void SetupFeel()
        {
            feedback=gameObject.AddComponent<AudioSource>();feedback.playOnAwake=false;feedback.spatialBlend=0;
            cues=new AudioClip[4];
            for(int n=0;n<cues.Length;n++)
            {
                int length=5292;float[] samples=new float[length];float frequency=new[]{460,660,220,880}[n];
                for(int i=0;i<length;i++){float t=i/44100f;float envelope=Mathf.Sin(Mathf.PI*i/(length-1))*Mathf.Exp(-t*24);samples[i]=Mathf.Sin(2*Mathf.PI*(frequency+t*300)*t)*envelope*.22f;}
                cues[n]=AudioClip.Create("Dex cue "+n,length,1,44100,false);cues[n].SetData(samples,0);
            }
        }
        void PlayCue(int cue){if(sound&&feedback!=null&&cues!=null&&cues[cue]!=null)feedback.PlayOneShot(cues[cue],.23f);}
        void ReleaseFeel(){if(cues!=null)foreach(var cue in cues)if(cue!=null)Destroy(cue);foreach(var material in tokenMaterials.Values)if(material!=null)Destroy(material);tokenMaterials.Clear();}
        void AddUnitVisuals(Unit u)
        {
            bool championToken=!u.minion&&System.Array.IndexOf(ChampionKit.Roster,u.f.champion)>=0;
            if(championToken)
            {
                if(u.art.sprite==MobaArt.Portrait(u.f.champion))
                {
                    // Keep a distinct material binding per texture: the custom unlit mask
                    // must not let an SRP batch reuse another champion's portrait.
                    var texture=u.art.sprite.texture;Material material;
                    if(!tokenMaterials.TryGetValue(texture,out material))
                    {var shader=Shader.Find("Dex/PortraitToken");if(shader!=null){material=new Material(shader){mainTexture=texture};tokenMaterials[texture]=material;}}
                    if(material!=null)u.art.sharedMaterial=material;
                }
                u.rim=Art("Team outline",MobaArt.Ring,u.p,Vector2.one*1.68f,u.red?Pink:Cyan,99);
                u.aura=Art("Control / barrier aura",MobaArt.Ring,u.p,Vector2.one*1.95f,Gold,98);u.aura.enabled=false;
            }
        }
        void UpdateUnitVisuals(Unit u)
        {
            if(u.rim!=null){u.rim.transform.position=u.p;u.rim.sortingOrder=u.art.sortingOrder-1;}
            if(u.aura!=null)
            {
                u.aura.enabled=u.f.novaUntil>sim.time||u.f.barrier>0||u.f.Shielded(sim.time)||u.f.ActiveControl(sim.time);
                u.aura.transform.position=u.p;u.aura.sortingOrder=u.art.sortingOrder-2;
                u.aura.color=u.f.ActiveControl(sim.time)?Pink:u.f.novaUntil>sim.time?Gold:new Color(.64f,.47f,1);
                u.aura.transform.localScale=Vector3.one*(u.f.novaUntil>sim.time?NovaRadius(u.f)*2:1.95f+.05f*Mathf.Sin(sim.time*6));
            }
        }
    }
}
