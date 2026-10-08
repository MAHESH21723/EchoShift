using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoShift
{
    public sealed class EchoGame : MonoBehaviour
    {
        public TMP_FontAsset regularFont, boldFont;
        public EchoSimulation Simulation { get; private set; }
        public int LevelIndex { get; private set; }
        public EchoAudio Audio { get; private set; }
        EchoWorld world; EchoUI ui; Camera gameCamera;
        bool running, won;
        float accumulator;
        public void Awake()
        {
            Application.targetFrameRate=60;
            gameCamera=Camera.main;
            if(gameCamera==null) { var c=new GameObject("Main Camera"); c.tag="MainCamera"; gameCamera=c.AddComponent<Camera>(); c.AddComponent<AudioListener>(); }
            gameCamera.orthographic=true; gameCamera.transform.position=new Vector3(10,5,-20); gameCamera.clearFlags=CameraClearFlags.SolidColor;
            gameCamera.backgroundColor=EchoWorld.Hex("0B131D"); FitCamera();
            Audio=gameObject.AddComponent<EchoAudio>(); Audio.Initialize();
            world=new GameObject("Echo World").AddComponent<EchoWorld>(); world.Initialize(regularFont);
            Simulation=new EchoSimulation(0); world.Build(Simulation);
            ui=new GameObject("Echo Interface",typeof(RectTransform)).AddComponent<EchoUI>(); ui.Initialize(this,regularFont,boldFont);
        }
        void FitCamera() { gameCamera.orthographicSize=Mathf.Max(7.75f,12.2f/Mathf.Max(.5f,gameCamera.aspect)); }
        public void StartLevel(int index)
        {
            LevelIndex=Mathf.Clamp(index,0,2); Simulation=new EchoSimulation(LevelIndex); won=false; accumulator=0;
            world.Build(Simulation); ui.ShowGame(); ui.Refresh(Simulation,LevelIndex); ui.ShowIntro(); Audio.Begin();
        }
        public void SetRunning(bool value) { running=value; accumulator=0; }
        public void Record()
        {
            if(!running) return;
            bool was=Simulation.Recording; if(Simulation.ToggleRecording()) Audio.Play(was?"release":"record");
        }
        public void Undo() { if(running) { Simulation.Undo(); Audio.Play("click"); } }
        public void Restart() { if(Simulation!=null) StartLevel(LevelIndex); }
        public void Pause() { if(running)ui.ShowPause();else if(ui.DialogOpen&&!won){if(ui.MenuVisible)ui.ShowMenu();else ui.CloseDialog();} }
        void OnApplicationFocus(bool focus) { if(!focus && running && ui!=null)ui.ShowPause(); }
        void Update()
        {
            FitCamera();
            var k=Keyboard.current;
            if(k!=null)
            {
                if(k.escapeKey.wasPressedThisFrame) Pause();
                if(running)
                {
                    if(k.eKey.wasPressedThisFrame) Record();
                    if(k.xKey.wasPressedThisFrame) Undo();
                    if(k.rKey.wasPressedThisFrame) Restart();
                    if(k.hKey.wasPressedThisFrame) ui.ShowHint();
                }
            }
            if(running)
            {
                Vector2 input=Vector2.zero;
                if(k!=null) { input.x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0); input.y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0); }
                if(Gamepad.current!=null && input==Vector2.zero) input=Gamepad.current.leftStick.ReadValue();
                int oldMemories=Simulation.MemoryCount,oldEchoes=Simulation.Echoes.Count; bool gate=false; foreach(var d in Simulation.Level.Doors)gate|=d.Open;
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.1f);
                while(accumulator>=1f/60 && running) { Simulation.Step(input,1f/60); accumulator-=1f/60; }
                if(Simulation.MemoryCount>oldMemories)Audio.Play("memory");
                if(Simulation.Echoes.Count>oldEchoes)Audio.Play("release");
                bool newGate=false; foreach(var d in Simulation.Level.Doors)newGate|=d.Open; if(!gate&&newGate)Audio.Play("door");
                if(Simulation.Complete&&!won)
                {
                    won=true; running=false; Audio.Play("complete");
                    PlayerPrefs.SetInt("EchoShift.Unlocked",Mathf.Max(PlayerPrefs.GetInt("EchoShift.Unlocked",0),Mathf.Min(2,LevelIndex+1)));
                    PlayerPrefs.SetInt("EchoShift.Memories."+LevelIndex,Mathf.Max(PlayerPrefs.GetInt("EchoShift.Memories."+LevelIndex,0),Simulation.MemoryCount));
                    PlayerPrefs.Save(); ui.ShowComplete();
                }
            }
            if(Simulation!=null) { world.Render(Time.unscaledTime); ui.Refresh(Simulation,LevelIndex); }
        }
    }
}
