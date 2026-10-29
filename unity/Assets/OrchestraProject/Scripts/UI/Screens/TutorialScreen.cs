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
            new[] { "Welcome, Music Director", "Four musicians are on stage: violin, viola, cello and double bass, led by your conductor.", "They start around 31–36 out of 100: experienced amateurs.", "Your job: train them into a world-class orchestra. Use headphones.", "Not sure what to do? The <b>Next step</b> card (bottom left) always suggests a good move." },
            new[] { "1 · Meet your musicians", "Tap <b>Roster</b>, or tap a musician on stage.", "The <b>Musician</b> tab shows 11 skills (0–100), an overall level from Novice to Master, and energy." },
            new[] { "2 · Hear them as they are", "Open <b>Music & Concerts</b>, pick <b>Twinkle, Twinkle</b>, choose <b>Separate Parts</b>, then <b>Listen</b>.", "Each player has their own part: violin melody, viola harmony, cello inner line, bass line.", "Wrong notes, out-of-tune notes and drifting apart show their current skill. The report card names the weakest area." },
            new[] { "3 · Train", "<b>Quick Practice:</b> tap a skill in the Musician tab, then Quick Practice (20 energy).", "<b>Rhythm game:</b> Practice → Four Steady Beats → Start. After 4 clicks, tap Space or the pad on each click. A better score gives a bigger gain, up to 2×.", "<b>Daily plan:</b> pick a focus per musician; they train it twice every new day." },
            new[] { "4 · Rehearse and rest", "<b>Rehearse</b> or <b>Quick rehearsal</b> raises the piece's prepared % and ensemble skill.", "Prepared pieces have fewer mistakes in concert.", "Each musician has up to 1,000 energy. <b>Next Day</b> restores 600. <b>+7 Days</b> skips a week." },
            new[] { "5 · Perform", "<b>Perform Concert</b> runs once per day and needs 25 energy per musician.", "<b>Conductor:</b> Automatic, or <b>You conduct</b>: tap the beat (Space or the pad). Steady beating adds up to +6 points; no beating costs 6.", "Three <b>judges</b> score every concert: if two approve you earn <b>30 coins</b>, otherwise 5. Spend coins in the <b>Shop</b> on masterclasses (+3 to a skill: tuning, timing, right notes, tone…) and better instruments, up to ten Stradivarius violins (3,000–5,000 coins).", "You get 1–5 stars and applause to match. Good concerts raise <b>reputation</b>: more of the hall fills up, and better musicians apply." },
            new[] { "6 · Grow the orchestra", "<b>Auditions:</b> weekly applicants. Hear them, compare them with your player, hire.", "<b>Invitations:</b> stronger players; their chance to accept grows with reputation." },
            new[] { "7 · Cameras and a winning routine", "Stage, Audience, Orchestra, Close-up, Free (keys 1–5). In Free: drag to look, scroll or pinch to zoom. <b>Hide UI</b> to watch.", "Routine: daily plans on weakest skills → one rhythm game a day → rehearse 3–4 times → concert → +7 days → check auditions.", "Expect Advanced after ~60 days, Professional after ~140, World-Class after ~275." },
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
