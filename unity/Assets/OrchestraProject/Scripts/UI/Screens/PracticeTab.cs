using UnityEngine;

namespace OrchestraProject
{
    /// <summary>Both practice modes: one-tap quick practice and the interactive rhythm mini-game.</summary>
    public class PracticeTab
    {
        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var m = gm.Selected;
            if (m == null) { UIFactory.Label(root, "Select a musician first.", 22); return; }
            var who = UIFactory.Label(root, "Practising: <b>" + m.FullName + "</b> (" + m.Instrument.PlayerTitle + ")   ·   Energy " + m.energy.ToString("0"), 20);
            UIFactory.Size(who, 34);

            UIFactory.Header(root, "Rhythm mini-game  (you play)");
            var expl = UIFactory.Label(root, "Tap along with a metronome. Your accuracy decides how much " + m.firstName + "'s Rhythm improves — up to 2x a quick practice.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(expl, 48);
            foreach (var ex in RhythmExercise.All)
            {
                var e = ex;
                bool ready = m.Get(SkillType.Rhythm) >= ex.MinRhythmSkill;
                UIFactory.Button(root, ex.Name + "  ·  " + ex.Bpm + " bpm" + (ready ? "" : "  ·  recommended Rhythm " + ex.MinRhythmSkill), () => ui.RhythmScreen.Open(m, e), 20, 62, ready ? UITheme.AccentDim : (Color?)null);
            }

            UIFactory.Header(root, "Quick practice  (one tap)");
            var q = UIFactory.Label(root, "Assign a focused session instantly. Costs " + TrainingSystem.EnergyPerPractice + " energy. Gains shrink as a skill approaches mastery.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(q, 48);
            RectTransform row = null;
            int i = 0;
            foreach (var s in Skills.All)
            {
                if (i % 2 == 0) row = UIFactory.Row(root, 58);
                var skill = s;
                UIFactory.Button(row, Skills.ShortName(s) + "  " + m.Get(s).ToString("0"), () => gm.QuickPractice(skill), 19, 58);
                i++;
            }
            if (i % 2 == 1) UIFactory.Rect("Spacer", row);
        }
    }
}
