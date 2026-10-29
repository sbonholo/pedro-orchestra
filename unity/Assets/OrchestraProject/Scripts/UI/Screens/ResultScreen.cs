using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Performance report: stars, five category scores, critic's review, per-musician details, gains.</summary>
    public class ResultScreen : MonoBehaviour
    {
        GameManager gm;
        UIManager ui;
        RectTransform body;

        public void Build(UIManager uiManager, GameManager gameManager)
        {
            ui = uiManager; gm = gameManager;
            var dim = UIFactory.Panel(transform, "Dim", new Color(0, 0, 0, 0.55f), false);
            UIFactory.Stretch(dim.rectTransform);
            var panel = UIFactory.Dialog(transform, "Result Panel", 960);
            UIFactory.Vertical(panel, 10, 24);
            ScrollRect sr;
            body = UIFactory.ScrollArea(panel.transform, out sr);
            UIFactory.Size(sr, -1, -1, 1).flexibleHeight = 1;
            UIFactory.Button(panel.transform, "Continue", () => { gameObject.SetActive(false); ui.Hud.Refresh(); }, 24, 68, UITheme.AccentDim);
            gameObject.SetActive(false);
        }

        public void Show(PerformanceResult r, TrainingResult gains)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UIFactory.Clear(body);

            string kind = r.Context == PerformanceContext.Concert ? "Concert Review" : r.Context == PerformanceContext.Audition ? "Audition" :
                          r.Context == PerformanceContext.Rehearsal ? "Rehearsal Report" : "Performance";
            var t = UIFactory.Label(body, kind, 20, TextAnchor.MiddleLeft, UITheme.Accent, FontStyle.Bold); UIFactory.Size(t, 30);
            var p = UIFactory.Label(body, "<b>" + r.Piece.Title + "</b>  <size=18>(" + (r.Mode == ArrangementMode.SeparateParts ? "separate parts" : r.Mode == ArrangementMode.Solo ? "solo" : "same melody") + ")</size>", 28); UIFactory.Size(p, 40);

            var starRow = UIFactory.Row(body, 60, 10);
            starRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            for (int i = 0; i < 5; i++)
            {
                var star = UIFactory.Rect("Star", starRow);
                UIFactory.Size(star, 44, 44, 0);
                var img = star.gameObject.AddComponent<Image>();
                img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced;
                img.color = i < r.Stars ? UITheme.Accent : new Color(0.25f, 0.23f, 0.22f);
                star.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var score = UIFactory.Label(starRow, "  " + r.Stars + " / 5   ·   score " + r.Overall.ToString("0"), 26, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Size(score, -1, 400, 1);

            var review = UIFactory.Label(body, "<i>\"" + r.Review + "\"</i>", 20, TextAnchor.UpperLeft); UIFactory.Size(review, 60);

            Category("Rhythm", r.Rhythm);
            Category("Intonation", r.Intonation);
            Category("Note accuracy", r.Accuracy);
            Category("Ensemble", r.Ensemble);
            Category("Artistry", r.Artistry);

            if (r.Judges != null)
            {
                int yes = 0; foreach (var j in r.Judges) if (j.Approves) yes++;
                UIFactory.Header(body, "The judges  (" + yes + " of " + Economy.JudgeCount + " approve)");
                foreach (var j in r.Judges)
                {
                    var jl = UIFactory.Label(body, "<b>" + j.Name + "</b>  <size=15>(listens for " + j.Focus + ")</size>   " + j.Score.ToString("0.0") + " / 10   " + (j.Approves ? "YES  " : "NO  ") + j.Comment,
                        18, TextAnchor.UpperLeft, j.Approves ? UITheme.Good : UITheme.Bad);
                    UIFactory.Size(jl, 28);
                }
                var tk = UIFactory.Label(body, "<b>Tickets: " + r.TicketsSold.ToString("N0") + " listeners × " + Economy.TicketPrice + " = +" + r.TicketCoins.ToString("N0") + " coins</b>", 20, TextAnchor.UpperLeft, UITheme.Accent);
                UIFactory.Size(tk, 32);
                var cl = UIFactory.Label(body, "<b>+" + r.CoinsEarned + " coins</b>" + (r.CoinsEarned == Economy.CoinsApproved ? "  The judges approved." : "  " + Economy.JudgesNeeded + " of " + Economy.JudgeCount + " judges must approve for " + Economy.CoinsApproved + " coins."), 20, TextAnchor.UpperLeft, UITheme.Accent);
                UIFactory.Size(cl, 32);
            }

            UIFactory.Header(body, "What the audience heard");
            foreach (var line in r.MusicianNotes)
            {
                var l = UIFactory.Label(body, line, 17, TextAnchor.UpperLeft, UITheme.TextDim); UIFactory.Size(l, 26);
            }

            if (gains != null && gains.Success)
            {
                UIFactory.Header(body, "Progress");
                var g = UIFactory.Label(body, gains.Summary(), 18, TextAnchor.UpperLeft, UITheme.Accent);
                UIFactory.Size(g, 50);
            }
            else if (r.Context == PerformanceContext.Audition)
            {
                var a = UIFactory.Label(body, "Decide in the Auditions tab: hire, invite, or wait for the next applicants.", 18, TextAnchor.UpperLeft, UITheme.TextDim);
                UIFactory.Size(a, 30);
            }
            else if (r.Context == PerformanceContext.Preview)
            {
                var a = UIFactory.Label(body, "Listening only: no training effect. Use Rehearse to improve preparation.", 18, TextAnchor.UpperLeft, UITheme.TextDim);
                UIFactory.Size(a, 30);
            }
        }

        void Category(string name, float value)
        {
            var row = UIFactory.Row(body, 30);
            var l = UIFactory.Label(row, name, 19); UIFactory.Size(l, -1, 200);
            UIFactory.Bar(row, 14, UIFactory.SkillColor(value), value / 100f);
            var v = UIFactory.Label(row, value.ToString("0"), 19, TextAnchor.MiddleRight); UIFactory.Size(v, -1, 60);
        }
    }
}
