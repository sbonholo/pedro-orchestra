using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestraProject
{
    public class SkillGain
    {
        public string MusicianId;
        public SkillType Skill;
        public float Before;
        public float After;
        public float Delta { get { return After - Before; } }
        public bool TierUp { get { return SkillLevels.TierName(After) != SkillLevels.TierName(Before); } }
    }

    public class TrainingResult
    {
        public bool Success;
        public string Message;
        public List<SkillGain> Gains = new List<SkillGain>();

        public string Summary()
        {
            if (!Success) return Message;
            var sb = new StringBuilder(Message);
            var musicians = new HashSet<string>();
            foreach (var g in Gains) musicians.Add(g.MusicianId);
            if (musicians.Count <= 1)
            {
                foreach (var g in Gains)
                    if (g.Delta > 0.005f)
                        sb.Append("\n").Append(Skills.ShortName(g.Skill)).Append(" +").Append(g.Delta.ToString("0.0"))
                          .Append(g.TierUp ? "  (now " + SkillLevels.TierName(g.After) + ")" : "");
                return sb.ToString();
            }
            // Group activity: average gain per skill across the musicians.
            foreach (var s in Skills.All)
            {
                float sum = 0f; int n = 0;
                foreach (var g in Gains) if (g.Skill == s) { sum += g.Delta; n++; }
                if (n > 0 && sum / n > 0.005f) sb.Append("\n").Append(Skills.ShortName(s)).Append(" +").Append((sum / n).ToString("0.0")).Append(" avg (").Append(n).Append(" musicians)");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Medium-paced progression. Each session gives gains that shrink as a skill approaches 100,
    /// so the climb from Student to Professional takes weeks of in-game time and Master takes months.
    /// </summary>
    public static class TrainingSystem
    {
        public const float BaseGain = 1.4f;
        public const float SecondaryShare = 0.35f;
        public const float AllSkillsShare = 0.15f;   // every practice also improves every other skill a little
        public const float EnergyPerPractice = 20f;
        public const float EnergyPerRehearsal = 15f;
        public const float EnergyPerConcert = 25f;
        public const float MaxEnergy = 2000f;
        public const float DailyRecovery = 1200f;
        public const float MinEnergyToWork = 10f;
        public const int DirectorSessionsPerDay = 2;

        /// <summary>How much room a skill still has to grow (1 at 0, ~0 at 100).</summary>
        public static float Headroom(float value)
        {
            float h = (float)Math.Pow(Math.Max(0.0, 1.0 - value / 100.0), 1.1);
            return Math.Max(0.01f, h);
        }

        static float EnergyFactor(float energy)
        {
            if (energy < 25f) return 0.45f;
            return 0.65f + 0.35f * (energy / MaxEnergy);
        }

        /// <summary>
        /// One focused practice session. <paramref name="quality"/> is 1 for a quick practice;
        /// the rhythm mini-game passes 0.3 – 2.0 depending on how well the player tapped.
        /// </summary>
        public static TrainingResult Practice(MusicianData m, SkillType focus, float quality, Random rng)
        {
            var r = new TrainingResult();
            if (m.energy < MinEnergyToWork)
            {
                r.Message = m.firstName + " is too tired to practise. Advance to the next day to rest.";
                return r;
            }

            float noise = 0.85f + (float)rng.NextDouble() * 0.3f;
            float factor = BaseGain * m.talent * EnergyFactor(m.energy) * quality * noise;

            Apply(m, focus, factor, r);
            var related = new List<SkillType>(Skills.Related(focus));
            foreach (var rel in related) Apply(m, rel, factor * SecondaryShare, r);
            foreach (var other in Skills.All) if (other != focus && !related.Contains(other)) Apply(m, other, factor * AllSkillsShare, r);

            m.energy = Math.Max(0f, m.energy - EnergyPerPractice);
            m.practiceSessions++;
            r.Success = true;
            r.Message = m.firstName + " practised " + Skills.DisplayName(focus) + ".";
            return r;
        }

        /// <summary>A group rehearsal of one piece: builds ensemble skills and piece preparation.</summary>
        public static TrainingResult Rehearse(List<MusicianData> players, PieceProgress piece, Random rng)
        {
            var r = new TrainingResult();
            var active = players.FindAll(p => p.energy >= MinEnergyToWork);
            if (active.Count == 0)
            {
                r.Message = "Everyone is too tired to rehearse. Advance to the next day.";
                return r;
            }

            float talent = 0f;
            foreach (var m in active)
            {
                float f = BaseGain * m.talent * EnergyFactor(m.energy) * (0.85f + (float)rng.NextDouble() * 0.3f);
                Apply(m, SkillType.Ensemble, f * 0.8f, r);
                Apply(m, SkillType.Rhythm, f * 0.3f, r);
                Apply(m, SkillType.SightReading, f * 0.2f, r);
                Apply(m, SkillType.Confidence, f * 0.2f, r);
                m.energy = Math.Max(0f, m.energy - EnergyPerRehearsal);
                talent += m.talent;
            }
            talent /= active.Count;

            float before = piece.preparation;
            float coverage = active.Count / (float)Math.Max(1, players.Count);
            piece.preparation = Math.Min(1f, piece.preparation + (1f - piece.preparation) * 0.2f * talent * coverage);
            piece.timesRehearsed++;

            r.Success = true;
            r.Message = "Rehearsal finished. Piece preparation " + (before * 100f).ToString("0") + "% → " + (piece.preparation * 100f).ToString("0") + "%." +
                        (active.Count < players.Count ? " (" + (players.Count - active.Count) + " musician(s) too tired to join.)" : "");
            return r;
        }

        /// <summary>Concert experience: confidence and artistry grow, more after a good concert.</summary>
        public static void ApplyConcertExperience(List<MusicianData> players, float rating01, TrainingResult r)
        {
            foreach (var m in players)
            {
                float f = BaseGain * m.talent * (0.6f + rating01);
                Apply(m, SkillType.Confidence, f * 1.2f, r);
                Apply(m, SkillType.Musicality, f * 0.3f, r);
                Apply(m, SkillType.Expression, f * 0.3f, r);
                m.energy = Math.Max(0f, m.energy - EnergyPerConcert);
                m.concertsPlayed++;
            }
        }

        static void Apply(MusicianData m, SkillType s, float factor, TrainingResult r)
        {
            float before = m.Get(s);
            m.Set(s, before + factor * Headroom(before));
            var existing = r.Gains.Find(g => g.Skill == s && g.MusicianId == m.id);
            if (existing != null) existing.After = m.Get(s);
            else r.Gains.Add(new SkillGain { MusicianId = m.id, Skill = s, Before = before, After = m.Get(s) });
        }

        public static SkillType ChooseFocus(MusicianData m)
        {
            return m.trainingFocus >= 0 ? (SkillType)m.trainingFocus : m.WeakestSkill();
        }

        /// <summary>Moves the calendar forward: rest, optional daily plans, slow forgetting of unrehearsed pieces.</summary>
        public static string AdvanceDay(OrchestraState state, Random rng)
        {
            state.day++;
            var sb = new StringBuilder("Day " + state.day + " begins.");
            foreach (var m in state.roster) m.energy = Math.Min(MaxEnergy, m.energy + DailyRecovery);

            if (state.playStyle == (int)PlayStyle.Director)
            {
                int totalSessions = 0;
                foreach (var m in state.roster)
                    for (int i = 0; i < DirectorSessionsPerDay; i++)
                    {
                        var res = Practice(m, ChooseFocus(m), 1f, rng);
                        if (res.Success) totalSessions++;
                    }
                sb.Append(" Musicians completed ").Append(totalSessions).Append(" practice sessions from their training plans.");
            }

            foreach (var p in state.pieces) p.preparation = Math.Max(0f, p.preparation * 0.985f);
            return sb.ToString();
        }
    }
}
