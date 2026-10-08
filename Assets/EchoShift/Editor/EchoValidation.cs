using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShift.Editor
{
    public static class EchoValidation
    {
        static readonly List<string> checks=new List<string>();
        static void Check(bool condition,string name) { if(!condition)throw new Exception("Echo Shift validation failed: "+name); checks.Add(name); }
        static void Wait(EchoSimulation s,float duration) { for(float t=0;t<duration;t+=1f/60)s.Step(Vector2.zero,1f/60); }
        static void Walk(EchoSimulation s,params Vector2[] route)
        {
            foreach(var target in route)
            {
                int safety=0;
                while(Vector2.Distance(s.Player,target)>.015f && !s.Complete)
                {
                    var delta=target-s.Player; s.Step(delta.normalized,Mathf.Min(1f/60,delta.magnitude/EchoSimulation.Speed));
                    if(++safety>1200)throw new Exception("Route blocked en route to "+target+" at "+s.Player+" in "+s.Level.Title);
                }
            }
        }
        public static string Run()
        {
            checks.Clear();
            var tape=new EchoRecording(new List<EchoFrame>{new EchoFrame(0,Vector2.zero),new EchoFrame(1,new Vector2(2,0))});
            Check(Vector2.Distance(tape.Sample(.5f),Vector2.right)<.001f,"Echo interpolates between recorded positions");
            Check(tape.Sample(-1)==Vector2.zero && tape.Sample(10)==new Vector2(2,0),"Echo clamps playback and holds its endpoint");
            var blocked=new EchoSimulation(0); for(int i=0;i<240;i++)blocked.Step(Vector2.right,1f/60);
            Check(blocked.Player.x<9.3f && !blocked.Level.Doors[0].Open,"Closed gate blocks the player");
            var timeout=new EchoSimulation(0); var anchor=timeout.Player; timeout.ToggleRecording();
            for(int i=0;i<310;i++)timeout.Step(Vector2.up,1f/60);
            Check(!timeout.Recording && timeout.Echoes.Count==1,"Recording automatically releases at five seconds");
            Check(timeout.Echoes[0].Duration<=5.001f,"Recording duration stays within its limit");
            Check(timeout.Player.y<anchor.y+1,"Auto release restores the recording anchor");
            Wait(timeout,6); Check(timeout.Echoes[0].Holding,"Completed echo continues holding");
            Check(!timeout.ToggleRecording(),"Echo slot limit is enforced"); timeout.Undo(); Check(timeout.Echoes.Count==0,"Undo frees an echo slot");
            timeout.ToggleRecording(); Wait(timeout,.3f); timeout.Undo(); Check(!timeout.Recording && timeout.Echoes.Count==0,"Undo cancels an unfinished recording");
            var diagonal=new EchoSimulation(0); diagonal.Step(Vector2.one,.2f);
            Check(Mathf.Abs(Vector2.Distance(diagonal.Player,diagonal.Level.Start)-.72f)<.001f,"Diagonal movement is normalized");
            for(int index=0;index<3;index++)
            {
                var s=new EchoSimulation(index); var start=s.Player;
                Check(s.ToggleRecording(),"Room "+(index+1)+" accepts its first recording");
                if(index==0) Walk(s,new Vector2(6,5),new Vector2(6,7));
                else if(index==1) Walk(s,new Vector2(5,5),new Vector2(5,7));
                else Walk(s,new Vector2(5,5),new Vector2(5,8));
                Check(s.FinishRecording(),"Room "+(index+1)+" releases a valid echo");
                Check(Vector2.Distance(s.Player,start)<.001f,"Room "+(index+1)+" returns to its exact anchor");
                Wait(s,s.Echoes[0].Duration+.15f); Check(s.ActivePlates[0],"Room "+(index+1)+" echo holds plate A");
                if(index==1)
                {
                    Walk(s,new Vector2(10,5)); s.ToggleRecording(); Walk(s,new Vector2(11,5),new Vector2(11,3)); s.FinishRecording();
                    Wait(s,s.Echoes[1].Duration+.15f); Check(s.ActivePlates[1],"Second echo holds plate B across the first gate");
                }
                if(index==2)
                {
                    Check(!s.Level.Doors[0].Open,"Final gate needs both plates, not just one");
                    Walk(s,new Vector2(5,5),new Vector2(5,2),new Vector2(9,2)); s.ToggleRecording(); Walk(s,new Vector2(12,2)); s.FinishRecording();
                    Wait(s,s.Echoes[1].Duration+.15f); Check(s.Level.Doors[0].Open,"Final gate opens with both echoes active");
                    Walk(s,new Vector2(13,2),new Vector2(13,5));
                }
                Walk(s,new Vector2(18,5)); Check(s.Complete,"Room "+(index+1)+" is completable through ordinary movement and recordings");
                Check(s.MemoryCount>=1,"Room "+(index+1)+" collects memory fragments along a recorded route");
                float elapsed=s.Elapsed; s.Step(Vector2.right,.2f); Check(s.Elapsed==elapsed,"Completed room stops simulating");
            }
            return checks.Count+" checks passed\n\n"+string.Join("\n",checks);
        }
    }
}
