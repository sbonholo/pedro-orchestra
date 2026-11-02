using System;
using System.Collections.Generic;
using System.Globalization;

namespace OrchestraProject
{
    /// <summary>One recorded note: mono audio plus its pitch and a sustain loop.</summary>
    public class InstrumentSample
    {
        public string Name;
        public int RootMidi;
        public double RootHz;     // measured pitch of the recording
        public int LoopStart, LoopEnd;
        public float[] Data;
        public int SampleRate;
    }

    /// <summary>A set of recorded notes for one instrument (e.g. the four open cello strings).</summary>
    public class SampleSet
    {
        public InstrumentType Instrument;
        public bool RecordedVibrato;   // the player's own vibrato is in the recording
        public List<InstrumentSample> Samples = new List<InstrumentSample>();

        /// <summary>
        /// Parses "samples.txt" (one line per sample: file rootMidi rootHz loopStart loopEnd) and loads
        /// each file as 16-bit little-endian mono PCM through <paramref name="loadBytes"/>.
        /// Pure C#: Unity passes a Resources loader, tests pass File.ReadAllBytes.
        /// </summary>
        public static SampleSet Parse(InstrumentType instrument, string manifest, Func<string, byte[]> loadBytes, int sampleRate = 48000)
        {
            var set = new SampleSet { Instrument = instrument };
            foreach (var raw in manifest.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("@recordedVibrato")) { set.RecordedVibrato = true; continue; }
                var f = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 5) throw new FormatException("Bad sample line: " + line);
                byte[] bytes = loadBytes(f[0]);
                if (bytes == null) throw new Exception("Sample file not found: " + f[0]);
                var data = new float[bytes.Length / 2];
                for (int i = 0; i < data.Length; i++) data[i] = (short)(bytes[2 * i] | (bytes[2 * i + 1] << 8)) / 32768f;
                var s = new InstrumentSample
                {
                    Name = f[0],
                    RootMidi = int.Parse(f[1], CultureInfo.InvariantCulture),
                    RootHz = double.Parse(f[2], CultureInfo.InvariantCulture),
                    LoopStart = int.Parse(f[3], CultureInfo.InvariantCulture),
                    LoopEnd = int.Parse(f[4], CultureInfo.InvariantCulture),
                    Data = data,
                    SampleRate = sampleRate
                };
                s.LoopEnd = Math.Min(s.LoopEnd, data.Length - 2);
                if (s.LoopStart < 0 || s.LoopStart >= s.LoopEnd) throw new FormatException("Bad loop points in " + f[0]);
                set.Samples.Add(s);
            }
            if (set.Samples.Count == 0) throw new FormatException("Sample manifest is empty");
            return set;
        }

        /// <summary>Recording closest in pitch, preferring to shift up (shifting down sounds duller).</summary>
        public InstrumentSample Nearest(double midi)
        {
            InstrumentSample best = null; double bestCost = double.MaxValue;
            foreach (var s in Samples)
            {
                double d = midi - s.RootMidi;
                double cost = d >= 0 ? d : -d * 1.6;
                if (cost < bestCost) { bestCost = cost; best = s; }
            }
            return best;
        }
    }

    /// <summary>
    /// Plays a part using REAL RECORDINGS. Each note picks the nearest recorded note and re-pitches it
    /// (high-quality interpolation), sustains with a crossfaded loop, and applies the same skill effects
    /// as the synthesizer: timing, intonation, wrong notes, vibrato, dynamics, bow noise and tone colour.
    /// </summary>
    public static class SampleVoice
    {
        const int CrossfadeSamples = 2400; // 50 ms

        /// <summary>
        /// Per-instrument clean-up for the lower strings (same numbers as CLEAN in the web preview):
        /// high-pass below the instrument's range, a cut in the muddy low-mids, a little presence,
        /// less added bow noise and vibrato (the recordings already have their own), and a longer, smoother release.
        /// </summary>
        struct Clean { public double HighPass, MudHz, MudDb, NoiseScale, VibScale, ReleaseSec; }
        static Clean CleanFor(InstrumentType t)
        {
            switch (t)
            {
                case InstrumentType.Viola: return new Clean { HighPass = 100, MudHz = 300, MudDb = -3, NoiseScale = 0.5, VibScale = 0.15, ReleaseSec = 0.22 };
                case InstrumentType.Cello: return new Clean { HighPass = 50, MudHz = 250, MudDb = -3.5, NoiseScale = 0.4, VibScale = 0.15, ReleaseSec = 0.28 };
                case InstrumentType.DoubleBass: return new Clean { HighPass = 32, MudHz = 180, MudDb = -3, NoiseScale = 0.35, VibScale = 0.1, ReleaseSec = 0.32 };
                default: return new Clean { HighPass = 160, MudHz = 0, MudDb = 0, NoiseScale = 1, VibScale = 0.25, ReleaseSec = 0.16 };
            }
        }

        /// <summary>Main body (wood) resonance of each instrument.</summary>
        static double BodyHz(InstrumentType t)
        {
            switch (t) { case InstrumentType.Viola: return 350; case InstrumentType.Cello: return 200; case InstrumentType.DoubleBass: return 110; default: return 450; }
        }

        public static float[] RenderPart(RenderedPart part, SampleSet set, int sampleRate, double totalSec, int seed)
        {
            int length = (int)((StringSynth.LeadInSec + totalSec + 1.5) * sampleRate);
            var buf = new float[length];
            uint noiseState = (uint)seed | 1u;
            float brightness = 0f;
            foreach (var n in part.Notes) brightness += n.Brightness;
            brightness = part.Notes.Count > 0 ? brightness / part.Notes.Count : 0.5f;

            var clean = CleanFor(part.Instrument.Type);
            // The instrument itself (Economy.Quality, squared so a Stradivarius stands far above a master-made one):
            // a fine instrument scratches less, resonates in its body, sings in the 2–3 kHz "bridge hill", has more air and projects.
            double w = part.Musician != null ? Math.Pow(Economy.Quality(part.Musician), 2) : 0;
            clean.NoiseScale *= 1 - 0.8 * w;
            foreach (var n in part.Notes) RenderNote(buf, n, set, sampleRate, ref noiseState, clean);
            Biquad.HighPass(sampleRate, clean.HighPass, 0.707).Process(buf);
            if (clean.MudHz > 0)
            {
                Biquad.Peaking(sampleRate, clean.MudHz, 1.0, clean.MudDb).Process(buf);
                Biquad.Peaking(sampleRate, 2500, 0.9, 1.5).Process(buf);   // a little presence so the line stays clear
            }

            if (w > 0)
            {
                Biquad.Peaking(sampleRate, BodyHz(part.Instrument.Type), 1.4, 3.0 * w).Process(buf);   // body resonance: warmth
                Biquad.Peaking(sampleRate, 2800, 0.8, 3.5 * w).Process(buf);                          // bridge hill: the singing, carrying sound
                Biquad.Peaking(sampleRate, 9000, 0.7, 2.5 * w).Process(buf);                          // air and sparkle
            }

            // Weaker tone = darker, less focused sound. A world-class tone keeps the full recording.
            double cutoff = 2200 + 9000 * Math.Pow(brightness, 1.5) + 3000 * w;
            if (cutoff < sampleRate * 0.45) Biquad.LowPass(sampleRate, cutoff, 0.707).Process(buf);

            float gain = 0.55f * part.Instrument.Loudness * part.SectionGain * (1f + 0.25f * (float)w);   // fine instruments project
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i] * gain;
                buf[i] = v / (1f + Math.Abs(v));
            }
            return buf;
        }

        static void RenderNote(float[] buf, RenderedNote n, SampleSet set, int sr, ref uint noiseState, Clean clean)
        {
            var s = set.Nearest(n.PlayedMidi);
            double targetHz = 440.0 * Math.Pow(2.0, (n.PlayedMidi - 69.0) / 12.0);
            double baseRate = targetHz / s.RootHz * s.SampleRate / sr;

            int start = (int)((StringSynth.LeadInSec + n.StartSec) * sr);
            if (start < 0) start = 0;
            int sustainEnd = start + (int)(n.DurationSec * sr);
            int end = Math.Min(buf.Length, sustainEnd + (int)(clean.ReleaseSec * sr));

            double pos = 0;
            double vibPhase = 0;
            float scoop = n.Noise * 120f;
            double release = 1.0, releaseMul = Math.Exp(-1.0 / (sr * clean.ReleaseSec / 4.0));
            double rate = baseRate;
            float noiseLp = 0f;
            int loopLen = s.LoopEnd - s.LoopStart;
            int xf = Math.Min(CrossfadeSamples, loopLen / 3);
            double attack = Math.Max(0.005, n.AttackSec * 0.25);   // the recording already has its own attack
            double vibratoCents = set.RecordedVibrato ? n.VibratoCents * clean.VibScale : n.VibratoCents; // don't double the recorded vibrato

            for (int i = start; i < end; i++)
            {
                double t = (i - start) / (double)sr;
                if (((i - start) & 31) == 0)
                {
                    double vibRamp = Math.Max(0.0, Math.Min(1.0, (t - n.VibratoDelay) / 0.25));
                    double cents = vibratoCents * vibRamp * Math.Sin(vibPhase) - scoop * Math.Exp(-t / 0.05);
                    rate = baseRate * Math.Pow(2.0, cents / 1200.0);
                }
                vibPhase += 2 * Math.PI * n.VibratoRate / sr;

                // Wrap into the sustain loop with a crossfade so long notes never run out.
                while (pos >= s.LoopEnd) pos -= loopLen;
                double v = Read(s.Data, pos);
                double intoFade = pos - (s.LoopEnd - xf);
                if (intoFade > 0)
                {
                    double w = intoFade / xf;
                    v = v * (1 - w) + Read(s.Data, pos - loopLen) * w;
                }

                double env = n.Velocity;
                if (t < attack) env *= t / attack;
                if (i >= sustainEnd) { release *= releaseMul; env *= release; }

                noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
                float white = (noiseState / (float)uint.MaxValue) * 2f - 1f;
                noiseLp += (white - noiseLp) * 0.3f;
                double scratch = (white - noiseLp) * n.Noise * (t < 0.12 ? 1.6 : 0.5) * clean.NoiseScale;

                buf[i] += (float)((v + scratch * 0.6) * env);
                pos += rate;
            }
        }

        /// <summary>4-point Hermite interpolation: clean re-pitching without the dullness of linear.</summary>
        static double Read(float[] d, double pos)
        {
            int i = (int)pos;
            if (i < 1 || i + 2 >= d.Length) return i >= 0 && i < d.Length ? d[i] : 0.0;
            double f = pos - i;
            double y0 = d[i - 1], y1 = d[i], y2 = d[i + 1], y3 = d[i + 2];
            double c1 = 0.5 * (y2 - y0);
            double c2 = y0 - 2.5 * y1 + 2 * y2 - 0.5 * y3;
            double c3 = 0.5 * (y3 - y0) + 1.5 * (y1 - y2);
            return ((c3 * f + c2) * f + c1) * f + y1;
        }
    }
}
