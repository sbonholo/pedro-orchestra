using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>
    /// PLACEHOLDER SOUND. A small bowed-string synthesizer (band-limited sawtooth = idealised bowed
    /// string motion, plus bow noise, vibrato and body resonance filters). It is recognisably
    /// violin/viola/cello/bass-like and reacts to every skill, but it is NOT a recorded instrument.
    /// Stage 3 replaces it with licensed multi-sampled recordings (see README).
    /// Pure C#, no Unity types, so it is unit-testable.
    /// </summary>
    public static class StringSynth
    {
        public const double LeadInSec = 0.5;     // silence before beat 1 so early notes are possible
        const double ReleaseSec = 0.14;

        public static float[] RenderPart(RenderedPart part, int sampleRate, double totalSec, int seed)
        {
            int length = (int)((LeadInSec + totalSec + 1.5) * sampleRate);
            var buf = new float[length];
            var rng = new Random(seed);
            uint noiseState = (uint)seed | 1u;
            var inst = part.Instrument;
            float brightness = 0f;
            foreach (var n in part.Notes) brightness += n.Brightness;
            brightness = part.Notes.Count > 0 ? brightness / part.Notes.Count : 0.5f;

            foreach (var n in part.Notes) RenderNote(buf, n, sampleRate, rng, ref noiseState);

            // Body: remove rumble, add wood resonances, then roll off harshness according to tone quality.
            var hp = Biquad.HighPass(sampleRate, inst.LowCutHz, 0.707f);
            hp.Process(buf);
            for (int i = 0; i < inst.FormantHz.Length; i++)
                Biquad.Peaking(sampleRate, inst.FormantHz[i], 1.4f, inst.FormantGainDb[i]).Process(buf);
            float cutoff = inst.BrightnessLowHz + (inst.BrightnessHighHz - inst.BrightnessLowHz) * brightness;
            Biquad.LowPass(sampleRate, Math.Min(cutoff, sampleRate * 0.45f), 0.707f).Process(buf);
            Biquad.LowPass(sampleRate, Math.Min(cutoff * 1.4f, sampleRate * 0.45f), 0.707f).Process(buf);

            float gain = 0.16f * inst.Loudness * part.SectionGain;
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i] * gain;
                buf[i] = v / (1f + Math.Abs(v));   // soft limiter, never clips
            }
            return buf;
        }

        static void RenderNote(float[] buf, RenderedNote n, int sr, Random rng, ref uint noiseState)
        {
            int start = (int)((LeadInSec + n.StartSec) * sr);
            if (start < 0) start = 0;
            int sustainEnd = start + (int)(n.DurationSec * sr);
            int end = Math.Min(buf.Length, sustainEnd + (int)(ReleaseSec * sr));

            double baseFreq = 440.0 * Math.Pow(2.0, (n.PlayedMidi - 69.0) / 12.0);
            double phase = rng.NextDouble();
            double vibPhase = rng.NextDouble() * Math.PI * 2;
            double freq = baseFreq, dt = freq / sr;
            float scoop = n.Noise * 120f;          // weaker players slide into the note (cents)
            float pressureDrift = 0f;
            float noiseLp = 0f;
            double attack = Math.Max(0.01, n.AttackSec);
            double releaseEnv = 1.0, releaseMul = Math.Exp(-1.0 / (sr * ReleaseSec / 4.0));

            for (int i = start; i < end; i++)
            {
                double t = (i - start) / (double)sr;

                if ((i - start) % 32 == 0)
                {
                    double vibRamp = Math.Max(0.0, Math.Min(1.0, (t - n.VibratoDelay) / 0.25));
                    double cents = n.VibratoCents * vibRamp * Math.Sin(vibPhase)
                                   - scoop * Math.Exp(-t / 0.05);
                    freq = baseFreq * Math.Pow(2.0, cents / 1200.0);
                    dt = freq / sr;
                    pressureDrift += (float)((rng.NextDouble() - 0.5) * 0.02) - pressureDrift * 0.02f;
                }
                vibPhase += 2 * Math.PI * n.VibratoRate / sr;

                // Envelope: bow attack, sustain with tiny pressure changes, release.
                double env = 1.0;
                if (t < attack * 1.5) env = 1.0 - Math.Exp(-5.0 * t / attack);
                if (i >= sustainEnd) { releaseEnv *= releaseMul; env *= releaseEnv; }
                env *= n.Velocity * (1.0 + pressureDrift);

                // PolyBLEP sawtooth (alias-free).
                phase += dt;
                if (phase >= 1.0) phase -= 1.0;
                double saw = 2.0 * phase - 1.0 - PolyBlep(phase, dt);

                // Bow noise: louder during the attack and for poor tone quality.
                noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
                float white = (noiseState / (float)uint.MaxValue) * 2f - 1f;
                noiseLp += (white - noiseLp) * 0.35f;
                double attackBurst = t < attack * 2 ? 2.5 : 1.0;
                double noise = (white - noiseLp) * n.Noise * attackBurst;

                buf[i] += (float)((saw * 0.9 + noise) * env);
            }
        }

        static double PolyBlep(double t, double dt)
        {
            if (t < dt) { t /= dt; return t + t - t * t - 1.0; }
            if (t > 1.0 - dt) { t = (t - 1.0) / dt; return t * t + t + t + 1.0; }
            return 0.0;
        }

        /// <summary>Metronome click track: accented first beat. Used by the rhythm mini-game.</summary>
        public static float[] ClickTrack(IList<double> clickTimes, IList<bool> accents, int sampleRate, double totalSec)
        {
            var buf = new float[(int)((totalSec + 0.5) * sampleRate)];
            for (int c = 0; c < clickTimes.Count; c++)
            {
                double f = accents[c] ? 1760.0 : 1320.0;
                int s0 = (int)(clickTimes[c] * sampleRate);
                int len = (int)(0.045 * sampleRate);
                for (int i = 0; i < len && s0 + i < buf.Length; i++)
                {
                    if (s0 + i < 0) continue;
                    double t = i / (double)sampleRate;
                    buf[s0 + i] += (float)(Math.Sin(2 * Math.PI * f * t) * Math.Exp(-t / 0.012) * (accents[c] ? 0.7 : 0.5));
                }
            }
            return buf;
        }
    }

    /// <summary>Standard RBJ biquad filter.</summary>
    public class Biquad
    {
        double b0, b1, b2, a1, a2, z1, z2;

        public void Process(float[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                double x = buf[i];
                double y = b0 * x + z1;
                z1 = b1 * x - a1 * y + z2;
                z2 = b2 * x - a2 * y;
                buf[i] = (float)y;
            }
        }

        static Biquad Make(double b0, double b1, double b2, double a0, double a1, double a2)
        {
            return new Biquad { b0 = b0 / a0, b1 = b1 / a0, b2 = b2 / a0, a1 = a1 / a0, a2 = a2 / a0 };
        }

        public static Biquad LowPass(int sr, double f, double q)
        {
            double w = 2 * Math.PI * f / sr, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * q);
            return Make((1 - cos) / 2, 1 - cos, (1 - cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
        }

        public static Biquad HighPass(int sr, double f, double q)
        {
            double w = 2 * Math.PI * f / sr, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * q);
            return Make((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
        }

        public static Biquad Peaking(int sr, double f, double q, double gainDb)
        {
            double A = Math.Pow(10, gainDb / 40), w = 2 * Math.PI * f / sr, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * q);
            return Make(1 + alpha * A, -2 * cos, 1 - alpha * A, 1 + alpha / A, -2 * cos, 1 - alpha / A);
        }
    }
}
