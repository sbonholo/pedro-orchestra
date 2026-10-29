using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    public enum RhythmPhase { Ready, CountIn, Playing, Done }

    /// <summary>
    /// Interactive rhythm practice. Plays a sample-accurate click track and measures each tap
    /// (Space / Enter / the on-screen pad) against the beat. Points come only from real taps.
    /// </summary>
    public class RhythmMiniGame : MonoBehaviour
    {
        public RhythmPhase Phase { get; private set; }
        public RhythmExercise Exercise { get; private set; }
        public RhythmScorer Scorer { get; private set; }
        public event Action<TapFeedback> Feedback;
        public event Action Finished;

        public static float CalibrationMs;   // positive = your device/headphones add delay

        AudioSource source;
        double realStart;      // realtime clock value when the first click sounds
        double spb;
        bool[] missReported;
        double lastTargetTime;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.bypassReverbZones = true;
        }

        /// <summary>Seconds since the first count-in click (negative before it).</summary>
        public double Elapsed { get { return Time.realtimeSinceStartupAsDouble - realStart; } }
        public double SecondsPerBeat { get { return spb; } }

        /// <summary>Beat number since the first count-in click (0-based, fractional).</summary>
        public double BeatPosition { get { return spb > 0 ? Elapsed / spb : 0; } }

        public void Begin(RhythmExercise ex)
        {
            Exercise = ex;
            spb = 60.0 / ex.Bpm;
            int totalBeats = ex.CountInBeats + ex.ExerciseBeats + 1;
            var clicks = new List<double>();
            var accents = new List<bool>();
            for (int i = 0; i < totalBeats; i++) { clicks.Add(i * spb); accents.Add(i % 4 == 0); }

            var targets = new List<double>();
            foreach (var b in ex.TapBeats) targets.Add((ex.CountInBeats + b) * spb);
            lastTargetTime = targets[targets.Count - 1];
            Scorer = new RhythmScorer(targets);
            missReported = new bool[targets.Count];

            int sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            var data = StringSynth.ClickTrack(clicks, accents, sr, totalBeats * spb);
            var clip = AudioClip.Create("Click Track", data.Length, 1, sr, false);
            clip.SetData(data, 0);
            if (source.clip != null) Destroy(source.clip);
            source.clip = clip;

            double lead = 0.6;
            double dspStart = AudioSettings.dspTime + lead;
            source.PlayScheduled(dspStart);
            int bufferLength, bufferCount;
            AudioSettings.GetDSPBufferSize(out bufferLength, out bufferCount);
            double outputLatency = (double)bufferLength * bufferCount / sr;
            realStart = Time.realtimeSinceStartupAsDouble + lead + outputLatency;
            Phase = RhythmPhase.CountIn;
        }

        public void Cancel()
        {
            source.Stop();
            Phase = RhythmPhase.Ready;
        }

        /// <summary>Called by the on-screen pad, or by Space/Enter in Update.</summary>
        public void Tap()
        {
            if (Phase != RhythmPhase.CountIn && Phase != RhythmPhase.Playing) return;
            double t = Elapsed - CalibrationMs / 1000.0;
            if (t < Exercise.CountInBeats * spb - RhythmScorer.MatchWindow) return; // taps during count-in are ignored
            var fb = Scorer.RegisterTap(t);
            if (Feedback != null) Feedback(fb);
        }

        void Update()
        {
            if (Phase != RhythmPhase.CountIn && Phase != RhythmPhase.Playing) return;
            if (InputHelper.KeyDown(GameKey.Space) || InputHelper.KeyDown(GameKey.Enter)) Tap();

            double e = Elapsed;
            if (Phase == RhythmPhase.CountIn && e >= Exercise.CountInBeats * spb - 0.05) Phase = RhythmPhase.Playing;

            int misses = Scorer.CountNewMisses(e - CalibrationMs / 1000.0, missReported);
            for (int i = 0; i < misses; i++) if (Feedback != null) Feedback(new TapFeedback { Grade = TapGrade.Miss });

            if (e > lastTargetTime + RhythmScorer.MatchWindow + 0.3)
            {
                Phase = RhythmPhase.Done;
                if (Finished != null) Finished();
            }
        }
    }
}
