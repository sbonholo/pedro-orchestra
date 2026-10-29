using System;

namespace OrchestraProject
{
    /// <summary>The eleven abilities every musician has. Values run from 0 to 100.</summary>
    public enum SkillType
    {
        Technique,
        Speed,
        Intonation,
        ToneQuality,
        Musicality,
        Expression,
        Rhythm,
        SightReading,
        Accuracy,
        Ensemble,
        Confidence
    }

    public static class Skills
    {
        public const int Count = 11;
        public static readonly SkillType[] All = (SkillType[])Enum.GetValues(typeof(SkillType));

        public static string DisplayName(SkillType s)
        {
            switch (s)
            {
                case SkillType.ToneQuality: return "Tone Quality";
                case SkillType.Expression: return "Emotional Expression";
                case SkillType.SightReading: return "Sight-reading";
                case SkillType.Ensemble: return "Ensemble Playing";
                case SkillType.Confidence: return "Performance Confidence";
                default: return s.ToString();
            }
        }

        public static string ShortName(SkillType s)
        {
            switch (s)
            {
                case SkillType.ToneQuality: return "Tone";
                case SkillType.Expression: return "Expression";
                case SkillType.SightReading: return "Sight-reading";
                case SkillType.Ensemble: return "Ensemble";
                case SkillType.Confidence: return "Confidence";
                default: return s.ToString();
            }
        }

        /// <summary>What the skill changes in the sound — shown in the UI so stats are not abstract.</summary>
        public static string Effect(SkillType s)
        {
            switch (s)
            {
                case SkillType.Technique: return "Cleaner note starts, harder pieces, steadier bowing.";
                case SkillType.Speed: return "Maximum tempo before the musician slows the ensemble down.";
                case SkillType.Intonation: return "How close each note is to the correct pitch (in cents).";
                case SkillType.ToneQuality: return "Less bow noise and scratch, warmer and brighter sound.";
                case SkillType.Musicality: return "Phrase shaping: notes grow and relax with the melody.";
                case SkillType.Expression: return "Vibrato depth and emotional colour on long notes.";
                case SkillType.Rhythm: return "How precisely each note lands on the beat.";
                case SkillType.SightReading: return "Accuracy on pieces the orchestra has not rehearsed yet.";
                case SkillType.Accuracy: return "Fewer wrong notes.";
                case SkillType.Ensemble: return "Stays together with the others instead of drifting.";
                case SkillType.Confidence: return "Fewer nerves (and mistakes) in auditions and concerts.";
                default: return "";
            }
        }

        /// <summary>Practising one skill also helps these two a little.</summary>
        public static SkillType[] Related(SkillType s)
        {
            switch (s)
            {
                case SkillType.Technique: return new[] { SkillType.Speed, SkillType.Accuracy };
                case SkillType.Speed: return new[] { SkillType.Technique, SkillType.Rhythm };
                case SkillType.Intonation: return new[] { SkillType.ToneQuality, SkillType.Accuracy };
                case SkillType.ToneQuality: return new[] { SkillType.Intonation, SkillType.Expression };
                case SkillType.Musicality: return new[] { SkillType.Expression, SkillType.Confidence };
                case SkillType.Expression: return new[] { SkillType.Musicality, SkillType.ToneQuality };
                case SkillType.Rhythm: return new[] { SkillType.Ensemble, SkillType.Accuracy };
                case SkillType.SightReading: return new[] { SkillType.Accuracy, SkillType.Rhythm };
                case SkillType.Accuracy: return new[] { SkillType.Technique, SkillType.Intonation };
                case SkillType.Ensemble: return new[] { SkillType.Rhythm, SkillType.Confidence };
                case SkillType.Confidence: return new[] { SkillType.Musicality, SkillType.Expression };
                default: return new SkillType[0];
            }
        }

        /// <summary>Weight of each skill in the overall rating.</summary>
        public static float Weight(SkillType s)
        {
            switch (s)
            {
                case SkillType.Technique: return 1.2f;
                case SkillType.Speed: return 0.8f;
                case SkillType.Intonation: return 1.1f;
                case SkillType.ToneQuality: return 1.0f;
                case SkillType.Musicality: return 1.1f;
                case SkillType.Expression: return 0.9f;
                case SkillType.Rhythm: return 1.0f;
                case SkillType.SightReading: return 0.6f;
                case SkillType.Accuracy: return 1.0f;
                case SkillType.Ensemble: return 0.9f;
                case SkillType.Confidence: return 0.6f;
                default: return 1f;
            }
        }
    }

    /// <summary>Named ability tiers, from first lessons to master.</summary>
    public static class SkillLevels
    {
        static readonly float[] Thresholds = { 0f, 20f, 35f, 50f, 65f, 80f, 92f };
        static readonly string[] Names = { "Novice", "Student", "Intermediate", "Advanced", "Professional", "World-Class", "Master" };

        public static string TierName(float value)
        {
            for (int i = Thresholds.Length - 1; i >= 0; i--)
                if (value >= Thresholds[i]) return Names[i];
            return Names[0];
        }

        /// <summary>Value needed for the next tier, or 100 when already at the top.</summary>
        public static float NextThreshold(float value)
        {
            for (int i = 0; i < Thresholds.Length; i++)
                if (value < Thresholds[i]) return Thresholds[i];
            return 100f;
        }
    }
}
