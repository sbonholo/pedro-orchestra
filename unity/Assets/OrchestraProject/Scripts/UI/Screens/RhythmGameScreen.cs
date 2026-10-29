using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>
    /// The rhythm mini-game window: instructions, Start button, 4-beat indicator, big TAP pad,
    /// live per-tap feedback and a result screen with real skill gains.
    /// </summary>
    public class RhythmGameScreen : MonoBehaviour
    {
        UIManager ui;
        GameManager gm;
        RhythmMiniGame game;
        MusicianData musician;
        int exerciseIndex;

        Text title, who, instructions, countText, feedbackText, statsText, resultText;
        Image[] beats = new Image[4];
        Text[] beatLabels = new Text[4];
        RectTransform readyGroup, playGroup, resultGroup;
        Image padImage;
        float feedbackFlash;
        int perfect, good, offBeat, missed, extra;

        public void Build(UIManager uiManager, GameManager gameManager)
        {
            ui = uiManager; gm = gameManager; game = gm.Rhythm;
            var dim = UIFactory.Panel(transform, "Dim", new Color(0, 0, 0, 0.55f), false);
            UIFactory.Stretch(dim.rectTransform);
            bool phone = UIManager.IsPhone;
            var panel = UIFactory.Dialog(transform, "Rhythm Panel", 940);
            UIFactory.Vertical(panel, phone ? 6 : 10, phone ? 14 : 24);

            title = UIFactory.Label(panel.transform, "", 30, TextAnchor.MiddleLeft, UITheme.Accent, FontStyle.Bold); UIFactory.Size(title, 42);
            who = UIFactory.Label(panel.transform, "", 19, TextAnchor.MiddleLeft, UITheme.TextDim); UIFactory.Size(who, 28);

            // Beat indicator: 4 circles = the 4 beats of a bar.
            float circle = phone ? 80 : 110;
            var beatRow = UIFactory.Row(panel.transform, circle + 10, 30);
            beatRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            beatRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            for (int i = 0; i < 4; i++)
            {
                var c = UIFactory.Rect("Beat " + (i + 1), beatRow);
                UIFactory.Size(c, circle, circle, 0);
                beats[i] = c.gameObject.AddComponent<Image>();
                beats[i].sprite = UITheme.Circle;
                beatLabels[i] = UIFactory.Label(c, (i + 1).ToString(), 36, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                UIFactory.Stretch(beatLabels[i].rectTransform);
            }
            countText = UIFactory.Label(panel.transform, "", 22, TextAnchor.MiddleCenter); UIFactory.Size(countText, 32);

            // Ready state
            readyGroup = UIFactory.Rect("Ready", panel.transform);
            UIFactory.Vertical(readyGroup, 10);
            instructions = UIFactory.Label(readyGroup, "", phone ? 18 : 20, TextAnchor.UpperLeft); UIFactory.Size(instructions, phone ? 70 : 120);
            var exRow = UIFactory.Row(readyGroup, 56);
            UIFactory.Button(exRow, "< Easier", () => { exerciseIndex = Mathf.Max(0, exerciseIndex - 1); ShowReady(); }, 19, 56);
            UIFactory.Button(exRow, "Harder >", () => { exerciseIndex = Mathf.Min(RhythmExercise.All.Count - 1, exerciseIndex + 1); ShowReady(); }, 19, 56);
            UIFactory.Button(readyGroup, "START PRACTICE", StartExercise, 28, phone ? 76 : 96, UITheme.AccentDim);

            // Playing state: the big pad responds on pointer DOWN for accurate timing.
            playGroup = UIFactory.Rect("Play", panel.transform);
            UIFactory.Vertical(playGroup, 10);
            feedbackText = UIFactory.Label(playGroup, "", 34, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold); UIFactory.Size(feedbackText, 50);
            padImage = UIFactory.Panel(playGroup, "Tap Pad", UITheme.PanelLight);
            UIFactory.Size(padImage, phone ? 170 : 230);
            var padLabel = UIFactory.Label(padImage.transform, "TAP\n<size=20>(or press Space)</size>", 44, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Stretch(padLabel.rectTransform);
            padImage.gameObject.AddComponent<TapPad>().OnTap = () => game.Tap();
            statsText = UIFactory.Label(playGroup, "", 18, TextAnchor.MiddleCenter, UITheme.TextDim); UIFactory.Size(statsText, 28);

            // Result state
            resultGroup = UIFactory.Rect("Result", panel.transform);
            UIFactory.Vertical(resultGroup, 10);
            resultText = UIFactory.Label(resultGroup, "", phone ? 18 : 21, TextAnchor.UpperLeft); UIFactory.Size(resultText, phone ? 180 : 300);
            var rr = UIFactory.Row(resultGroup, 70);
            UIFactory.Button(rr, "Try again", StartExercise, 22, 70, UITheme.AccentDim);
            UIFactory.Button(rr, "Other exercise", ShowReady, 22, 70);

            UIFactory.Button(panel.transform, "Close", Close, 20, 56);

            game.Feedback += OnFeedback;
            game.Finished += OnFinished;
            gameObject.SetActive(false);
        }

        public void Open(MusicianData m, RhythmExercise ex = null)
        {
            musician = m;
            if (ex != null) exerciseIndex = RhythmExercise.All.IndexOf(ex);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowReady();
        }

        void ShowReady()
        {
            game.Cancel();
            var ex = RhythmExercise.All[exerciseIndex];
            title.text = "Rhythm Practice — " + ex.Name;
            who.text = musician.FullName + " (" + musician.Instrument.PlayerTitle + ")  ·  Rhythm " + musician.Get(SkillType.Rhythm).ToString("0.0") + "  ·  Energy " + musician.energy.ToString("0");
            instructions.text = ex.Instructions + "\n\nTempo " + ex.Bpm + " bpm. Use headphones or speakers. Tap the big pad, or press <b>Space</b>."
                                + (musician.energy < TrainingSystem.MinEnergyToWork ? "\n<color=#" + UITheme.Hex(UITheme.Bad) + ">" + musician.firstName + " is too tired: you can play, but no skill will be gained.</color>" : "");
            countText.text = "Pattern: " + PatternText(ex);
            readyGroup.gameObject.SetActive(true);
            playGroup.gameObject.SetActive(false);
            resultGroup.gameObject.SetActive(false);
            SetBeats(-1, false);
        }

        static string PatternText(RhythmExercise ex)
        {
            var sb = new System.Text.StringBuilder();
            for (int beat = 0; beat < 4; beat++)
            {
                bool on = false, and = false;
                foreach (var b in ex.TapBeats) { if (Mathf.Approximately(b % 4, beat)) on = true; if (Mathf.Approximately(b % 4, beat + 0.5f)) and = true; }
                sb.Append(on ? (beat + 1).ToString() : "·").Append(and ? " &  " : "    ");
            }
            return sb.ToString().TrimEnd() + "   (repeat)";
        }

        void StartExercise()
        {
            perfect = good = offBeat = missed = extra = 0;
            feedbackText.text = "Listen…";
            feedbackText.color = UITheme.TextDim;
            statsText.text = "";
            readyGroup.gameObject.SetActive(false);
            resultGroup.gameObject.SetActive(false);
            playGroup.gameObject.SetActive(true);
            game.Begin(RhythmExercise.All[exerciseIndex]);
        }

        void OnFeedback(TapFeedback fb)
        {
            switch (fb.Grade)
            {
                case TapGrade.Perfect: perfect++; feedbackText.color = UITheme.Accent; break;
                case TapGrade.Good: good++; feedbackText.color = UITheme.Good; break;
                case TapGrade.Early:
                case TapGrade.Late: offBeat++; feedbackText.color = new Color(0.9f, 0.7f, 0.4f); break;
                case TapGrade.Extra: extra++; feedbackText.color = UITheme.Bad; break;
                default: missed++; feedbackText.color = UITheme.Bad; break;
            }
            feedbackText.text = fb.Text;
            feedbackFlash = 1f;
            statsText.text = "Perfect " + perfect + "   Good " + good + "   Off-beat " + offBeat + "   Missed " + missed + "   Extra " + extra;
        }

        void OnFinished()
        {
            var sc = game.Scorer;
            var gains = gm.ApplyRhythmPractice(musician, sc.Score);
            float bias = sc.AverageOffsetMs;
            string tendency = Mathf.Abs(bias) < 15f ? "centred on the beat" : bias < 0 ? "rushing (" + bias.ToString("0") + " ms)" : "dragging (+" + bias.ToString("0") + " ms)";
            resultText.text = "<size=40><b>" + Mathf.RoundToInt(sc.Score * 100) + "%</b></size>   " + sc.Verdict + "\n\n"
                + "Hits " + sc.Hits + " / " + sc.TargetCount + "   ·   extra taps " + sc.ExtraTaps + "\n"
                + "Average timing error ±" + sc.AverageAbsOffsetMs.ToString("0") + " ms, " + tendency + "\n\n"
                + "<color=#" + UITheme.Hex(UITheme.Accent) + ">" + gains.Summary() + "</color>";
            playGroup.gameObject.SetActive(false);
            resultGroup.gameObject.SetActive(true);
            countText.text = "";
            SetBeats(-1, false);
        }

        void Close()
        {
            game.Cancel();
            gameObject.SetActive(false);
            ui.Hud.Refresh();
        }

        void SetBeats(int active, bool countIn, float flash = 0f)
        {
            for (int i = 0; i < 4; i++)
            {
                Color baseCol = countIn ? new Color(0.25f, 0.30f, 0.40f) : new Color(0.22f, 0.20f, 0.24f);
                Color lit = countIn ? new Color(0.55f, 0.70f, 0.95f) : UITheme.Accent;
                beats[i].color = i == active ? Color.Lerp(baseCol, lit, 0.4f + 0.6f * flash) : baseCol;
                beats[i].rectTransform.localScale = Vector3.one * (i == active ? 1f + 0.12f * flash : 1f);
            }
        }

        void Update()
        {
            if (game.Phase == RhythmPhase.CountIn || game.Phase == RhythmPhase.Playing)
            {
                double pos = game.BeatPosition;
                var ex = game.Exercise;
                if (pos < 0) { SetBeats(-1, true); countText.text = "Get ready…"; }
                else
                {
                    int beat = (int)pos;
                    float frac = (float)(pos - beat);
                    bool countIn = beat < ex.CountInBeats;
                    SetBeats(beat % 4, countIn, 1f - Mathf.Clamp01(frac * 2.5f));
                    countText.text = countIn ? "Count-in:  " + (beat + 1) + "  —  start tapping after 4"
                        : "Bar " + ((beat - ex.CountInBeats) / 4 + 1) + " of " + (ex.ExerciseBeats / 4) + "   ·   " + PatternText(ex);
                }
            }
            feedbackFlash = Mathf.MoveTowards(feedbackFlash, 0f, Time.unscaledDeltaTime * 3f);
            if (padImage != null) padImage.color = Color.Lerp(UITheme.PanelLight, UITheme.AccentDim, feedbackFlash);
        }
    }
}
