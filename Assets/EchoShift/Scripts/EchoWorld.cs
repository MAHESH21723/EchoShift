using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EchoShift
{
    // All scene art is original procedural geometry. No external art assets are needed.
    public sealed class EchoWorld : MonoBehaviour
    {
        public static readonly Color Mint = Hex("87F6D4"), Amber = Hex("FFC778"), Violet = Hex("AD9AFF");
        static Sprite square, disc, ring, glow, rounded;
        readonly List<Transform> echoBodies = new List<Transform>();
        readonly List<LineRenderer> echoPaths = new List<LineRenderer>();
        readonly List<SpriteRenderer> plates = new List<SpriteRenderer>(), doors = new List<SpriteRenderer>();
        readonly List<Transform> memories = new List<Transform>();
        readonly List<SpriteRenderer> wires = new List<SpriteRenderer>();
        Transform worldRoot, player, anchor, portal;
        LineRenderer livePath;
        Material lineMaterial;
        TMP_FontAsset font;
        EchoSimulation sim;
        int shownEchoes;
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        public void Initialize(TMP_FontAsset uiFont)
        {
            font = uiFont; BuildSprites();
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        static void BuildSprites()
        {
            if (square != null) return;
            square = MakeSprite(2, (x,y) => 1);
            disc = MakeSprite(96, (x,y) => Mathf.Clamp01((1 - Mathf.Sqrt(x*x+y*y)) * 36));
            ring = MakeSprite(128, (x,y) => Mathf.Clamp01((.075f - Mathf.Abs(Mathf.Sqrt(x*x+y*y)-.83f))*70));
            glow = MakeSprite(96, (x,y) => Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(x*x+y*y)), 3));
            rounded = MakeSprite(64, (x,y) => { float dx=Mathf.Max(Mathf.Abs(x)-.65f,0), dy=Mathf.Max(Mathf.Abs(y)-.65f,0); return Mathf.Clamp01((.35f-Mathf.Sqrt(dx*dx+dy*dy))*32); });
        }
        static Sprite MakeSprite(int size, System.Func<float,float,float> alpha)
        {
            var t = new Texture2D(size,size,TextureFormat.RGBA32,false); t.name="Echo procedural shape"; t.filterMode=FilterMode.Bilinear; t.wrapMode=TextureWrapMode.Clamp;
            var pixels = new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) pixels[y*size+x]=new Color(1,1,1,alpha((x+.5f)/size*2-1,(y+.5f)/size*2-1));
            t.SetPixels(pixels); t.Apply(); return Sprite.Create(t,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
        SpriteRenderer Shape(string name, Vector2 at, Vector2 size, Color color, int order, Sprite sprite = null, Transform parent = null)
        {
            var o=new GameObject(name); o.transform.SetParent(parent == null ? worldRoot : parent,false); o.transform.localPosition=at; o.transform.localScale=size;
            var r=o.AddComponent<SpriteRenderer>(); r.sprite=sprite == null ? square : sprite; r.color=color; r.sortingOrder=order; return r;
        }
        void Label(string label, Vector2 at, float size, Color color, int order=20)
        {
            var o=new GameObject(label); o.transform.SetParent(worldRoot,false); o.transform.localPosition=new Vector3(at.x,at.y,0);
            var t=o.AddComponent<TextMeshPro>(); t.font=font; t.text=label; t.fontSize=size; t.alignment=TextAlignmentOptions.Center; t.color=color;
            t.rectTransform.sizeDelta=new Vector2(5,1); t.GetComponent<MeshRenderer>().sortingOrder=order;
        }
        public void Build(EchoSimulation simulation)
        {
            sim=simulation; if(worldRoot != null) Destroy(worldRoot.gameObject);
            worldRoot=new GameObject("Chamber geometry").transform; worldRoot.SetParent(transform,false);
            echoBodies.Clear(); echoPaths.Clear(); plates.Clear(); doors.Clear(); memories.Clear(); wires.Clear(); shownEchoes=0;
            Shape("Floating foundation shadow",new Vector2(10,4.7f),new Vector2(21.8f,11.7f),Hex("04090F"),-10,rounded);
            Shape("Foundation",new Vector2(10,5),new Vector2(21.4f,11.4f),Hex("22313D"),-9,rounded);
            Shape("Room floor",new Vector2(10,5),new Vector2(21.1f,11.1f),Hex("111C29"),-8,rounded);
            for(int x=0;x<21;x++) for(int y=0;y<11;y++)
            {
                var c=((x+y)%2==0)?Hex("152331"):Hex("172635");
                Shape("Floor panel",new Vector2(x,y),new Vector2(.969f,.969f),c,-7,rounded);
                if(x%3==1 && y%3==1) Shape("Recessed fastener",new Vector2(x+.34f,y+.34f),Vector2.one*.032f,Hex("47606B"),-6,disc);
            }
            for(int i=0;i<sim.Level.Plates.Count;i++)
            {
                Vector2 p=sim.Level.Plates[i]; var gate=sim.Level.Doors.Find(d=>System.Array.IndexOf(d.Required,i)>=0);
                float mid=(p.x+gate.Position.x)*.5f;
                Wire(p,new Vector2(mid,p.y)); Wire(new Vector2(mid,p.y),new Vector2(mid,gate.Position.y)); Wire(new Vector2(mid,gate.Position.y),gate.Position);
            }
            foreach(var w in sim.Level.Walls)
            {
                Vector2 at=w.center;
                Shape("Wall shadow",at+Vector2.down*.16f,w.size+Vector2.one*.02f,Hex("09111A"),1,rounded);
                Shape("Wall side",at,w.size-new Vector2(.035f,.035f),Hex("2A3B49"),2,rounded);
                Shape("Wall face",at+Vector2.up*.09f,w.size-new Vector2(.11f,.15f),Hex("3B5260"),3,rounded);
                Shape("Wall highlight",new Vector2(at.x,w.yMax-.09f),new Vector2(w.width-.22f,.027f),Hex("637C85"),4);
                if(w.width>1.5f) for(float x=w.xMin+.3f;x<w.xMax-.2f;x+=.3f) Shape("Vent",new Vector2(x,at.y),new Vector2(.05f,.32f),Hex("273C4A"),4);
            }
            for(int i=0;i<sim.Level.Plates.Count;i++)
            {
                Vector2 at=sim.Level.Plates[i];
                Shape("Plate aura",at,Vector2.one*1.7f,new Color(Amber.r,Amber.g,Amber.b,.3f),-2,glow);
                Shape("Plate base",at,Vector2.one*.92f,Hex("43392D"),0,rounded);
                var p=Shape("Pressure plate "+(char)('A'+i),at,Vector2.one*.76f,Amber,1,ring); plates.Add(p);
                Label(((char)('A'+i)).ToString(),at,.0f+3,Amber);
                Label("HOLD",at+Vector2.down*.72f,1.5f,Hex("BAAA8E"));
            }
            foreach(var d in sim.Level.Doors)
            {
                Shape("Door rail",d.Position+Vector2.down*.48f,new Vector2(1,.09f),Amber,5);
                Shape("Door rail",d.Position+Vector2.up*.48f,new Vector2(1,.09f),Amber,5);
                doors.Add(Shape("Energy door",d.Position,new Vector2(.3f,.95f),Amber,6,rounded));
                Shape("Door threshold",d.Position,new Vector2(.7f,.94f),new Color(Amber.r,Amber.g,Amber.b,.08f),0);
                string key=d.Required.Length>1?"A + B":((char)('A'+d.Required[0])).ToString();
                Label(key,d.Position+Vector2.up*.85f,1.8f,Amber);
            }
            for(int i=0;i<sim.Level.Memories.Count;i++)
            {
                var m=Shape("Memory fragment",sim.Level.Memories[i],Vector2.one*.18f,Mint,8,rounded); m.transform.localRotation=Quaternion.Euler(0,0,45); memories.Add(m.transform);
                Shape("Memory glow",sim.Level.Memories[i],Vector2.one*1.0f,new Color(Mint.r,Mint.g,Mint.b,.14f),0,glow);
            }
            Shape("Exit aura",sim.Level.Exit,Vector2.one*2.6f,new Color(Mint.r,Mint.g,Mint.b,.4f),0,glow);
            portal=Shape("Exit ring",sim.Level.Exit,Vector2.one*1.17f,Mint,4,ring).transform;
            Shape("Exit interior",sim.Level.Exit,Vector2.one*.64f,Hex("173F40"),3,disc);
            Label("EXIT",sim.Level.Exit+Vector2.down*.95f,1.9f,Mint);
            Label("ANCHOR / 00",sim.Level.Start+Vector2.down*.88f,1.55f,Hex("5A8393"));
            Shape("Spawn marker",sim.Level.Start,Vector2.one*.8f,Hex("426575"),0,ring);
            anchor=Shape("Recording anchor",sim.Level.Start,Vector2.one*.75f,Violet,5,ring).transform; anchor.gameObject.SetActive(false);
            player=CreateBody("You",Mint,false); player.localPosition=sim.Player;
            livePath=Path("Live recording",Violet,.04f); livePath.gameObject.SetActive(false);
        }
        void Wire(Vector2 from, Vector2 to)
        {
            Vector2 delta=to-from;
            var r=Shape("Signal trace",(from+to)*.5f,new Vector2(Mathf.Abs(delta.x)+.025f,Mathf.Abs(delta.y)+.025f),Hex("5D4C38"),-4); wires.Add(r);
        }
        Transform CreateBody(string name, Color color, bool echo)
        {
            var body=new GameObject(name).transform; body.SetParent(worldRoot,false);
            Shape("Ground shadow",new Vector2(.03f,-.17f),new Vector2(.62f,.32f),new Color(0,0,0,.4f),10,disc,body);
            Shape("Halo",Vector2.zero,Vector2.one*1.6f,new Color(color.r,color.g,color.b,.33f),11,glow,body);
            Shape("Suit",Vector2.zero,new Vector2(.45f,.55f),echo?new Color(color.r,color.g,color.b,.7f):color,12,rounded,body);
            Shape("Visor",new Vector2(.02f,.085f),new Vector2(.33f,.15f),Hex("112D38"),13,rounded,body);
            Shape("Eye",new Vector2(.095f,.085f),new Vector2(.1f,.044f),Color.white,14,rounded,body);
            Shape("Boot",new Vector2(-.12f,-.28f),new Vector2(.11f,.09f),color,12,rounded,body);
            Shape("Boot",new Vector2(.12f,-.28f),new Vector2(.11f,.09f),color,12,rounded,body);
            return body;
        }
        LineRenderer Path(string name, Color color, float width)
        {
            var o=new GameObject(name); o.transform.SetParent(worldRoot,false);
            var l=o.AddComponent<LineRenderer>(); l.material=lineMaterial; l.startColor=l.endColor=new Color(color.r,color.g,color.b,.36f); l.startWidth=l.endWidth=width;
            l.sortingOrder=0; l.numCornerVertices=3; l.numCapVertices=3; l.positionCount=0; return l;
        }
        void SetPath(LineRenderer line,List<EchoFrame> frames)
        {
            int count=(frames.Count+3)/4; line.positionCount=count;
            for(int i=0;i<count;i++) line.SetPosition(i,(Vector3)frames[Mathf.Min(i*4,frames.Count-1)].Position);
        }
        public void Render(float time)
        {
            if(sim==null) return;
            player.localPosition=sim.Player;
            player.localScale=Vector3.one*(1+Mathf.Sin(time*3)*.018f);
            while(echoBodies.Count>sim.Echoes.Count)
            { int n=echoBodies.Count-1; Destroy(echoBodies[n].gameObject); Destroy(echoPaths[n].gameObject); echoBodies.RemoveAt(n); echoPaths.RemoveAt(n); }
            while(echoBodies.Count<sim.Echoes.Count)
            { int i=echoBodies.Count; echoBodies.Add(CreateBody("Echo "+(i+1),i==0?Violet:Hex("85B9FF"),true)); var line=Path("Echo route",Violet,.035f); SetPath(line,sim.Echoes[i].Frames); echoPaths.Add(line); }
            shownEchoes=sim.Echoes.Count;
            for(int i=0;i<shownEchoes;i++) { echoBodies[i].localPosition=sim.Echoes[i].Position; echoBodies[i].localScale=Vector3.one*(.98f+Mathf.Sin(time*4+i)*.025f); }
            for(int i=0;i<plates.Count;i++) plates[i].color=sim.ActivePlates[i]?Mint:Amber;
            for(int i=0;i<doors.Count;i++)
            {
                Color target=sim.Level.Doors[i].Open?new Color(Mint.r,Mint.g,Mint.b,.12f):Amber;
                doors[i].color=Color.Lerp(doors[i].color,target,Time.unscaledDeltaTime*12);
                doors[i].transform.localScale=new Vector3(sim.Level.Doors[i].Open?.07f:.3f,.95f,1);
            }
            for(int i=0;i<memories.Count;i++) { memories[i].gameObject.SetActive(!sim.Collected[i]); memories[i].localPosition=sim.Level.Memories[i]+Vector2.up*Mathf.Sin(time*2+i)*.07f; }
            portal.localScale=Vector3.one*(1.17f+Mathf.Sin(time*2)*.035f);
            anchor.gameObject.SetActive(sim.Recording); anchor.localPosition=sim.Anchor; anchor.localEulerAngles=new Vector3(0,0,time*20);
            livePath.gameObject.SetActive(sim.Recording); if(sim.Recording) SetPath(livePath,sim.Tape);
        }
        void OnDestroy() { if(lineMaterial!=null) Destroy(lineMaterial); }
    }
}
