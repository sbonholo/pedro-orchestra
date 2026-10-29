using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>A rhythm exercise. Add harder ones to <see cref="All"/>; the mini-game handles the rest.</summary>
    public class RhythmExercise
    {
        public string Name;
        public string Instructions;
        public int Bpm;
        public int CountInBeats = 4;
        public int ExerciseBeats = 8;       // length after the count-in (clicks keep going)
        public float[] TapBeats;            // when to tap, in beats after the count-in
        public float MinRhythmSkill;        // recommended level

        public static readonly List<RhythmExercise> All = new List<RhythmExercise>
        {
            new RhythmExercise
            {
                Name = "Four Steady Beats",
                Instructions = "You will hear 4 count-in clicks. Then tap exactly WITH each of the next 8 clicks (two bars of four beats).",
                Bpm = 80, TapBeats = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 }, MinRhythmSkill = 0
            },
            new RhythmExercise
            {
                Name = "Half Notes and Quarters",
                Instructions = "After the count-in, tap on beats 1, 3, 4 of each bar (long – short – short). Skip beat 2.",
                Bpm = 88, TapBeats = new float[] { 0, 2, 3, 4, 6, 7 }, MinRhythmSkill = 35
            },
            new RhythmExercise
            {
                Name = "Off-beat Eighths",
                Instructions = "After the count-in, tap exactly BETWEEN the clicks (on every 'and'). Hardest of the three.",
                Bpm = 76, TapBeats = new float[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f }, MinRhythmSkill = 50
            }
        };
    }

    public enum TapGrade { Perfect, Good, Early, Late, Miss, Extra }

    public struct TapFeedback
    {
        public TapGrade Grade;
        public float OffsetMs;     // negative = early
        public string Text
        {
            get
            {
                switch (Grade)
                {
                    case TapGrade.Perfect: return "PERFECT  " + Signed(OffsetMs);
                    case TapGrade.Good: return "GOOD  " + Signed(OffsetMs);
                    case TapGrade.Early: return "EARLY  " + Signed(OffsetMs);
                    case TapGrade.Late: return "LATE  " + Signed(OffsetMs);
                    case TapGrade.Extra: return "EXTRA TAP";
                    default: return "MISS";
                }
            }
        }
        static string Signed(float ms) { return (ms >= 0 ? "+" : "") + ms.ToString("0") + " ms"; }
    }

    /// <summary>
    /// Scores taps against target times. Pure logic, no Unity — fully testable.
    /// Every target can be matched once; taps far from any target count as extra taps.
    /// </summary>
    public class RhythmScorer
    {
        public const double PerfectWindow = 0.035, GoodWindow = 0.075, OkWindow = 0.13, MatchWindow = 0.25;

        readonly double[] targets;
        readonly bool[] matched;
        readonly float[] points;
        readonly float[] offsets;
        int extraTaps;

        public RhythmScorer(IList<double> targetTimes)
        {
            targets = new double[targetTimes.Count];
            for (int i = 0; i < targets.Length; i++) targets[i] = targetTimes[i];
            matched = new bool[targets.Length];
            points = new float[targets.Length];
            offsets = new float[targets.Length];
        }

        public int TargetCount { get { return targets.Length; } }

        public TapFeedback RegisterTap(double time)
        {
            int best = -1; double bestDist = double.MaxValue;
            for (int i = 0; i < targets.Length; i++)
            {
                if (matched[i]) continue;
                double d = Math.Abs(time - targets[i]);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best < 0 || bestDist > MatchWindow)
            {
                extraTaps++;
                return new TapFeedback { Grade = TapGrade.Extra };
            }
            matched[best] = true;
            double off = time - targets[best];
            offsets[best] = (float)(off * 1000.0);
            var fb = new TapFeedback { OffsetMs = offsets[best] };
            double a = Math.Abs(off);
            if (a <= PerfectWindow) { fb.Grade = TapGrade.Perfect; points[best] = 1f; }
            else if (a <= GoodWindow) { fb.Grade = TapGrade.Good; points[best] = 0.75f; }
            else { fb.Grade = off < 0 ? TapGrade.Early : TapGrade.Late; points[best] = a <= OkWindow ? 0.4f : 0.15f; }
            return fb;
        }

        /// <summary>Targets whose window has passed without a tap.</summary>
        public int CountNewMisses(double now, bool[] reported)
        {
            int c = 0;
            for (int i = 0; i < targets.Length; i++)
                if (!matched[i] && !reported[i] && now > targets[i] + MatchWindow) { reported[i] = true; c++; }
            return c;
        }

        public int Hits { get { int h = 0; foreach (var m in matched) if (m) h++; return h; } }
        public int ExtraTaps { get { return extraTaps; } }

        public float AverageAbsOffsetMs
        {
            get
            {
                float s = 0; int n = 0;
                for (int i = 0; i < targets.Length; i++) if (matched[i]) { s += Math.Abs(offsets[i]); n++; }
                return n > 0 ? s / n : 0f;
            }
        }

        /// <summary>Average signed offset: negative = you rush, positive = you drag.</summary>
        public float AverageOffsetMs
        {
            get
            {
                float s = 0; int n = 0;
                for (int i = 0; i < targets.Length; i++) if (matched[i]) { s += offsets[i]; n++; }
                return n > 0 ? s / n : 0f;
            }
        }

        /// <summary>0 – 1. Extra taps cost a quarter of a beat each.</summary>
        public float Score
        {
            get
            {
                if (targets.Length == 0) return 0f;
                float sum = 0; foreach (var p in points) sum += p;
                return Math.Max(0f, Math.Min(1f, (sum - 0.25f * extraTaps) / targets.Length));
            }
        }

        public string Verdict
        {
            get
            {
                float s = Score;
                if (s >= 0.9f) return "Excellent — rock-steady pulse.";
                if (s >= 0.75f) return "Very good — a reliable pulse.";
                if (s >= 0.55f) return "Good effort — keep listening to the click.";
                if (s >= 0.3f) return "Unsteady — try counting the beats out loud.";
                return "Off the beat — listen to the count-in and try again.";
            }
        }
    }
}
