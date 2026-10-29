using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Runs a performance: arranges the piece, renders each musician's part with their own skills,
    /// synthesizes one audio track PER MUSICIAN (on a background thread), starts every track on the
    /// same audio-clock instant, drives the animations, then scores the result.
    /// Each musician's sound comes from their own AudioSource on stage — four real, separate parts.
    /// </summary>
    public class PerformanceController : MonoBehaviour
    {
        public bool IsBusy { get; private set; }
        public string Status { get; private set; }
        public float Progress { get; private set; }
        public PerformanceContext Context { get; private set; }
        public int PlayerCount { get; private set; }

        // "You conduct" mode: the player's taps are scored against the beats and drive the conductor's baton.
        public bool PlayerConducting { get; private set; }
        public int ConductTaps { get; private set; }
        public double LastConductTapDsp { get; private set; }
        public TapFeedback LastConductFeedback { get; private set; }
        RhythmScorer conductScorer;

        public void ConductTap()
        {
            if (!PlayerConducting || conductScorer == null) return;
            double now = AudioSettings.dspTime;
            LastConductFeedback = conductScorer.RegisterTap(now);
            LastConductTapDsp = now; ConductTaps++;
        }

        // Timing of the piece being played (for the conductor): dsp time of beat 0, seconds per beat, metre.
        public bool HasTiming { get; private set; }
        public double BeatZeroDsp { get; private set; }
        public double MusicEndDsp { get; private set; }
        public float SecondsPerBeat { get; private set; }
        public int BeatsPerBar { get; private set; }
        public float PickupBeats { get; private set; }

        StageManager stage;
        readonly List<MusicianView> activeViews = new List<MusicianView>();
        Coroutine running;
        int seedCounter;

        public void Init(StageManager stageManager) { stage = stageManager; }

        public void Perform(PieceDefinition piece, ArrangementMode mode, List<MusicianData> players, PerformanceContext ctx,
            float preparation, Action<PerformanceResult> onFinished, InstrumentType soloist = InstrumentType.Violin, bool playerConducts = false)
        {
            if (IsBusy) Stop();
            PlayerConducting = playerConducts && ctx != PerformanceContext.Audition && players.Count > 1;
            conductScorer = null; ConductTaps = 0;
            running = StartCoroutine(Run(piece, mode, players, ctx, preparation, onFinished, soloist));
        }

        public void Stop()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            foreach (var v in activeViews) if (v != null) { v.Audio.Stop(); v.EndPerformance(); ReleaseClip(v); }
            activeViews.Clear();
            stage.HideAuditionCandidate();
            IsBusy = false;
            HasTiming = false;
            PlayerConducting = false; conductScorer = null;
            Status = null;
        }

        static void ReleaseClip(MusicianView v)
        {
            var clip = v.Audio.clip;
            v.Audio.clip = null;
            if (clip != null) Destroy(clip);
        }

        IEnumerator Run(PieceDefinition piece, ArrangementMode mode, List<MusicianData> players, PerformanceContext ctx,
            float prep, Action<PerformanceResult> onFinished, InstrumentType soloist)
        {
            IsBusy = true;
            Context = ctx;
            PlayerCount = players.Count;
            Progress = 0f;
            Status = "Preparing " + piece.Title + "…";

            var instruments = new List<InstrumentType>();
            foreach (var m in players) instruments.Add(m.instrument);
            var arrangement = Arranger.Arrange(piece, mode, instruments, soloist);
            float tempoScale = PerformanceRenderer.EnsembleTempoScale(players, arrangement);
            float bpm = piece.Bpm * tempoScale;

            // Musicians sharing a part (e.g. 3 violins) are balanced so a section is not 3x louder.
            var sectionCount = new Dictionary<InstrumentType, int>();
            foreach (var m in players) { int c; sectionCount.TryGetValue(m.instrument, out c); sectionCount[m.instrument] = c + 1; }

            var parts = new List<RenderedPart>();
            foreach (var m in players)
            {
                var rp = PerformanceRenderer.Render(m, arrangement.Parts[m.instrument], piece, prep, ctx, bpm, ++seedCounter * 7919 + m.appearanceSeed);
                rp.SectionGain = 1f / Mathf.Sqrt(sectionCount[m.instrument]) * (players.Count > 1 ? 0.85f : 1.1f) * arrangement.Parts[m.instrument].Gain;
                parts.Add(rp);
            }

            // Synthesize off the main thread so the game never freezes.
            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            double length = 0;
            foreach (var p in parts) length = Math.Max(length, p.LengthSec);
            var buffers = new float[parts.Count][];
            int seedBase = seedCounter;
            // Recorded samples where available (loaded here, on the main thread); synthesizer otherwise.
            var sampleSets = new SampleSet[parts.Count];
            for (int i = 0; i < parts.Count; i++) sampleSets[i] = SampleLibrary.Get(parts[i].Instrument.Type);
            var task = Task.Run(() =>
            {
                for (int i = 0; i < parts.Count; i++)
                    buffers[i] = sampleSets[i] != null
                        ? SampleVoice.RenderPart(parts[i], sampleSets[i], sampleRate, length, seedBase * 31 + i)
                        : StringSynth.RenderPart(parts[i], sampleRate, length, seedBase * 31 + i);
            });
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                Status = null;
                IsBusy = false;
                running = null;
                if (GameManager.Instance != null) GameManager.Instance.Notify("Audio rendering failed. Open Window > General > Console and send me the red error.");
                yield break;
            }

            // Bind audio to the musicians on stage (or the audition candidate).
            activeViews.Clear();
            for (int i = 0; i < parts.Count; i++)
            {
                MusicianView view = ctx == PerformanceContext.Audition ? stage.ShowAuditionCandidate(parts[i].Musician) : stage.Get(parts[i].Musician.id);
                if (view == null) continue;
                var clip = AudioClip.Create(parts[i].Musician.FullName + " - " + piece.Title, buffers[i].Length, 1, sampleRate, false);
                clip.SetData(buffers[i], 0);
                view.Audio.clip = clip;
                activeViews.Add(view);
            }

            double dspStart = AudioSettings.dspTime + 0.25;
            for (int i = 0; i < activeViews.Count; i++)
            {
                activeViews[i].Audio.PlayScheduled(dspStart);
                var part = parts.Find(p => p.Musician.id == activeViews[i].Data.id);
                if (part != null) activeViews[i].BeginPerformance(part.Notes, dspStart + StringSynth.LeadInSec);
            }

            double end = dspStart + StringSynth.LeadInSec + length + 0.6;
            BeatZeroDsp = dspStart + StringSynth.LeadInSec; MusicEndDsp = BeatZeroDsp + length;
            SecondsPerBeat = 60f / bpm; BeatsPerBar = piece.BeatsPerBar; PickupBeats = piece.PickupBeats; HasTiming = true;
            if (PlayerConducting)
            {
                var beats = new List<double>();
                for (double b = BeatZeroDsp; b < MusicEndDsp - 0.05; b += SecondsPerBeat) beats.Add(b);
                conductScorer = new RhythmScorer(beats);
            }
            Status = (ctx == PerformanceContext.Concert ? "Concert: " : ctx == PerformanceContext.Audition ? "Audition: " : ctx == PerformanceContext.Rehearsal ? "Rehearsing: " : "Playing: ")
                     + piece.Title + (mode == ArrangementMode.SeparateParts ? " (separate parts)" : mode == ArrangementMode.Solo ? " (" + InstrumentCatalog.Get(soloist).DisplayName.ToLowerInvariant() + " solo)" : " (same melody)")
                     + (tempoScale < 0.99f ? " — tempo " + Mathf.RoundToInt(tempoScale * 100) + "%" : "");
            while (AudioSettings.dspTime < end)
            {
                Progress = (float)((AudioSettings.dspTime - dspStart) / (end - dspStart));
                yield return null;
            }

            foreach (var v in activeViews) { v.EndPerformance(); ReleaseClip(v); }
            activeViews.Clear();
            HasTiming = false;
            if (ctx == PerformanceContext.Audition) stage.HideAuditionCandidate();

            var result = PerformanceEvaluator.Evaluate(parts, piece, mode, ctx, tempoScale);
            if (conductScorer != null) PerformanceEvaluator.ApplyConducting(result, conductScorer.Score);
            conductScorer = null; PlayerConducting = false;
            IsBusy = false;
            Status = null;
            running = null;
            if (onFinished != null) onFinished(result);
        }
    }
}
