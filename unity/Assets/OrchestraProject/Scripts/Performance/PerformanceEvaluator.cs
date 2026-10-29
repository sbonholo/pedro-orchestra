using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    public class PerformanceResult
    {
        public PieceDefinition Piece;
        public ArrangementMode Mode;
        public PerformanceContext Context;
        public float TempoScale;
        public float Rhythm, Intonation, Accuracy, Ensemble, Artistry;
        public float Overall;
        public int Stars;
        public string Review;
        public List<string> MusicianNotes = new List<string>();
        public float ConductingScore = -1f;   // 0–1 when the player conducted, otherwise -1
        public float ConductingBonus;
        public List<JudgeVerdict> Judges;     // concerts only
        public int CoinsEarned;
        public int TicketsSold, TicketCoins;
    }

    /// <summary>
    /// Scores what was actually played (the rendered notes the audience heard), not the raw stats.
    /// </summary>
    public static class PerformanceEvaluator
    {
        public static PerformanceResult Evaluate(List<RenderedPart> parts, PieceDefinition piece, ArrangementMode mode,
            PerformanceContext ctx, float tempoScale)
        {
            var r = new PerformanceResult { Piece = piece, Mode = mode, Context = ctx, TempoScale = tempoScale };
            if (parts.Count == 0) return r;

            float rhythm = 0, inton = 0, acc = 0, art = 0;
            foreach (var p in parts)
            {
                float pr = 100f * (float)Math.Exp(-p.AvgAbsTimingMs / 45.0);
                float pi = 100f * (float)Math.Exp(-p.AvgAbsCents / 18.0);
                float wrongRate = p.PlayedNotes > 0 ? p.WrongNotes / (float)p.PlayedNotes : 0f;
                float pa = Math.Max(0f, 100f * (1f - wrongRate * 4f));
                var m = p.Musician;
                float pArt = (m.Get(SkillType.Musicality) + Economy.Played(m, SkillType.Expression) + Economy.Played(m, SkillType.ToneQuality)) / 3f;
                rhythm += pr; inton += pi; acc += pa; art += pArt;

                r.MusicianNotes.Add(m.FullName + " (" + p.Instrument.DisplayName + "): timing ±" + p.AvgAbsTimingMs.ToString("0") +
                    " ms, pitch ±" + p.AvgAbsCents.ToString("0") + " cents, " + p.WrongNotes + " wrong note" + (p.WrongNotes == 1 ? "" : "s"));
            }
            int n = parts.Count;
            r.Rhythm = rhythm / n; r.Intonation = inton / n; r.Accuracy = acc / n; r.Artistry = art / n;
            r.Ensemble = parts.Count > 1 ? EnsembleScore(parts) : r.Rhythm;

            float overall = 0.22f * r.Rhythm + 0.2f * r.Intonation + 0.2f * r.Accuracy + 0.2f * r.Ensemble + 0.18f * r.Artistry;
            if (mode != ArrangementMode.SameMelody) overall += 2f;          // richer texture, audiences notice
            overall *= 0.8f + 0.2f * Math.Max(0f, Math.Min(1f, (tempoScale - 0.7f) / 0.3f)); // too slow = less exciting
            r.Overall = Math.Max(0f, Math.Min(100f, overall));

            r.Stars = r.Overall >= 88 ? 5 : r.Overall >= 75 ? 4 : r.Overall >= 60 ? 3 : r.Overall >= 45 ? 2 : 1;
            r.Review = ReviewText(r);
            return r;
        }

        /// <summary>
        /// "You conduct" mode: steady beating keeps the orchestra together (+6 at 100 %), a lost or absent beat
        /// costs up to −6. Same rule as applyConducting() in the web preview.
        /// </summary>
        public static void ApplyConducting(PerformanceResult r, float score01)
        {
            score01 = Math.Max(0f, Math.Min(1f, score01));
            r.ConductingScore = score01;
            r.ConductingBonus = 12f * score01 - 6f;
            r.Overall = Math.Max(0f, Math.Min(100f, r.Overall + r.ConductingBonus));
            r.Stars = r.Overall >= 88 ? 5 : r.Overall >= 75 ? 4 : r.Overall >= 60 ? 3 : r.Overall >= 45 ? 2 : 1;
            r.Review = ReviewText(r);
            r.MusicianNotes.Insert(0, "Your conducting: " + (score01 * 100f).ToString("0") + "% on the beat (" + (r.ConductingBonus >= 0 ? "+" : "") + r.ConductingBonus.ToString("0.0") + " points)");
        }

        /// <summary>How tightly musicians start shared beats together (std-dev of onsets in ms).</summary>
        static float EnsembleScore(List<RenderedPart> parts)
        {
            // Bucket every note start by its written time (rounded to 1 ms of ideal time).
            var buckets = new Dictionary<long, List<double>>();
            foreach (var p in parts)
                foreach (var note in p.Notes)
                {
                    long key = (long)Math.Round((note.StartSec - note.TimingErrorSec) * 1000.0);
                    List<double> l;
                    if (!buckets.TryGetValue(key, out l)) buckets[key] = l = new List<double>();
                    l.Add(note.TimingErrorSec);
                }
            double sum = 0; int count = 0;
            foreach (var l in buckets.Values)
            {
                if (l.Count < 2) continue;
                double mean = 0; foreach (var v in l) mean += v; mean /= l.Count;
                double variance = 0; foreach (var v in l) variance += (v - mean) * (v - mean); variance /= l.Count;
                sum += Math.Sqrt(variance) * 1000.0; count++;
            }
            if (count == 0) return 70f;
            return 100f * (float)Math.Exp(-(sum / count) / 30.0);
        }

        static string ReviewText(PerformanceResult r)
        {
            string weakest = "rhythm";
            float min = r.Rhythm;
            if (r.Intonation < min) { min = r.Intonation; weakest = "intonation"; }
            if (r.Accuracy < min) { min = r.Accuracy; weakest = "note accuracy"; }
            if (r.Ensemble < min) { min = r.Ensemble; weakest = "ensemble coordination"; }
            if (r.Artistry < min) { min = r.Artistry; weakest = "musical expression"; }

            string head;
            switch (r.Stars)
            {
                case 5: head = "A radiant, world-class performance. The hall rose to its feet."; break;
                case 4: head = "A polished and moving performance with real character."; break;
                case 3: head = "A solid performance with moments of genuine music-making."; break;
                case 2: head = "An earnest effort, but the seams were audible."; break;
                default: head = "A difficult evening. The orchestra needs much more preparation."; break;
            }
            string tempo = r.TempoScale < 0.98f ? " The tempo was held back to " + (r.TempoScale * 100f).ToString("0") + "% of the marking." : "";
            return head + " Weakest area: " + weakest + "." + tempo;
        }
    }
}
