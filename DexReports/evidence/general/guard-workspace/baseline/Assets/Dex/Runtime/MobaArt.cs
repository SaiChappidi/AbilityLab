using System.Collections.Generic;
using UnityEngine;

namespace Dex
{
    // Original arena art with official static champion portraits and optional user sprite sheets.
    public static class MobaArt
    {
        static Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static Dictionary<string,Sprite[]> animation=new Dictionary<string,Sprite[]>();
        static Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
        // Native runtime sprites are destroyed when Play ends, even if managed statics survive.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches(){cache.Clear();animation.Clear();icons.Clear();}
        public static Texture2D Ability(string champion,int slot)
        {string key=champion+"_"+slot;Texture2D icon;if(icons.TryGetValue(key,out icon)&&icon!=null)return icon;icon=Resources.Load<Texture2D>("Dex/Abilities/"+key);icons[key]=icon;return icon;}
        public static Sprite Square {get{return Get("square");}}
        public static Sprite Frame(string name,float time)
        {
            if(!animation.ContainsKey(name))animation[name]=Resources.LoadAll<Sprite>("Dex/Champions/"+name);
            var frames=animation[name];return frames.Length>0?frames[Mathf.FloorToInt(time*10)%frames.Length]:Champion(name);
        }
        public static Sprite Disc {get{return Get("disc");}} public static Sprite Ring {get{return Get("ring");}}
        public static Sprite Tree {get{return Get("tree");}}public static Sprite Ground {get{return Get("ground");}}
        public static Sprite Stone {get{return Get("stone");}}public static Sprite Crystal {get{return Get("crystal");}}
        public static Sprite Minion {get{return Get("minion");}}public static Sprite Tower {get{return Get("tower");}}
        public static Sprite Bolt {get{return Get("bolt");}}public static Sprite Heart {get{return Get("heart");}}
        public static Sprite Champion(string name)
        {
            string key="external-"+name;Sprite saved;if(cache.TryGetValue(key,out saved)&&saved!=null)return saved;
            var imported=Resources.Load<Sprite>("Dex/Champions/"+name);if(imported!=null){cache[key]=imported;return imported;}
            var texture=Resources.Load<Texture2D>("Dex/Champions/"+name);
            if(texture!=null){var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.32f),texture.width);cache[key]=s;return s;}
            return Portrait(name);
        }
        public static Sprite Portrait(string name)
        {
            string key="portrait-"+name;Sprite saved;if(cache.TryGetValue(key,out saved)&&saved!=null)return saved;
            var sprite=Resources.Load<Sprite>("Dex/Portraits/"+name);
            if(sprite!=null){cache[key]=sprite;return sprite;}
            var texture=Resources.Load<Texture2D>("Dex/Portraits/"+name);
            if(texture!=null){sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width,0,SpriteMeshType.FullRect);cache[key]=sprite;return sprite;}
            return Get(name);
        }
        static Sprite Get(string name)
        {
            Sprite saved;if(cache.TryGetValue(name,out saved)&&saved!=null)return saved;var p=new Paint(name=="ground"?512:256);var rng=new System.Random(39);
            var ink=new Color(.055f,.08f,.12f);var gold=new Color(.83f,.67f,.31f);
            switch(name)
            {
                case "square":p.Fill(Color.white);break;
                case "disc":p.Ellipse(.5f,.5f,.48f,.48f,Color.white);break;
                case "ring":p.Ellipse(.5f,.5f,.48f,.48f,Color.white);p.Ellipse(.5f,.5f,.455f,.455f,Color.clear);break;
                case "ground":
                    p.Fill(new Color(.055f,.105f,.13f));
                    for(int i=0;i<17000;i++){float x=(float)rng.NextDouble(),y=(float)rng.NextDouble(),v=(float)rng.NextDouble();p.Ellipse(x,y,.0006f+v*.003f,.001f+v*.003f,new Color(.055f+v*.025f,.11f+v*.055f,.135f+v*.045f));}
                    for(int i=0;i<120;i++){float x=(float)rng.NextDouble(),y=(float)rng.NextDouble();p.Line(x,y,x+.004f,y+.009f,.0015f,new Color(.22f,.23f,.16f,.7f));}break;
                case "stone":
                    p.Fill(new Color(.19f,.24f,.23f));
                    for(int row=0;row<9;row++)for(int col=0;col<4;col++){float x=col*.29f+(row%2)*.13f,y=row*.12f;p.Polygon(new Color(.28f+row*.005f,.32f+row*.005f,.3f+row*.005f),x+.01f,y+.01f,x+.25f,y+.015f,x+.27f,y+.1f,x+.015f,y+.105f);p.Line(x+.025f,y+.1f,x+.25f,y+.1f,.004f,new Color(.39f,.42f,.36f));}break;
                case "tree":
                    p.Ellipse(.53f,.15f,.38f,.1f,new Color(0,0,0,.3f));p.Polygon(new Color(.17f,.12f,.085f),.44f,.1f,.55f,.1f,.52f,.55f,.47f,.55f);
                    for(int i=0;i<5;i++){float y=.24f+i*.125f,w=.37f-i*.058f;p.Polygon(new Color(.035f+i*.012f,.15f+i*.025f,.13f+i*.015f),.5f-w,y,.5f+w,y+.025f,.5f,.95f);p.Line(.5f-w+.035f,y+.025f,.5f,.91f,.008f,new Color(.14f,.3f,.22f));}break;
                case "crystal":
                    p.Polygon(new Color(.3f,.4f,.46f),.13f,.18f,.5f,.05f,.87f,.18f,.8f,.3f,.2f,.3f);p.Polygon(Color.white,.5f,.25f,.29f,.58f,.5f,.95f,.7f,.58f);p.Polygon(new Color(.55f,.85f,1),.5f,.25f,.5f,.95f,.7f,.58f);break;
                case "tower":
                    p.Ellipse(.5f,.2f,.38f,.13f,ink);p.Ellipse(.5f,.23f,.34f,.13f,new Color(.3f,.36f,.38f));
                    p.Polygon(new Color(.22f,.28f,.32f),.27f,.25f,.73f,.25f,.65f,.76f,.35f,.76f);p.Polygon(new Color(.38f,.44f,.46f),.3f,.29f,.46f,.25f,.47f,.76f,.37f,.76f);
                    p.Line(.33f,.37f,.68f,.37f,.018f,gold);p.Line(.36f,.69f,.64f,.69f,.022f,gold);
                    p.Polygon(new Color(.25f,.8f,.9f),.5f,.53f,.39f,.77f,.5f,.98f,.62f,.77f);p.Line(.5f,.6f,.5f,.92f,.018f,Color.white);break;
                case "bolt":p.Ellipse(.55f,.5f,.43f,.22f,new Color(1,1,1,.25f));p.Polygon(Color.white,.05f,.5f,.63f,.65f,.98f,.5f,.63f,.35f);break;
                case "heart":p.Ellipse(.34f,.6f,.23f,.23f,Color.white);p.Ellipse(.66f,.6f,.23f,.23f,Color.white);p.Polygon(Color.white,.12f,.58f,.88f,.58f,.5f,.13f);break;
                case "minion":
                    p.Polygon(ink,.2f,.08f,.8f,.08f,.72f,.6f,.5f,.87f,.28f,.6f);p.Polygon(new Color(.25f,.55f,.62f),.25f,.15f,.75f,.15f,.64f,.55f,.36f,.55f);p.Ellipse(.5f,.63f,.19f,.2f,new Color(.38f,.45f,.5f));p.Line(.38f,.63f,.62f,.63f,.03f,gold);p.Line(.78f,.1f,.85f,.8f,.04f,gold);break;
                default:
                    bool fox=name=="Ahri",witch=name=="Morgana";
                    if(fox)for(int i=0;i<7;i++){float angle=(i-3)*.26f;float x=.5f+Mathf.Sin(angle)*.37f,y=.34f+Mathf.Cos(angle)*.04f;p.Ellipse(x,y,.12f,.25f,new Color(.77f,.82f,.83f));p.Ellipse(x-.015f,y+.025f,.075f,.18f,new Color(.95f,.95f,.9f));}
                    if(witch)for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++){float x=.5f+side*(.14f+i*.06f);p.Polygon(new Color(.21f+i*.025f,.12f,.31f+i*.025f),.5f,.56f,x,.85f-i*.09f,x+side*.12f,.4f-i*.06f,.5f,.37f);}
                    p.Ellipse(.5f,.15f,.17f,.07f,new Color(.03f,.04f,.06f));
                    p.Line(.43f,.14f,.45f,.36f,.06f,ink);p.Line(.57f,.14f,.55f,.36f,.06f,ink);
                    p.Polygon(ink,.3f,.29f,.7f,.29f,.63f,.62f,.5f,.69f,.37f,.62f);
                    Color robe=fox?new Color(.68f,.13f,.23f):witch?new Color(.3f,.17f,.47f):new Color(.18f,.39f,.54f);
                    p.Polygon(robe,.34f,.31f,.66f,.31f,.6f,.61f,.4f,.61f);
                    p.Line(.4f,.52f,.29f,.38f,.045f,new Color(.91f,.72f,.57f));p.Line(.6f,.52f,.73f,.4f,.045f,new Color(.91f,.72f,.57f));
                    p.Line(.4f,.36f,.6f,.36f,.018f,gold);
                    p.Ellipse(.5f,.7f,.145f,.16f,ink);p.Ellipse(.5f,.69f,.115f,.135f,new Color(.95f,.78f,.65f));
                    p.Polygon(fox||witch?new Color(.11f,.12f,.2f):new Color(.91f,.72f,.32f),.35f,.72f,.4f,.86f,.6f,.86f,.65f,.71f,.55f,.77f,.46f,.74f);
                    if(fox){p.Polygon(ink,.37f,.79f,.37f,.98f,.48f,.83f);p.Polygon(ink,.53f,.83f,.64f,.98f,.63f,.79f);p.Ellipse(.24f,.4f,.09f,.09f,new Color(.2f,.8f,1));p.Ellipse(.24f,.42f,.04f,.04f,Color.white);}
                    if(!fox&&!witch){p.Ellipse(.75f,.41f,.09f,.1f,gold);p.Ellipse(.75f,.42f,.045f,.055f,new Color(.27f,.8f,1));}
                    break;
            }
            var texture=p.Texture();var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width);cache[name]=sprite;return sprite;
        }
        class Paint
        {
            int n;Color[] pixels;public Paint(int size){n=size;pixels=new Color[n*n];}
            public void Fill(Color c){for(int i=0;i<pixels.Length;i++)pixels[i]=c;}
            public void Ellipse(float x,float y,float rx,float ry,Color c)
            {int x0=Mathf.Clamp((int)((x-rx)*n),0,n-1),x1=Mathf.Clamp((int)((x+rx)*n),0,n-1),y0=Mathf.Clamp((int)((y-ry)*n),0,n-1),y1=Mathf.Clamp((int)((y+ry)*n),0,n-1);for(int j=y0;j<=y1;j++)for(int i=x0;i<=x1;i++){float a=(i/(float)n-x)/rx,b=(j/(float)n-y)/ry;if(a*a+b*b<=1)pixels[j*n+i]=c;}}
            public void Line(float ax,float ay,float bx,float by,float width,Color c){int steps=Mathf.Max(1,(int)(Vector2.Distance(new Vector2(ax,ay),new Vector2(bx,by))*n*1.5f));for(int i=0;i<=steps;i++){float t=i/(float)steps;Ellipse(Mathf.Lerp(ax,bx,t),Mathf.Lerp(ay,by,t),width,width,c);}}
            public void Polygon(Color c,params float[] v)
            {float lo=1,hi=0;for(int k=1;k<v.Length;k+=2){lo=Mathf.Min(lo,v[k]);hi=Mathf.Max(hi,v[k]);}for(int j=Mathf.Max(0,(int)(lo*n));j<=Mathf.Min(n-1,(int)(hi*n));j++){float y=j/(float)n;var xs=new List<float>();for(int k=0;k<v.Length;k+=2){int next=(k+2)%v.Length;float a=v[k+1],b=v[next+1];if((a<=y&&b>y)||(b<=y&&a>y))xs.Add(v[k]+(y-a)/(b-a)*(v[next]-v[k]));}xs.Sort();for(int k=0;k+1<xs.Count;k+=2)for(int i=Mathf.Max(0,(int)(xs[k]*n));i<=Mathf.Min(n-1,(int)(xs[k+1]*n));i++)pixels[j*n+i]=c;}}
            public Texture2D Texture(){var t=new Texture2D(n,n,TextureFormat.RGBA32,false);t.SetPixels(pixels);t.Apply();t.filterMode=FilterMode.Bilinear;return t;}
        }
    }
}
