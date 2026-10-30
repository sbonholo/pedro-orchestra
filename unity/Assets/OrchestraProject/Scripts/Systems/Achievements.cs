using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    public class Achievement
    {
        public string Id, Title, Description;
        public int Reward;
        public Func<OrchestraState, bool> Done;
    }

    /// <summary>
    /// Career goals with coin rewards. Same list, order and rewards as ACHIEVEMENTS in the web preview.
    /// Checked after concerts, hiring, shopping and new days; each one pays out once.
    /// </summary>
    public static class Achievements
    {
        public static readonly List<Achievement> All = new List<Achievement>
        {
            A("first_concert", "Opening Night", "Perform your first concert.", 20, s => s.concertsPerformed >= 1),
            A("approved", "Judges' Approval", "Win the approval of the judges (3 of 5).", 30, s => s.judgeApprovals >= 1),
            A("three_stars", "A Fine Evening", "Earn 3 stars in a concert.", 40, s => s.bestStars >= 3),
            A("hire", "New Talent", "Hire or invite a new musician.", 30, s => s.musiciansHired >= 1),
            A("instrument", "A Finer Instrument", "Buy an instrument in the Shop.", 30, s => s.ownedInstruments.Count >= 1),
            A("soloist", "Soloist", "Win the judges' approval with a solo.", 50, s => s.soloApproved),
            A("maestro", "Maestro", "Conduct a concert yourself with 90 % of beats on time.", 80, s => s.bestConducting >= 0.9f),
            A("repertoire", "Repertoire", "Perform 5 different pieces in concert.", 100, s => s.concertPieces.Count >= 5),
            A("advanced", "Advanced Ensemble", "Reach orchestra level 50 (Advanced).", 100, s => AverageLevel(s) >= 50f),
            A("renowned", "Renowned", "Reach reputation 50.", 150, s => s.reputation >= 50f),
            A("professional", "Professional Orchestra", "Reach orchestra level 65 (Professional).", 250, s => AverageLevel(s) >= 65f),
            A("five_stars", "World-Class Evening", "Earn 5 stars in a concert.", 300, s => s.bestStars >= 5),
            A("strad", "A Stradivarius", "Own a Stradivarius.", 500, s => s.ownedInstruments.Exists(id => id.StartsWith("strad_"))),
            A("national", "National Stage", "Be promoted to the National Orchestra.", 400, s => s.level >= 4),
            A("top", "Top of the World", "Become the World Symphony Orchestra.", 1000, s => s.level >= Economy.MaxLevel),
        };

        static Achievement A(string id, string title, string desc, int reward, Func<OrchestraState, bool> done)
        {
            return new Achievement { Id = id, Title = title, Description = desc, Reward = reward, Done = done };
        }

        public static float AverageLevel(OrchestraState s)
        {
            if (s.roster.Count == 0) return 0f;
            float a = 0f; foreach (var m in s.roster) a += m.Overall;
            return a / s.roster.Count;
        }

        /// <summary>Unlocks every newly completed goal, pays its reward and returns them (for the banner).</summary>
        public static List<Achievement> Check(OrchestraState s)
        {
            var list = new List<Achievement>();
            foreach (var a in All)
            {
                if (s.achievements.Contains(a.Id) || !a.Done(s)) continue;
                s.achievements.Add(a.Id);
                s.coins += a.Reward;
                list.Add(a);
            }
            return list;
        }
    }
}
