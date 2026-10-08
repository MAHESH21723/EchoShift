using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShift
{
    public sealed class Chamber
    {
        public string Title, Subtitle, Brief, Hint;
        public int Width = 21, Height = 11, EchoLimit;
        public Vector2 Start, Exit;
        public readonly List<Rect> Walls = new List<Rect>();
        public readonly List<Vector2> Plates = new List<Vector2>();
        public readonly List<Vector2> Memories = new List<Vector2>();
        public readonly List<Door> Doors = new List<Door>();
        public void Wall(float x, float y, float w = 1, float h = 1) => Walls.Add(new Rect(x - .5f, y - .5f, w, h));
        public void Partition(int x, int gap)
        { for (int y = 1; y < Height - 1; y++) if (y != gap) Wall(x, y); }
        public static Chamber Create(int index)
        {
            var c = new Chamber();
            for (int x = 0; x < c.Width; x++) { c.Wall(x, 0); c.Wall(x, c.Height - 1); }
            for (int y = 1; y < c.Height - 1; y++) { c.Wall(0, y); c.Wall(c.Width - 1, y); }
            if (index == 0)
            {
                c.Title = "FIRST CONTACT"; c.Subtitle = "Leave a little of yourself behind.";
                c.Brief = "Record a walk onto plate A. Release your echo, then head through the open door.";
                c.Hint = "Press E to record. Walk to the amber plate A and press E again. Your echo retraces the walk and stays on the plate. You return to your starting point; now walk through the open door.";
                c.Start = new Vector2(3, 5); c.Exit = new Vector2(18, 5); c.EchoLimit = 1;
                c.Partition(10, 5); c.Plates.Add(new Vector2(6, 7)); c.Doors.Add(new Door(new Vector2(10, 5), 0));
                c.Wall(14, 3, 3, 1); c.Wall(14, 7, 3, 1);
                c.Memories.AddRange(new[] { new Vector2(6, 7), new Vector2(13, 5), new Vector2(17, 8) });
            }
            else if (index == 1)
            {
                c.Title = "CHAIN REACTION"; c.Subtitle = "One small action opens another possibility.";
                c.Brief = "Two doors. Two echoes. Start a new recording after passing the first door.";
                c.Hint = "Leave your first echo on A. Pass door A, then begin a new recording inside the middle room. Leave your second echo on B and pass door B. Each recording returns you to its own starting point.";
                c.Start = new Vector2(3, 5); c.Exit = new Vector2(18, 5); c.EchoLimit = 2;
                c.Partition(8, 5); c.Partition(14, 5);
                c.Plates.Add(new Vector2(5, 7)); c.Plates.Add(new Vector2(11, 3));
                c.Doors.Add(new Door(new Vector2(8, 5), 0)); c.Doors.Add(new Door(new Vector2(14, 5), 1));
                c.Wall(10, 7, 3, 1); c.Wall(16, 2, 2, 1);
                c.Memories.AddRange(new[] { new Vector2(5, 7), new Vector2(11, 3), new Vector2(18, 8) });
            }
            else
            {
                c.Title = "IN GOOD COMPANY"; c.Subtitle = "You were never meant to do this alone.";
                c.Brief = "Both plates must stay active together. Find two anchors and reunite at the exit.";
                c.Hint = "Record one route ending on A and a second route ending on B. Both echoes must hold their plates together. Start recordings anywhere: move closer to a plate before recording if five seconds is not enough.";
                c.Start = new Vector2(3, 5); c.Exit = new Vector2(18, 5); c.EchoLimit = 2;
                c.Partition(15, 5); c.Wall(7, 3, 1, 5); c.Wall(11, 4, 1, 4);
                c.Plates.Add(new Vector2(5, 8)); c.Plates.Add(new Vector2(12, 2));
                c.Doors.Add(new Door(new Vector2(15, 5), 0, 1));
                c.Memories.AddRange(new[] { new Vector2(5, 8), new Vector2(9, 5), new Vector2(12, 2) });
            }
            return c;
        }
    }
    public sealed class Door
    {
        public readonly Vector2 Position;
        public readonly int[] Required;
        public bool Open;
        public Rect Bounds => new Rect(Position.x - .5f, Position.y - .5f, 1, 1);
        public Door(Vector2 position, params int[] required) { Position = position; Required = required; }
    }
    public struct EchoFrame
    {
        public float Time;
        public Vector2 Position;
        public EchoFrame(float time, Vector2 position) { Time = time; Position = position; }
    }
    public sealed class EchoRecording
    {
        public readonly List<EchoFrame> Frames;
        public float Time;
        public float Duration => Frames[Frames.Count - 1].Time;
        public Vector2 Position => Sample(Time);
        public bool Holding => Time >= Duration;
        public EchoRecording(List<EchoFrame> frames) { Frames = new List<EchoFrame>(frames); }
        public Vector2 Sample(float time)
        {
            if (time <= 0) return Frames[0].Position;
            if (time >= Duration) return Frames[Frames.Count - 1].Position;
            int low = 0, high = Frames.Count - 1;
            while (high - low > 1) { int mid = (low + high) / 2; if (Frames[mid].Time <= time) low = mid; else high = mid; }
            return Vector2.Lerp(Frames[low].Position, Frames[high].Position, Mathf.InverseLerp(Frames[low].Time, Frames[high].Time, time));
        }
    }
    // Fixed-step simulation, independent from rendering and input, shared with editor validation.
    public sealed class EchoSimulation
    {
        public const float RecordLimit = 5f, Speed = 3.6f, Radius = .22f;
        public readonly Chamber Level;
        public readonly List<EchoRecording> Echoes = new List<EchoRecording>();
        public readonly List<EchoFrame> Tape = new List<EchoFrame>();
        public readonly bool[] ActivePlates, Collected;
        public Vector2 Player { get; private set; }
        public Vector2 Anchor { get; private set; }
        public bool Recording { get; private set; }
        public bool Complete { get; private set; }
        public float RecordingTime { get; private set; }
        public float Elapsed { get; private set; }
        public int RecordingsMade { get; private set; }
        public int MemoryCount { get { int n = 0; foreach (bool b in Collected) if (b) n++; return n; } }
        public string Notice { get; private set; }
        public EchoSimulation(int index)
        {
            Level = Chamber.Create(index); Player = Level.Start; Anchor = Player;
            ActivePlates = new bool[Level.Plates.Count]; Collected = new bool[Level.Memories.Count];
            Notice = "Find your first anchor. Press E to record.";
        }
        public bool ToggleRecording()
        {
            if (Complete) return false;
            if (Recording) return FinishRecording();
            if (Echoes.Count >= Level.EchoLimit) { Notice = "Echo slots full. Press X to undo the latest echo, or R to restart."; return false; }
            Recording = true; RecordingTime = 0; Anchor = Player; Tape.Clear(); Tape.Add(new EchoFrame(0, Player));
            Notice = "Recording your route. Press E to release early."; return true;
        }
        public bool FinishRecording()
        {
            if (!Recording || RecordingTime < .15f) return false;
            Recording = false; Echoes.Add(new EchoRecording(Tape)); RecordingsMade++;
            Player = Anchor; Notice = "Echo released. It will hold its final position."; UpdatePlates(); return true;
        }
        public void Undo()
        {
            if (Recording) { Recording = false; Player = Anchor; Tape.Clear(); Notice = "Recording cancelled. Back at your anchor."; }
            else if (Echoes.Count > 0) { Echoes.RemoveAt(Echoes.Count - 1); Notice = "Latest echo cleared. A slot is available."; }
            else Notice = "No echoes to clear. Press E to begin recording.";
            UpdatePlates();
        }
        public void Step(Vector2 input, float dt)
        {
            if (Complete || dt <= 0) return;
            float remaining = Mathf.Min(dt, .25f);
            while (remaining > .00001f)
            {
                float step = Mathf.Min(remaining, 1f / 60); remaining -= step; Elapsed += step;
                foreach (var echo in Echoes) echo.Time += step;
                UpdatePlates();
                Vector2 delta = Vector2.ClampMagnitude(input, 1) * Speed * step;
                Move(new Vector2(delta.x, 0)); Move(new Vector2(0, delta.y)); UpdatePlates();
                for (int i = 0; i < Collected.Length; i++) if (!Collected[i] && Vector2.Distance(Player, Level.Memories[i]) < .48f) Collected[i] = true;
                if (Recording)
                {
                    RecordingTime = Mathf.Min(RecordingTime + step, RecordLimit); Tape.Add(new EchoFrame(RecordingTime, Player));
                    if (RecordingTime >= RecordLimit - .0001f) FinishRecording();
                }
                if (!Recording && Vector2.Distance(Player, Level.Exit) < .46f) { Complete = true; Notice = "Chamber synchronized."; break; }
            }
        }
        void Move(Vector2 delta)
        {
            Vector2 next = Player + delta;
            foreach (Rect wall in Level.Walls) if (Intersects(next, wall)) return;
            foreach (var door in Level.Doors) if (!door.Open && Intersects(next, door.Bounds)) return;
            Player = next;
        }
        public static bool Intersects(Vector2 center, Rect rect)
        {
            return center.x + Radius > rect.xMin && center.x - Radius < rect.xMax && center.y + Radius > rect.yMin && center.y - Radius < rect.yMax;
        }
        void UpdatePlates()
        {
            for (int i = 0; i < ActivePlates.Length; i++)
            {
                bool active = Vector2.Distance(Player, Level.Plates[i]) < .48f;
                foreach (var echo in Echoes) active |= Vector2.Distance(echo.Position, Level.Plates[i]) < .48f;
                ActivePlates[i] = active;
            }
            foreach (var door in Level.Doors)
            {
                bool open = true; foreach (int plate in door.Required) open &= ActivePlates[plate];
                door.Open = open || Intersects(Player, door.Bounds);
            }
        }
    }
}
