using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    public enum PerformanceContext { Preview, Rehearsal, Concert, Audition }

    /// <summary>One note exactly as the musician will play it — imperfections included.</summary>
    public class RenderedNote
    {
        public double StartSec;
        public double DurationSec;
        public int WrittenMidi;
        public float PlayedMidi;        // fractional = out of tune
        public float Velocity;          // 0 – 1
        public float VibratoCents;
        public float VibratoRate;
        public float VibratoDelay;
        public float AttackSec;
        public float Noise;             // bow noise / scratch amount
        public float Brightness;        // 0 – 1 tone colour
        public float TimingErrorSec;
        public bool Wrong;
        public bool DownBow;
    }

    public class RenderedPart
    {
        public MusicianData Musician;
        public InstrumentDefinition Instrument;
        public Part Part;
        public List<RenderedNote> Notes = new List<RenderedNote>();
        public double LengthSec;
        public float SectionGain = 1f;

        public float AvgAbsTimingMs;
        public float AvgAbsCents;
        public int WrongNotes;
        public int PlayedNotes;
    }

    /// <summary>
    /// The heart of "skills affect the sound". Converts a written part into played notes using
    /// the musician's abilities: rhythm → timing, intonation → pitch, accuracy → wrong notes,
    /// tone → noise/brightness, expression → vibrato, musicality → phrasing, ensemble → drift,
    /// confidence → nerves in auditions and concerts, sight-reading → unrehearsed pieces.
    /// </summary>
    public static class PerformanceRenderer
    {
        static float Lerp(float a, float b, float t) { t = t < 0 ? 0 : (t > 1 ? 1 : t); return a + (b - a) * t; }
        static float Clamp01(float v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }

        /// <summary>Fastest notes per second this musician plays comfortably.</summary>
        public static float ComfortableNotesPerSecond(MusicianData m)
        {
            return 1.5f + 9f * (0.6f * m.Get01(SkillType.Speed) + 0.4f * m.Get01(SkillType.Technique));
        }

        public static float FastestNotesPerSecond(Part part, float bpm)
        {
            float minBeats = float.MaxValue;
            foreach (var n in part.Notes) if (!n.IsRest && n.beats < minBeats) minBeats = n.beats;
            if (minBeats == float.MaxValue) return 0f;
            return 1f / (minBeats * 60f / bpm);
        }

        /// <summary>
        /// Tempo the ensemble can manage (0.7 – 1 of the marked tempo). Beginners slow down,
        /// exactly like a real amateur group would.
        /// </summary>
        public static float EnsembleTempoScale(List<MusicianData> players, Arrangement arr)
        {
            float scale = 1f;
            foreach (var m in players)
            {
                Part p;
                if (!arr.Parts.TryGetValue(m.instrument, out p)) continue;
                float need = FastestNotesPerSecond(p, arr.Piece.Bpm);
                // Short dotted rhythms are grace-like; weight them less than steady fast notes.
                need = Math.Min(need, 1.6f * arr.Piece.Bpm / 60f);
                float can = ComfortableNotesPerSecond(m);
                if (need > can) scale = Math.Min(scale, can / need);
            }
            return Math.Max(0.7f, scale);
        }

        public static RenderedPart Render(MusicianData m, Part part, PieceDefinition piece, float preparation,
            PerformanceContext ctx, float bpm, int seed)
        {
            var rng = new Random(seed);
            var inst = m.Instrument;
            var rp = new RenderedPart { Musician = m, Instrument = inst, Part = part };

            float rhythm = m.Get01(SkillType.Rhythm);
            float ensemble = m.Get01(SkillType.Ensemble);
            float intonation = m.Get01(SkillType.Intonation);
            float technique = m.Get01(SkillType.Technique);
            float tone = m.Get01(SkillType.ToneQuality);
            float expression = m.Get01(SkillType.Expression);
            float musicality = m.Get01(SkillType.Musicality);
            float confidence = m.Get01(SkillType.Confidence);
            float accuracy = m.Get01(SkillType.Accuracy);
            float sight = m.Get01(SkillType.SightReading);

            float nervesWeight = ctx == PerformanceContext.Concert ? 0.6f : ctx == PerformanceContext.Audition ? 0.45f : 0.15f;
            float nerves = (1f - confidence) * nervesWeight;
            float readiness = Lerp(sight, 1f, preparation);

            float nps = FastestNotesPerSecond(part, bpm);
            float comfort = ComfortableNotesPerSecond(m);
            float stress = Clamp01((nps - comfort) / comfort);
            float difficultyFactor = 1f + 0.6f * ((int)piece.Difficulty - 1) * (1f - technique);

            float timingSd = Lerp(0.035f, 0.003f, rhythm) * (1f + nerves + stress * 1.5f) * Lerp(1.3f, 0.85f, preparation);
            float driftStep = Lerp(0.022f, 0.002f, ensemble) * (1f + nerves);
            float driftPull = Lerp(0.15f, 0.65f, ensemble);
            float centsSd = Lerp(22f, 2f, intonation) * (1f + nerves * 0.7f + stress * 0.5f);
            float accEff = 0.55f * accuracy + 0.25f * technique + 0.20f * readiness;
            float pWrong = Lerp(0.05f, 0.001f, accEff) * (1f + stress * 3f) * (1f + nerves) * difficultyFactor;
            pWrong = Math.Min(0.4f, pWrong);

            double secPerBeat = 60.0 / bpm;
            double beat = 0;
            double drift = 0;
            bool downBow = piece.PickupBeats <= 0f;     // pickups start up-bow, as real string players do
            float phraseBeats = piece.PhraseBars * piece.BeatsPerBar;
            int lastIndex = part.Notes.Count - 1;
            double sumT = 0, sumC = 0;

            for (int i = 0; i < part.Notes.Count; i++)
            {
                var n = part.Notes[i];
                if (n.IsRest) { beat += n.beats; continue; }

                drift = drift * (1 - driftPull) + MusicianFactory.Gaussian(rng) * driftStep;
                double tErr = drift + MusicianFactory.Gaussian(rng) * timingSd;
                double start = beat * secPerBeat + tErr;
                double dur = n.beats * secPerBeat * Lerp(0.88f, 0.97f, technique) + MusicianFactory.Gaussian(rng) * timingSd * 0.5;
                dur = Math.Max(0.06, dur);

                bool wrong = rng.NextDouble() < pWrong;
                int midi = n.midi;
                if (wrong) midi += (rng.NextDouble() < 0.5 ? -1 : 1) * (rng.NextDouble() < 0.7 ? 1 : 2);
                float cents = (float)(MusicianFactory.Gaussian(rng) * centsSd);

                // Phrase shaping: musical players swell toward the middle of each phrase.
                double phrasePos = ((beat - piece.PickupBeats) % phraseBeats + phraseBeats) % phraseBeats / phraseBeats;
                float shape = (float)Math.Sin(Math.PI * phrasePos);
                float vel = 0.68f + musicality * 0.22f * shape + (n.beats >= 2f ? 0.05f * musicality : 0f);
                vel += (float)(MusicianFactory.Gaussian(rng) * 0.08 * (1f - tone));          // uneven bow pressure
                if (i == lastIndex) vel *= Lerp(1f, 0.85f, musicality);                     // gentle final note
                vel = Math.Max(0.25f, Math.Min(1f, vel));

                var rn = new RenderedNote
                {
                    StartSec = start,
                    DurationSec = dur,
                    WrittenMidi = n.midi,
                    PlayedMidi = midi + cents / 100f,
                    Velocity = vel,
                    VibratoCents = dur > 0.3 ? Lerp(2f, 18f, expression) : Lerp(0f, 6f, expression),
                    VibratoRate = 4.6f + 1.6f * expression + (float)rng.NextDouble() * 0.4f,
                    VibratoDelay = Lerp(0.35f, 0.12f, technique),
                    AttackSec = Lerp(0.11f, 0.035f, technique),
                    Noise = Lerp(0.10f, 0.012f, tone) * (1f + nerves * 0.5f),
                    Brightness = 0.3f + 0.6f * tone,
                    TimingErrorSec = (float)tErr,
                    Wrong = wrong,
                    DownBow = downBow
                };
                rp.Notes.Add(rn);
                downBow = !downBow;

                sumT += Math.Abs(tErr);
                sumC += Math.Abs(cents);
                if (wrong) rp.WrongNotes++;
                rp.PlayedNotes++;
                beat += n.beats;
            }

            rp.LengthSec = beat * secPerBeat;
            if (rp.PlayedNotes > 0)
            {
                rp.AvgAbsTimingMs = (float)(sumT / rp.PlayedNotes * 1000.0);
                rp.AvgAbsCents = (float)(sumC / rp.PlayedNotes);
            }
            return rp;
        }
    }
}
