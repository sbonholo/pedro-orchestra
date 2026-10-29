using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Selected musician: profile, all 11 skills, energy, quick practice and daily plan.</summary>
    public class MusicianTab
    {
        SkillType focus = SkillType.Technique;

        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var m = gm.Selected;
            if (m == null) { UIFactory.Label(root, "Tap a musician on stage.", 22); return; }

            // Player chips: switch musician without a separate roster panel
            var list = new System.Collections.Generic.List<MusicianData>(gm.State.roster);
            list.Sort((a, b) => a.Instrument.SeatingOrder != b.Instrument.SeatingOrder ? a.Instrument.SeatingOrder.CompareTo(b.Instrument.SeatingOrder) : b.Overall.CompareTo(a.Overall));
            RectTransform chips = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (i % 3 == 0) chips = UIFactory.Row(root, 50);
                var mm = list[i];
                var c = UIFactory.Button(chips, "<b>" + mm.firstName + "</b>  <size=15>" + mm.Instrument.DisplayName + " · " + mm.Overall.ToString("0") + "</size>", () => gm.Select(mm.id), 18, 50);
                UIFactory.SetSelected(c, mm.id == m.id);
            }
            if (list.Count % 3 != 0) for (int i = list.Count % 3; i < 3; i++) UIFactory.Rect("Spacer", chips);

            var name = UIFactory.Label(root, "<b>" + m.FullName + "</b>", 30);
            UIFactory.Size(name, 40);
            var sub = UIFactory.Label(root, m.Instrument.PlayerTitle + "  ·  " + m.country + "  ·  age " + m.age + "  ·  " + m.yearsExperience + " years playing", 19, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(sub, 28);

            var overallRow = UIFactory.Row(root, 46);
            var ol = UIFactory.Label(overallRow, "Overall  <b>" + m.Overall.ToString("0.0") + "</b>  <color=#" + UITheme.Hex(UITheme.Accent) + ">" + m.LevelName + "</color>", 24);
            UIFactory.Size(ol, -1, 330);
            float next = SkillLevels.NextThreshold(m.Overall);
            var nl = UIFactory.Label(overallRow, next < 100f ? "next level at " + next.ToString("0") : "highest level", 17, TextAnchor.MiddleRight, UITheme.TextDim);
            UIFactory.Size(nl, -1, -1, 1);

            var energyRow = UIFactory.Row(root, 30);
            var el = UIFactory.Label(energyRow, "Energy " + m.energy.ToString("0") + " / " + TrainingSystem.MaxEnergy.ToString("0"), 18); UIFactory.Size(el, -1, 140);
            UIFactory.Bar(energyRow, 14, m.energy >= 0.4f * TrainingSystem.MaxEnergy ? UITheme.Good : UITheme.Bad, m.energy / TrainingSystem.MaxEnergy);
            string learn = m.talent > 1.15f ? "Fast learner" : m.talent < 0.92f ? "Slow, steady learner" : "Average learner";
            var tl = UIFactory.Label(energyRow, learn, 17, TextAnchor.MiddleRight, UITheme.TextDim); UIFactory.Size(tl, -1, 200);

            UIFactory.Header(root, "Skills — tap one to choose a practice focus");
            foreach (var s in Skills.All)
            {
                var skill = s;
                float v = m.Get(s);
                var b = UIFactory.Button(root, "", () => { focus = skill; gm.Notify(Skills.DisplayName(skill) + ": " + Skills.Effect(skill)); ui.Hud.Refresh(); }, 19, 44);
                UIFactory.Clear(b.transform);
                UIFactory.Horizontal(b, 10, 6).childForceExpandWidth = false;
                var label = UIFactory.Label(b.transform, Skills.DisplayName(s), 19);
                UIFactory.Size(label, -1, 230);
                var fill = UIFactory.Bar(b.transform, 14, UIFactory.SkillColor(v), v / 100f);
                ((RectTransform)fill.parent).GetComponent<LayoutElement>().flexibleWidth = 1;
                var val = UIFactory.Label(b.transform, v.ToString("0.0"), 19, TextAnchor.MiddleRight);
                UIFactory.Size(val, -1, 64);
                var tier = UIFactory.Label(b.transform, SkillLevels.TierName(v), 15, TextAnchor.MiddleLeft, UITheme.TextDim);
                UIFactory.Size(tier, -1, 104);
                UIFactory.SetSelected(b, s == focus);
            }

            UIFactory.Header(root, "Training");
            var effect = UIFactory.Label(root, "<i>" + Skills.Effect(focus) + "</i>", 18, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(effect, 30);
            var practice = UIFactory.Button(root, "Quick Practice: " + Skills.DisplayName(focus) + "   (−" + TrainingSystem.EnergyPerPractice + " energy)", () => gm.QuickPractice(focus), 21, 66, UITheme.AccentDim);
            practice.interactable = m.energy >= TrainingSystem.MinEnergyToWork;

            var plan = m.trainingFocus < 0 ? "Balanced" : Skills.DisplayName((SkillType)m.trainingFocus);
            var planRow = UIFactory.Row(root, 60);
            UIFactory.Button(planRow, "Daily plan → " + Skills.ShortName(focus), () => gm.SetTrainingPlan(m, (int)focus), 18, 60);
            UIFactory.Button(planRow, "Daily plan → Balanced", () => gm.SetTrainingPlan(m, -1), 18, 60);
            var pl = UIFactory.Label(root, "Current daily plan: <b>" + plan + "</b>. In Director play style each musician practises it twice whenever a day passes.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(pl, 50);

            var row2 = UIFactory.Row(root, 60);
            UIFactory.Button(row2, "Rhythm game (bigger Rhythm gain)", () => ui.RhythmScreen.Open(m), 19, 60);
            UIFactory.Button(row2, "Close-up", () => gm.Cam.SetMode(CameraMode.CloseUp), 19, 60);

            var stats = UIFactory.Label(root, "Practice sessions: " + m.practiceSessions + "   ·   Concerts: " + m.concertsPlayed, 16, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(stats, 30);
        }
    }
}
