using UnityEngine;

namespace OrchestraProject
{
    /// <summary>Career goals (achievements) with their coin rewards.</summary>
    public class GoalsTab
    {
        public void Build(Transform root, GameManager gm, UIManager ui)
        {
            var s = gm.State;
            int done = 0; foreach (var a in Achievements.All) if (s.achievements.Contains(a.Id)) done++;
            var h = UIFactory.Label(root, "<b>" + done + " / " + Achievements.All.Count + " goals</b>   <size=16>Each goal pays coins once.  Concerts so far: " + s.concertsPerformed + "  ·  best stars: " + s.bestStars + "</size>", 22, TextAnchor.UpperLeft, UITheme.Accent);
            UIFactory.Size(h, 50);
            foreach (var a in Achievements.All)
            {
                bool got = s.achievements.Contains(a.Id);
                var l = UIFactory.Label(root, (got ? "<color=#" + UITheme.Hex(UITheme.Good) + ">DONE</color>  " : "<color=#" + UITheme.Hex(UITheme.TextDim) + ">GOAL</color>  ") + "<b>" + a.Title + "</b>  —  " + a.Description + "   <color=#" + UITheme.Hex(UITheme.Accent) + ">+" + a.Reward + " coins</color>",
                    18, TextAnchor.MiddleLeft, got ? UITheme.Text : UITheme.TextDim);
                UIFactory.Size(l, 40);
            }
        }
    }
}
