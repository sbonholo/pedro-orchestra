using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Hear and compare applicants, hire them, or invite stronger musicians.</summary>
    public class AuditionTab
    {
        string expandedId;

        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var s = gm.State;
            int refreshIn = Mathf.Max(0, RecruitmentSystem.RefreshEveryDays - (s.day - s.lastPoolRefreshDay));
            var intro = UIFactory.Label(root, "New applicants every " + RecruitmentSystem.RefreshEveryDays + " days (next in " + refreshIn + "). Higher reputation attracts stronger players.", 17, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(intro, 48);

            UIFactory.Header(root, "Auditions — applicants");
            if (s.auditionPool.Count == 0) UIFactory.Size(UIFactory.Label(root, "No applicants right now.", 18, TextAnchor.MiddleLeft, UITheme.TextDim), 30);
            foreach (var c in s.auditionPool.ToArray()) Card(root, gm, ui, c, false);

            UIFactory.Header(root, "Invitations — established musicians");
            if (s.invitePool.Count == 0) UIFactory.Size(UIFactory.Label(root, "No one to invite this week.", 18, TextAnchor.MiddleLeft, UITheme.TextDim), 30);
            foreach (var c in s.invitePool.ToArray()) Card(root, gm, ui, c, true);
        }

        void Card(Transform root, GameManager gm, UIManager ui, MusicianData c, bool invite)
        {
            var card = UIFactory.Panel(root, "Card", UITheme.PanelLight);
            UIFactory.Vertical(card, 6, 12);
            var title = UIFactory.Label(card.transform, "<b>" + c.FullName + "</b>  ·  " + c.Instrument.PlayerTitle, 22);
            UIFactory.Size(title, 30);
            var leader = RecruitmentSystem.SectionLeader(gm.State, c.instrument);
            string compare = leader == null ? "  ·  <color=#" + UITheme.Hex(UITheme.Good) + ">fills an empty section</color>"
                : "  ·  vs your " + leader.firstName + ": " + Signed(c.Overall - leader.Overall);
            var line = UIFactory.Label(card.transform, c.LevelName + " " + c.Overall.ToString("0") + "  ·  age " + c.age + "  ·  " + c.yearsExperience + " yrs" + compare, 18, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(line, 26);
            var sw = UIFactory.Label(card.transform,
                "<color=#" + UITheme.Hex(UITheme.Good) + ">Strengths:</color> " + Names(c.Strongest(2)) + "    <color=#" + UITheme.Hex(UITheme.Bad) + ">Weaknesses:</color> " + Names(c.Weakest(2)), 17, TextAnchor.UpperLeft);
            UIFactory.Size(sw, 26);
            if (invite)
            {
                var ch = UIFactory.Label(card.transform, "Chance to accept: " + (RecruitmentSystem.InviteAcceptance(gm.State, c) * 100f).ToString("0") + "%  (one attempt)", 17, TextAnchor.UpperLeft, UITheme.Accent);
                UIFactory.Size(ch, 24);
            }

            if (expandedId == c.id)
            {
                foreach (var s in Skills.All)
                {
                    var row = UIFactory.Row(card.transform, 24, 6);
                    var l = UIFactory.Label(row, Skills.ShortName(s), 16); UIFactory.Size(l, -1, 130);
                    UIFactory.Bar(row, 10, UIFactory.SkillColor(c.Get(s)), c.Get(s) / 100f);
                    string cmp = leader != null ? "  <color=#" + UITheme.Hex(UITheme.TextDim) + ">(" + leader.Get(s).ToString("0") + ")</color>" : "";
                    var v = UIFactory.Label(row, c.Get(s).ToString("0") + cmp, 16, TextAnchor.MiddleRight); UIFactory.Size(v, -1, 110);
                }
                if (leader != null) UIFactory.Size(UIFactory.Label(card.transform, "(grey numbers = your current " + leader.Instrument.PlayerTitle.ToLowerInvariant() + ")", 15, TextAnchor.UpperLeft, UITheme.TextDim), 22);
            }

            var buttons = UIFactory.Row(card.transform, 58);
            var hear = UIFactory.Button(buttons, invite ? "Hear recording" : "Hear audition", () => gm.HearAudition(c), 18, 58);
            hear.interactable = !gm.Performance.IsBusy;
            UIFactory.Button(buttons, expandedId == c.id ? "Hide details" : "Compare skills", () => { expandedId = expandedId == c.id ? null : c.id; ui.Hud.Refresh(); }, 18, 58);
            var act = UIFactory.Button(buttons, invite ? "Send invitation" : "Hire", () => { if (invite) gm.Invite(c); else gm.Hire(c); }, 18, 58, UITheme.AccentDim);
            act.interactable = !gm.Performance.IsBusy;
        }

        static string Signed(float v) { return (v >= 0 ? "<color=#" + UITheme.Hex(UITheme.Good) + ">+" : "<color=#" + UITheme.Hex(UITheme.Bad) + ">") + v.ToString("0") + "</color>"; }

        static string Names(System.Collections.Generic.List<SkillType> list)
        {
            var parts = new string[list.Count];
            for (int i = 0; i < list.Count; i++) parts[i] = Skills.ShortName(list[i]);
            return string.Join(", ", parts);
        }
    }
}
