using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EchoShift
{
    public sealed class EchoUI : MonoBehaviour
    {
        EchoGame game;
        TMP_FontAsset regular, bold;
        RectTransform root, menu, hud, modal, card, hero;
        TMP_Text title, brief, status, slots, fragments, clock, recordLabel, soundLabel;
        UnityEngine.UI.Image progress;
        readonly Color ink=EchoWorld.Hex("0B131D"), panel=EchoWorld.Hex("15222F"), muted=EchoWorld.Hex("93A6B3"), white=EchoWorld.Hex("E8F1EE");
        public void Initialize(EchoGame owner,TMP_FontAsset regularFont,TMP_FontAsset boldFont)
        {
            game=owner; regular=regularFont; bold=boldFont;
            var canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>(); root=GetComponent<RectTransform>();
            if(FindFirstObjectByType<EventSystem>()==null) { var es=new GameObject("Event System"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions(); }
            CreateHUD(); CreateMenu();
            modal=Panel(root,"Dialog shade",new Color(ink.r,ink.g,ink.b,.95f));
            modal.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            modal.gameObject.SetActive(false);
            ShowMenu();
        }
        RectTransform Element(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor; r.pivot=pivot; r.anchoredPosition=position; r.sizeDelta=size; return r;
        }
        RectTransform Panel(Transform parent,string name,Color color)
        {
            var r=Element(parent,name,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero); r.anchorMax=Vector2.one;
            var image=r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=false; return r;
        }
        UnityEngine.UI.Image Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var r=Element(parent,name,Vector2.zero,Vector2.zero,new Vector2(x,y),new Vector2(w,h)); var image=r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=false; return image;
        }
        TMP_Text Text(Transform parent,string content,float x,float y,float w,float h,float size,Color color,bool heavy=false,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {
            var r=Element(parent,content,Vector2.zero,Vector2.zero,new Vector2(x,y),new Vector2(w,h)); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=heavy?bold:regular; t.text=content; t.fontSize=size; t.color=color; t.alignment=align; t.raycastTarget=false;
            t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Overflow; return t;
        }
        UnityEngine.UI.Button Button(Transform parent,string label,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var bg=Box(parent,label,x,y,w,h,primary?EchoWorld.Mint:EchoWorld.Hex("1B2C3B")); bg.raycastTarget=true;
            var b=bg.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic=bg;
            var colors=b.colors; colors.normalColor=Color.white; colors.highlightedColor=primary?new Color(.84f,1,.93f):new Color(1.22f,1.22f,1.22f); colors.pressedColor=new Color(.68f,.83f,.78f); colors.selectedColor=Color.white; b.colors=colors;
            var t=Text(bg.transform,label,12,0,w-24,h,19,primary?ink:white,true,TextAlignmentOptions.Center); t.textWrappingMode=TextWrappingModes.NoWrap;
            b.onClick.AddListener(()=>{ game.Audio.Play("click"); action(); EventSystem.current.SetSelectedGameObject(null); }); return b;
        }
        void CreateMenu()
        {
            menu=Panel(root,"Main menu",ink);
            // A reference-sized inner layout stays centered across browser aspect ratios.
            var inner=Element(menu,"Menu layout",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1500,820));
            Text(inner,"E / S",30,747,150,40,27,EchoWorld.Mint,true);
            Text(inner,"TEMPORAL RESEARCH DIVISION     /     EXPERIMENT 003",195,752,650,28,16,muted);
            Text(inner,"AN EXPERIMENT IN COOPERATION",45,610,640,30,19,EchoWorld.Mint,true);
            Text(inner,"ECHO",38,452,640,150,146,white,true);
            Text(inner,"SHIFT",38,305,660,155,146,EchoWorld.Mint,true);
            Text(inner,"Five seconds. A second self.\nA way forward.",48,198,600,90,31,white);
            Button(inner,"ENTER THE LOOP   >",48,107,340,64,()=>game.StartLevel(Mathf.Min(PlayerPrefs.GetInt("EchoShift.Unlocked",0),2)),true);
            Button(inner,"CHAMBERS",409,107,185,64,ShowChambers);
            Button(inner,"HOW TO PLAY",48,32,220,48,()=>ShowHelp(true));
            Text(inner,"A short puzzle journey about helping yourself.",293,42,550,28,17,muted);
            Text(inner,"01 / RECORD     02 / REPLAY     03 / RECONNECT",48,-5,850,25,15,muted);
            HeroArt(inner);
            Text(inner,"SINGLE PLAYER  /  THREE CHAMBERS",940,40,470,26,15,muted,false,TextAlignmentOptions.Right);
        }
        void HeroArt(RectTransform parent)
        {
            hero=Element(parent,"Temporal schematic",Vector2.zero,Vector2.zero,new Vector2(820,200),new Vector2(610,490));
            Box(hero,"Art surface",0,0,610,490,panel);
            for(int i=0;i<12;i++) Box(hero,"Grid",i*55,0,1,490,new Color(.4f,.6f,.7f,.07f));
            for(int i=0;i<10;i++) Box(hero,"Grid",0,i*55,610,1,new Color(.4f,.6f,.7f,.07f));
            Text(hero,"SIGNAL FOUND",30,430,450,26,17,EchoWorld.Mint,true);
            Text(hero,"YOU + YOU",30,390,470,34,30,white,true);
            for(int i=0;i<6;i++) Box(hero,"Recorded trace",125+i*48,207+Mathf.Sin(i*.75f)*65,16,3,EchoWorld.Violet);
            Box(hero,"Anchor",99,176,66,66,EchoWorld.Hex("433E63"));
            Box(hero,"Echo",115,192,34,44,EchoWorld.Violet);
            Box(hero,"Echo visor",119,215,26,11,ink);
            Box(hero,"Player shadow",394,244,96,98,EchoWorld.Hex("183F40"));
            Box(hero,"Player",411,262,62,75,EchoWorld.Mint);
            Box(hero,"Player visor",419,300,46,19,ink);
            Box(hero,"Player eye",448,307,10,5,white);
            Text(hero,"PAST SELF",58,113,170,25,17,EchoWorld.Violet,true,TextAlignmentOptions.Center);
            Text(hero,"PRESENT YOU",349,182,190,26,17,EchoWorld.Mint,true,TextAlignmentOptions.Center);
            Box(hero,"Bottom divider",30,80,550,1,EchoWorld.Hex("344958"));
            Text(hero,"RECORDING CAPACITY",30,30,350,27,15,muted);
            Text(hero,"05.00 s",395,27,185,38,31,white,true,TextAlignmentOptions.Right);
        }
        void CreateHUD()
        {
            hud=Panel(root,"Game HUD",Color.clear);
            var top=Element(hud,"Header",new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,new Vector2(1600,114));
            Box(top,"Header surface",0,0,1600,114,ink); Box(top,"Header line",40,0,1520,1,EchoWorld.Hex("2B3A47"));
            Text(top,"ECHO / SHIFT",40,43,270,38,30,white,true);
            title=Text(top,"01 / FIRST CONTACT",350,51,700,35,25,white,true);
            brief=Text(top,"Leave a little of yourself behind.",350,19,820,26,17,muted);
            var sound=Button(top,"SOUND ON",1190,34,155,45,()=>{game.Audio.Toggle();RefreshSound();}); soundLabel=sound.GetComponentInChildren<TMP_Text>();
            Button(top,"PAUSE",1360,34,112,45,()=>game.Pause()); Button(top,"?",1486,34,70,45,()=>ShowHelp(false));
            var bottom=Element(hud,"Footer",new Vector2(.5f,0),new Vector2(.5f,0),Vector2.zero,new Vector2(1600,132));
            Box(bottom,"Footer surface",0,0,1600,132,ink); Box(bottom,"Footer line",40,131,1520,1,EchoWorld.Hex("2B3A47"));
            slots=Text(bottom,"ECHOES  0 / 1",42,84,320,27,18,EchoWorld.Violet,true);
            fragments=Text(bottom,"MEMORIES  0 / 3",42,50,280,27,17,muted);
            clock=Text(bottom,"00:00",294,83,130,30,20,white);
            Text(bottom,"RECORDING WINDOW",450,89,480,22,14,muted);
            Box(bottom,"Timeline track",450,69,465,5,EchoWorld.Hex("2E3C49")); progress=Box(bottom,"Timeline fill",450,69,0,5,EchoWorld.Violet);
            for(int i=0;i<6;i++) Text(bottom,i+"s",443+i*93,42,45,20,12,muted);
            var record=Button(bottom,"E   RECORD",972,63,221,48,()=>game.Record(),true); recordLabel=record.GetComponentInChildren<TMP_Text>();
            Button(bottom,"X   UNDO",1208,63,160,48,()=>game.Undo()); Button(bottom,"R   RESTART",1383,63,174,48,()=>game.Restart());
            status=Text(bottom,"Press E to record your first echo.",450,10,1080,29,17,white);
            Text(bottom,"WASD / ARROWS    MOVE",42,11,385,27,15,muted);
        }
        public void Refresh(EchoSimulation s,int index)
        {
            title.text=(index+1).ToString("00")+" / "+s.Level.Title; brief.text=s.Level.Subtitle;
            slots.text="ECHOES  "+s.Echoes.Count+" / "+s.Level.EchoLimit; fragments.text="MEMORIES  "+s.MemoryCount+" / 3";
            clock.text=TimeSpan.FromSeconds(s.Elapsed).ToString(@"mm\:ss");
            progress.rectTransform.sizeDelta=new Vector2(465*(s.Recording?s.RecordingTime/5:0),5);
            recordLabel.text=s.Recording?"E   RELEASE":"E   RECORD"; status.text=s.Notice; status.color=s.Recording?EchoWorld.Violet:white;
        }
        public void RefreshSound() { soundLabel.text=game.Audio.Muted?"SOUND OFF":"SOUND ON"; }
        public void ShowMenu() { menu.gameObject.SetActive(true); hud.gameObject.SetActive(false); if(modal!=null) modal.gameObject.SetActive(false); game.SetRunning(false); }
        public void ShowGame() { menu.gameObject.SetActive(false); hud.gameObject.SetActive(true); modal.gameObject.SetActive(false); RefreshSound(); }
        RectTransform Dialog(string eyebrow,string heading,string body,float height=490)
        {
            if(card!=null) Destroy(card.gameObject);
            modal.gameObject.SetActive(true); game.SetRunning(false);
            card=Element(modal,"Dialog",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(780,height));
            Panel(card,"Card surface",panel); Box(card,"Accent",0,height-4,780,4,EchoWorld.Mint);
            Text(card,eyebrow,44,height-68,700,25,16,EchoWorld.Mint,true);
            Text(card,heading,40,height-143,704,66,43,white,true);
            Text(card,body,44,130,686,height-284,22,muted);
            return card;
        }
        public void ShowIntro()
        {
            var s=game.Simulation;
            Dialog("CHAMBER "+(game.LevelIndex+1).ToString("00")+" / 03",s.Level.Title,s.Level.Brief+"\n\n"+s.Level.EchoLimit+" echo slot"+(s.Level.EchoLimit>1?"s":"")+"  /  3 optional memory fragments");
            Button(card,"BEGIN   >",44,42,300,60,CloseDialog,true); Button(card,"CONTROLS",366,42,370,60,()=>ShowHelp(false));
        }
        public void CloseDialog() { modal.gameObject.SetActive(false); game.SetRunning(true); game.Audio.Begin(); }
        public void ShowPause()
        {
            Dialog("TIME IS ON YOUR SIDE","TAKE A BREATH","Your echoes are paused exactly where you left them.");
            Button(card,"RESUME",44,42,220,60,CloseDialog,true); Button(card,"RESTART",280,42,215,60,()=>game.Restart()); Button(card,"MENU",511,42,225,60,ShowMenu);
        }
        public void ShowHelp(bool fromMenu)
        {
            Dialog("FIELD GUIDE","COOPERATE WITH YOURSELF","WASD / Arrow keys  -  Move\nE  -  Start recording; press again to release\nX  -  Cancel recording / remove latest echo\nR  -  Restart    Esc  -  Pause    H  -  Hint\n\nYou return to your recording anchor. Your echo replays the route, then holds its last position. Use echoes to hold plates while you cross doors.\n\nMemory fragments are optional. There is no time limit.",660);
            Button(card,fromMenu?"BACK":"GOT IT",44,38,fromMenu?692:338,60,()=>{if(fromMenu)ShowMenu();else CloseDialog();},true);
            if(!fromMenu)Button(card,"CHAMBER HINT",398,38,338,60,ShowHint);
        }
        public void ShowHint()
        {
            Dialog("A GENTLE NUDGE","FOLLOW THE SIGNAL",game.Simulation.Level.Hint,530);
            Button(card,"BACK TO CHAMBER",44,42,692,60,CloseDialog,true);
        }
        public void ShowChambers()
        {
            Dialog("SELECT AN EXPERIMENT","THE CHAMBERS","",520);
            int unlocked=PlayerPrefs.GetInt("EchoShift.Unlocked",0);
            for(int i=0;i<3;i++)
            {
                int level=i; string label=(i+1).ToString("00")+"   "+Chamber.Create(i).Title;
                if(i>unlocked) label+="   / LOCKED";
                var b=Button(card,label,44,263-i*73,692,59,()=>game.StartLevel(level),i<=unlocked); b.interactable=i<=unlocked;
            }
            Button(card,"BACK",44,32,692,52,ShowMenu);
        }
        public void ShowComplete()
        {
            var s=game.Simulation; bool last=game.LevelIndex==2;
            string body="Memories recovered: "+s.MemoryCount+" / 3\nEchoes recorded: "+s.RecordingsMade+"\nChamber time: "+TimeSpan.FromSeconds(s.Elapsed).ToString(@"mm\:ss")+"\n\n"+(last?"Three chambers. A few past selves. One way forward.\nThank you for playing Echo Shift.":s.Level.Subtitle);
            Dialog(last?"EXPERIMENT COMPLETE":"CHAMBER SYNCHRONIZED",last?"YOU FOUND YOUR WAY.":"BETTER TOGETHER.",body,560);
            Button(card,last?"CHAMBERS":"NEXT CHAMBER   >",44,42,440,60,()=>{if(last)ShowChambers();else game.StartLevel(game.LevelIndex+1);},true);
            Button(card,"REPLAY",506,42,230,60,()=>game.Restart());
        }
        public bool DialogOpen => modal!=null && modal.gameObject.activeSelf;
        public bool MenuVisible => menu!=null && menu.gameObject.activeSelf;
    }
}
