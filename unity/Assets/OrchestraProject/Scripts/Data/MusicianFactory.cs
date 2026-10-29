using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>Creates musicians: the starting four, audition candidates and invitation targets.</summary>
    public static class MusicianFactory
    {
        static readonly string[][] Names =
        {
            // first names (female), first names (male), last names, country
            new[] { "Clara", "Sofia", "Hana", "Elena", "Amira", "Ingrid", "Lucía", "Mei", "Anya", "Chiara", "Nadia", "Yuki", "Beatriz", "Freya", "Leila", "Marta" },
            new[] { "Tomás", "Felix", "Kenji", "Mateo", "Jonas", "Arjun", "Luca", "Pavel", "Samuel", "Oskar", "Rafael", "Daniel", "Henrik", "Omar", "Viktor", "André" },
            new[] { "Moreau", "Lindqvist", "Takahashi", "Rossi", "Novak", "Fernandes", "Weber", "Kowalski", "Haddad", "Okafor", "Petrov", "Silva", "Brennan", "Castellano", "Jansen", "Mendes", "Ito", "Kaya", "Dubois", "Nieminen" },
            new[] { "France", "Sweden", "Japan", "Italy", "Czechia", "Brazil", "Germany", "Poland", "Lebanon", "Nigeria", "Bulgaria", "Portugal", "Ireland", "Spain", "Netherlands", "Finland", "Turkey", "Korea", "Austria", "Canada" }
        };

        public static List<MusicianData> CreateStartingOrchestra(Random rng)
        {
            // Experienced amateurs: overall ~30-38, each with a different personality.
            var list = new List<MusicianData>
            {
                Create(InstrumentType.Violin, 36f, rng, SkillType.Speed, SkillType.Expression),
                Create(InstrumentType.Viola, 33f, rng, SkillType.Ensemble, SkillType.ToneQuality),
                Create(InstrumentType.Cello, 35f, rng, SkillType.Musicality, SkillType.Intonation),
                Create(InstrumentType.DoubleBass, 31f, rng, SkillType.Rhythm, SkillType.Confidence)
            };
            return list;
        }

        /// <summary>Creates a musician whose skills average about <paramref name="targetMean"/>.</summary>
        public static MusicianData Create(InstrumentType instrument, float targetMean, Random rng,
            SkillType? strength = null, SkillType? secondStrength = null)
        {
            bool female = rng.NextDouble() < 0.5;
            var m = new MusicianData
            {
                id = Guid.NewGuid().ToString("N"),
                female = female,
                firstName = Pick(Names[female ? 0 : 1], rng),
                lastName = Pick(Names[2], rng),
                country = Pick(Names[3], rng),
                instrument = instrument,
                talent = (float)(0.8 + rng.NextDouble() * 0.5),
                appearanceSeed = rng.Next(),
                trainingFocus = -1,
                energy = 100f
            };

            if (strength == null) strength = Skills.All[rng.Next(Skills.Count)];
            if (secondStrength == null) secondStrength = Skills.All[rng.Next(Skills.Count)];
            SkillType weakness = Skills.All[rng.Next(Skills.Count)];
            if (weakness == strength.Value) weakness = Skills.All[((int)weakness + 3) % Skills.Count];

            foreach (var s in Skills.All)
            {
                float v = targetMean + (float)Gaussian(rng) * 4.5f;
                if (s == strength.Value) v += 9f;
                if (s == secondStrength.Value) v += 5f;
                if (s == weakness) v -= 8f;
                m.Set(s, v);
            }

            // Shift so the overall rating matches the target.
            float correction = targetMean - m.Overall;
            foreach (var s in Skills.All) m.Set(s, m.Get(s) + correction);

            m.age = 19 + (int)(targetMean / 3f) + rng.Next(0, 12);
            m.yearsExperience = Math.Max(2, (int)(targetMean / 4.5f) + rng.Next(-2, 4));
            return m;
        }

        static string Pick(string[] arr, Random rng) { return arr[rng.Next(arr.Length)]; }

        public static double Gaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
