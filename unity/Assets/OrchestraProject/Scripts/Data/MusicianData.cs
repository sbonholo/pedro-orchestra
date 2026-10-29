using System;
using System.Collections.Generic;

namespace OrchestraProject
{
    /// <summary>
    /// One musician. Plain serializable data (no Unity objects) so it can be saved to JSON
    /// and tested outside Unity.
    /// </summary>
    [Serializable]
    public class MusicianData
    {
        public string id;
        public string firstName;
        public string lastName;
        public string country;
        public bool female;
        public int age;
        public int yearsExperience;
        public InstrumentType instrument;
        public float[] skills = new float[Skills.Count];
        public float talent = 1f;          // learning speed multiplier, ~0.8 – 1.3
        public float energy = TrainingSystem.MaxEnergy;   // 0 – MaxEnergy (1000), spent by practice, restored each day
        public int trainingFocus = -1;     // -1 = balanced (weakest skill), otherwise (int)SkillType
        public int practiceSessions;
        public int concertsPlayed;
        public int appearanceSeed;
        public bool rareTalent;            // 1 % applicants/invitations: world-class
        public string instrumentModel = "";   // Economy catalog id; "" = the player's own student instrument

        public string FullName { get { return firstName + " " + lastName; } }
        public InstrumentDefinition Instrument { get { return InstrumentCatalog.Get(instrument); } }

        public float Get(SkillType s) { return skills[(int)s]; }
        public float Get01(SkillType s) { return skills[(int)s] / 100f; }
        public void Set(SkillType s, float v) { skills[(int)s] = Math.Max(0f, Math.Min(100f, v)); }

        public float Overall
        {
            get
            {
                float sum = 0f, w = 0f;
                foreach (var s in Skills.All) { sum += Get(s) * Skills.Weight(s); w += Skills.Weight(s); }
                return sum / w;
            }
        }

        public string LevelName { get { return SkillLevels.TierName(Overall); } }

        public SkillType WeakestSkill()
        {
            SkillType best = SkillType.Technique; float min = float.MaxValue;
            foreach (var s in Skills.All) if (Get(s) < min) { min = Get(s); best = s; }
            return best;
        }

        public List<SkillType> Strongest(int n) { var l = Sorted(); l.Reverse(); return l.GetRange(0, Math.Min(n, l.Count)); }
        public List<SkillType> Weakest(int n) { var l = Sorted(); return l.GetRange(0, Math.Min(n, l.Count)); }

        List<SkillType> Sorted()
        {
            var l = new List<SkillType>(Skills.All);
            l.Sort((a, b) => Get(a).CompareTo(Get(b)));
            return l;
        }

        /// <summary>Repairs data from older or hand-edited saves.</summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString("N");
            if (skills == null || skills.Length != Skills.Count)
            {
                var fixedSkills = new float[Skills.Count];
                for (int i = 0; i < fixedSkills.Length; i++) fixedSkills[i] = (skills != null && i < skills.Length) ? skills[i] : 25f;
                skills = fixedSkills;
            }
            for (int i = 0; i < skills.Length; i++) skills[i] = Math.Max(0f, Math.Min(100f, skills[i]));
            if (talent <= 0f) talent = 1f;
            energy = Math.Max(0f, Math.Min(TrainingSystem.MaxEnergy, energy));
            if (instrumentModel == null) instrumentModel = "";
            if (trainingFocus < -1 || trainingFocus >= Skills.Count) trainingFocus = -1;
        }
    }
}
