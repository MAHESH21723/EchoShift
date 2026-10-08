using UnityEngine;

namespace EchoShift
{
    public sealed class EchoAudio : MonoBehaviour
    {
        AudioSource music, effects;
        AudioClip start, release, pickup, door, complete, click;
        public bool Muted { get; private set; }
        public void Initialize()
        {
            music=gameObject.AddComponent<AudioSource>(); effects=gameObject.AddComponent<AudioSource>();
            music.loop=true; music.volume=.15f; effects.volume=.4f;
            music.clip=Tone("Atmosphere",12,110,165,true); start=Tone("Record",.22f,420,650); release=Tone("Echo released",.45f,240,720);
            pickup=Tone("Memory",.16f,880,1320); door=Tone("Door",.22f,160,300); complete=Tone("Synchronized",.9f,330,660); click=Tone("Interface",.07f,600,540);
            Muted=PlayerPrefs.GetInt("EchoShift.Muted",0)==1; Apply();
        }
        AudioClip Tone(string name,float seconds,float from,float to,bool ambient=false)
        {
            const int rate=22050; int count=Mathf.CeilToInt(seconds*rate); float[] data=new float[count];
            double phase=0;
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate, p=(float)i/count; phase+=2*System.Math.PI*Mathf.Lerp(from,to,p)/rate;
                float envelope=ambient?Mathf.Sin(Mathf.PI*p)*.45f:Mathf.Min(1,t*70)*Mathf.Pow(1-p,2);
                float sample=ambient?(Mathf.Sin(2*Mathf.PI*110*t)+.4f*Mathf.Sin(2*Mathf.PI*164.83333f*t)+.3f*Mathf.Sin(2*Mathf.PI*220*t))*.45f:(float)System.Math.Sin(phase);
                data[i]=sample*envelope;
            }
            var clip=AudioClip.Create(name,count,1,rate,false); clip.SetData(data,0); return clip;
        }
        public void Begin() { if(!music.isPlaying) music.Play(); }
        public void Toggle() { Muted=!Muted; PlayerPrefs.SetInt("EchoShift.Muted",Muted?1:0); PlayerPrefs.Save(); Apply(); }
        void Apply() { if(music!=null) music.mute=Muted; if(effects!=null) effects.mute=Muted; }
        public void Play(string cue)
        {
            if(effects==null) return;
            AudioClip clip=cue=="record"?start:cue=="release"?release:cue=="memory"?pickup:cue=="door"?door:cue=="complete"?complete:click;
            effects.PlayOneShot(clip);
        }
    }
}
