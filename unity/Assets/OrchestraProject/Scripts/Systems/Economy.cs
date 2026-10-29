using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>A purchasable instrument. Quality 0 (student instrument) – 1 (the finest Stradivarius).</summary>
    public class InstrumentModel
    {
        public string Id, Name, Maker;
        public InstrumentType Type;
        public int Price;
        public float Quality;
    }

    /// <summary>One judge's verdict on a concert (score 1–10).</summary>
    public class JudgeVerdict
    {
        public string Name, Focus, Comment;
        public float Score;
        public bool Approves { get { return Score >= Economy.JudgePassMark; } }
    }

    /// <summary>
    /// Coins, judges, masterclasses and the instrument shop. Same rules and numbers as the "economy"
    /// section of the web preview.
    ///   Concert: 5 judges score it; if at least 3 approve (score >= 5.5 / 10) you earn 60 coins, otherwise 10.
    ///   Masterclass: +3 to one skill of one musician; costs 10 + half the current skill value.
    ///   Instruments: better instruments add Tone (+20 x quality), Expression (+8 x q) and Intonation (+4 x q).
    /// </summary>
    public static class Economy
    {
        public const int StartingCoins = 500;
        public const int CoinsApproved = 60, CoinsRejected = 10;
        public const float JudgePassMark = 5.5f;
        public const int JudgeCount = 5, JudgesNeeded = 3;   // a majority of the five judges
        public const float MasterclassGain = 3f;

        public static int MasterclassCost(float skill) { return 10 + (int)Math.Round(skill * 0.5f); }

        // ---- Instruments ----
        static List<InstrumentModel> catalog;
        public static List<InstrumentModel> Catalog { get { if (catalog == null) BuildCatalog(); return catalog; } }

        public static InstrumentModel Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var m in Catalog) if (m.Id == id) return m;
            return null;
        }

        static void BuildCatalog()
        {
            catalog = new List<InstrumentModel>();
            foreach (InstrumentType t in new[] { InstrumentType.Violin, InstrumentType.Viola, InstrumentType.Cello, InstrumentType.DoubleBass })
            {
                string n = InstrumentCatalog.Get(t).DisplayName;
                catalog.Add(new InstrumentModel { Id = t + "_workshop", Name = "Workshop " + n.ToLowerInvariant(), Maker = "Modern workshop", Type = t, Price = 300, Quality = 0.25f });
                catalog.Add(new InstrumentModel { Id = t + "_master", Name = "Master-made " + n.ToLowerInvariant(), Maker = "Contemporary luthier", Type = t, Price = 1200, Quality = 0.5f });
            }
            // Ten Stradivarius violins, 3,000 to 5,000 coins. The "Messiah" is the finest.
            string[,] strads =
            {
                { "hellier", "“Hellier”", "1679" }, { "molitor", "“Molitor”", "1697" }, { "hammer", "“Hammer”", "1707" },
                { "viotti", "“Viotti”", "1709" }, { "dolphin", "“Dolphin”", "1714" }, { "soil", "“Soil”", "1714" },
                { "alard", "“Alard”", "1715" }, { "lipinski", "“Lipinski”", "1715" }, { "ladyblunt", "“Lady Blunt”", "1721" },
                { "messiah", "“Messiah”", "1716" },
            };
            for (int i = 0; i < 10; i++)
                catalog.Add(new InstrumentModel
                {
                    Id = "strad_" + strads[i, 0], Name = "Stradivarius " + strads[i, 1], Maker = "Antonio Stradivari, " + strads[i, 2],
                    Type = InstrumentType.Violin, Price = 3000 + (int)Math.Round(i * 2000 / 9.0), Quality = 0.8f + 0.2f * i / 9f
                });
            catalog.Add(new InstrumentModel { Id = "strad_archinto", Name = "Stradivarius “Archinto” viola", Maker = "Antonio Stradivari, 1696", Type = InstrumentType.Viola, Price = 4000, Quality = 0.9f });
            catalog.Add(new InstrumentModel { Id = "strad_macdonald", Name = "Stradivarius “Macdonald” viola", Maker = "Antonio Stradivari, 1701", Type = InstrumentType.Viola, Price = 4800, Quality = 0.98f });
            catalog.Add(new InstrumentModel { Id = "strad_duport", Name = "Stradivarius “Duport” cello", Maker = "Antonio Stradivari, 1711", Type = InstrumentType.Cello, Price = 4500, Quality = 0.92f });
            catalog.Add(new InstrumentModel { Id = "strad_davidov", Name = "Stradivarius “Davidov” cello", Maker = "Antonio Stradivari, 1712", Type = InstrumentType.Cello, Price = 5000, Quality = 1f });
            catalog.Add(new InstrumentModel { Id = "dragonetti", Name = "“Dragonetti” double bass", Maker = "Gasparo da Salò, c. 1600", Type = InstrumentType.DoubleBass, Price = 4000, Quality = 0.95f });
        }

        public static float Quality(MusicianData m) { var im = Get(m.instrumentModel); return im != null ? im.Quality : 0f; }

        /// <summary>Skill as heard in performance: the player's skill plus what their instrument adds.</summary>
        public static float Played(MusicianData m, SkillType s)
        {
            float q = Quality(m), bonus = 0f;
            if (s == SkillType.ToneQuality) bonus = 20f * q;
            else if (s == SkillType.Expression) bonus = 8f * q;
            else if (s == SkillType.Intonation) bonus = 4f * q;
            return Math.Min(100f, m.Get(s) + bonus);
        }

        public static string Buy(OrchestraState s, MusicianData m, InstrumentModel im)
        {
            if (im.Type != m.instrument) return "That instrument is for a different section.";
            if (!s.ownedInstruments.Contains(im.Id))
            {
                if (s.coins < im.Price) return "Not enough coins: " + im.Name + " costs " + im.Price.ToString("N0") + ".";
                s.coins -= im.Price;
                s.ownedInstruments.Add(im.Id);
            }
            foreach (var other in s.roster) if (other.instrumentModel == im.Id) other.instrumentModel = "";   // one player at a time
            m.instrumentModel = im.Id;
            return m.firstName + " now plays the " + im.Name + ".";
        }

        public static string Masterclass(OrchestraState s, MusicianData m, SkillType skill)
        {
            float v = m.Get(skill);
            if (v >= 100f) return m.firstName + "'s " + Skills.DisplayName(skill) + " is already at the maximum.";
            int cost = MasterclassCost(v);
            if (s.coins < cost) return "Not enough coins: this masterclass costs " + cost + ".";
            s.coins -= cost;
            m.Set(skill, v + MasterclassGain);
            return "Masterclass: " + m.firstName + "'s " + Skills.DisplayName(skill) + " " + v.ToString("0.0") + " → " + m.Get(skill).ToString("0.0") + ".";
        }

        // ---- Judges ----
        static readonly string[] JudgeNames = { "Prof. Ilse Varga", "Dr. Kenji Mori", "Marcus Bell", "Amara Okoye", "Sofia Ricci" };
        static readonly string[] JudgeFocus = { "intonation", "note accuracy", "rhythm and ensemble", "the overall impression", "artistry" };
        static readonly float[] JudgeBias = { -0.4f, -0.2f, 0f, 0.2f, 0.4f };   // from strict to generous

        public static List<JudgeVerdict> Judge(PerformanceResult r, Random rng)
        {
            float[] focus = { r.Intonation, r.Accuracy, (r.Rhythm + r.Ensemble) * 0.5f, r.Overall, r.Artistry };
            var list = new List<JudgeVerdict>();
            for (int i = 0; i < JudgeCount; i++)
            {
                float score = (r.Overall + (focus[i] - r.Overall) * 0.5f) / 10f + JudgeBias[i] + ((float)rng.NextDouble() - 0.5f) * 0.6f;
                score = (float)Math.Round(Math.Max(1f, Math.Min(10f, score)), 1);
                list.Add(new JudgeVerdict
                {
                    Name = JudgeNames[i], Focus = JudgeFocus[i], Score = score,
                    Comment = score >= 8.5f ? "Outstanding." : score >= JudgePassMark ? "Convincing." : score >= 4f ? "Not yet ready." : "Far from the standard."
                });
            }
            return list;
        }

        public static int ConcertCoins(List<JudgeVerdict> judges)
        {
            int yes = 0; foreach (var j in judges) if (j.Approves) yes++;
            return yes >= JudgesNeeded ? CoinsApproved : CoinsRejected;
        }
    }
}
