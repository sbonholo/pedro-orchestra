using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Paged "How to Play" guide. Opens automatically on the first New Game; also from the menu and the top bar.</summary>
    public class TutorialScreen : MonoBehaviour
    {
        const string SeenKey = "tutorial_seen";

        static readonly string[][] Pages =
        {
            new[] { "Welcome, Music Director", "Four musicians are on stage: violin, viola, cello and double bass. They start as experienced amateurs (about 31–36 out of 100).", "Your goal: a world-class orchestra. Use headphones." },
            new[] { "Follow the Next step card", "The card at the bottom left always suggests the best move: <b>listen</b>, <b>rehearse</b>, <b>perform</b> or <b>rest</b>.", "Tap its button. That alone is enough to play." },
            new[] { "Train your players", "<b>Orchestra</b> tab: pick a player, tap a skill, then <b>Quick Practice</b>. The <b>Rhythm game</b> gives bigger gains.", "Each new day, every player trains their <b>daily plan</b> on their own." },
            new[] { "Concerts and recruiting", "Concerts earn 1–5 stars and <b>reputation</b>. Reputation attracts stronger applicants in <b>Recruit</b>.", "The <b>Camera</b> button switches views; <b>Hide UI</b> lets you just watch." },
        };

        Text title, body, dots;
        Button back, next;
        int page;

        public static bool Seen { get { return PlayerPrefs.GetInt(SeenKey, 0) == 1; } }

        public void Build()
        {
            var dim = UIFactory.Panel(transform, "Dim", new Color(0, 0, 0, 0.6f), false);
            UIFactory.Stretch(dim.rectTransform);
            var panel = UIFactory.Dialog(transform, "Tutorial Panel", 820);
            UIFactory.Vertical(panel, 12, 24);
            UIFactory.Header(panel.transform, "How to play");
            title = UIFactory.Label(panel.transform, "", 32, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Size(title, 44);
            ScrollRect scroll;
            var content = UIFactory.ScrollArea(panel.transform, out scroll);
            UIFactory.Size(scroll, -1, -1, 1).flexibleHeight = 1;
            body = UIFactory.Label(content, "", 21, TextAnchor.UpperLeft);
            dots = UIFactory.Label(panel.transform, "", 22, TextAnchor.MiddleCenter, UITheme.Accent);
            UIFactory.Size(dots, 30);
            var row = UIFactory.Row(panel.transform, 64);
            back = UIFactory.Button(row, "Back", () => { page = Mathf.Max(0, page - 1); Draw(); }, 22, 64);
            next = UIFactory.Button(row, "Next", () => { if (page == Pages.Length - 1) Close(); else { page++; Draw(); } }, 22, 64, UITheme.AccentDim);
            UIFactory.Button(panel.transform, "Close tutorial", Close, 20, 56);
            gameObject.SetActive(false);
        }

        public void Show(int startPage = 0)
        {
            page = startPage;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Draw();
        }

        void Draw()
        {
            var p = Pages[page];
            title.text = p[0];
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i < p.Length; i++) sb.Append("•  ").Append(p[i]).Append(i < p.Length - 1 ? "\n\n" : "");
            body.text = sb.ToString();
            var d = new System.Text.StringBuilder();
            for (int i = 0; i < Pages.Length; i++) d.Append(i == page ? "● " : "○ ");
            dots.text = d.ToString().TrimEnd();
            back.interactable = page > 0;
            UIFactory.SetLabel(next, page == Pages.Length - 1 ? "Start playing" : "Next");
        }

        void Close() { gameObject.SetActive(false); }
    }
}
