using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>Auditions (musicians who apply) and invitations (stronger musicians you approach).</summary>
    public static class RecruitmentSystem
    {
        public const int AuditionPoolSize = 3;
        public const int InvitePoolSize = 2;
        public const int RefreshEveryDays = 7;
        /// <summary>Each applicant and each invitation has a 1 % chance to be a rare, world-class talent.</summary>
        public const double RareTalentChance = 0.01;

        public static void RefreshIfDue(OrchestraState s, Random rng)
        {
            if (s.day - s.lastPoolRefreshDay >= RefreshEveryDays || (s.auditionPool.Count == 0 && s.invitePool.Count == 0))
                Refresh(s, rng);
        }

        public static void Refresh(OrchestraState s, Random rng)
        {
            var instruments = InstrumentCatalog.Implemented();
            s.auditionPool.Clear();
            s.invitePool.Clear();

            float baseLevel = 26f + s.reputation * 0.45f + 4f * s.level;   // better orchestras attract better players
            for (int i = 0; i < AuditionPoolSize; i++)
            {
                float mean = Clamp(baseLevel + (float)(rng.NextDouble() * 16.0 - 6.0), 18f, 85f);
                s.auditionPool.Add(Candidate(instruments[rng.Next(instruments.Count)], mean, rng));
            }
            for (int i = 0; i < InvitePoolSize; i++)
            {
                float mean = Clamp(baseLevel + 12f + (float)(rng.NextDouble() * 12.0), 30f, 95f);
                s.invitePool.Add(Candidate(instruments[rng.Next(instruments.Count)], mean, rng));
            }
            s.lastPoolRefreshDay = s.day;
        }

        static MusicianData Candidate(InstrumentType instrument, float mean, Random rng)
        {
            bool rare = rng.NextDouble() < RareTalentChance;
            var m = MusicianFactory.Create(instrument, rare ? 86f + (float)rng.NextDouble() * 8f : mean, rng);   // rare: world-class, 86–94
            m.rareTalent = rare;
            return m;
        }

        /// <summary>Chance (0–1) that an invited musician says yes. Grows with reputation.</summary>
        public static float InviteAcceptance(OrchestraState s, MusicianData candidate)
        {
            float expected = 30f + s.reputation * 0.6f;
            return Clamp(0.75f - (candidate.Overall - expected) / 40f, 0.10f, 0.95f);
        }

        public static string Hire(OrchestraState s, MusicianData candidate)
        {
            if (s.roster.Count >= OrchestraState.MaxRosterSize) return "The stage is full (" + OrchestraState.MaxRosterSize + " musicians).";
            s.auditionPool.Remove(candidate);
            s.invitePool.Remove(candidate);
            candidate.energy = TrainingSystem.MaxEnergy;
            s.roster.Add(candidate);
            s.Log(candidate.FullName + " (" + candidate.Instrument.DisplayName + ") joined the orchestra.");
            return candidate.FullName + " joins the orchestra as " + candidate.Instrument.PlayerTitle + ".";
        }

        public static string Invite(OrchestraState s, MusicianData candidate, Random rng, out bool accepted)
        {
            float chance = InviteAcceptance(s, candidate);
            accepted = rng.NextDouble() < chance;
            if (accepted) return Hire(s, candidate);
            s.invitePool.Remove(candidate);
            s.Log(candidate.FullName + " declined an invitation.");
            return candidate.FullName + " politely declined. Build reputation with concerts to attract stronger players.";
        }

        /// <summary>Best current player on the same instrument (for side-by-side comparison), or null.</summary>
        public static MusicianData SectionLeader(OrchestraState s, InstrumentType t)
        {
            MusicianData best = null;
            foreach (var m in s.roster) if (m.instrument == t && (best == null || m.Overall > best.Overall)) best = m;
            return best;
        }

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}
