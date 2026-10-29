using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Volume, graphics quality, play style, tap calibration and save management.</summary>
    public class SettingsScreen : MonoBehaviour
    {
        UIManager ui;
        GameManager gm;
        RectTransform body;
        bool confirmDelete;

        public static void ApplySaved()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("volume", 0.85f);
            int q = PlayerPrefs.GetInt("quality", -1);
            if (q >= 0 && q < QualitySettings.names.Length) QualitySettings.SetQualityLevel(q, true);
            RhythmMiniGame.CalibrationMs = PlayerPrefs.GetFloat("calibration", 0f);
        }

        public void Build(UIManager uiManager, GameManager gameManager)
        {
            ui = uiManager; gm = gameManager;
            var dim = UIFactory.Panel(transform, "Dim", new Color(0, 0, 0, 0.6f), false);
            UIFactory.Stretch(dim.rectTransform);
            var panel = UIFactory.Dialog(transform, "Settings Panel", 780);
            UIFactory.Vertical(panel, 10, 20);
            var t = UIFactory.Label(panel.transform, "Settings", 30, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Size(t, 44);
            ScrollRect scroll;
            body = UIFactory.ScrollArea(panel.transform, out scroll);
            UIFactory.Size(scroll, -1, -1, 1).flexibleHeight = 1;
            UIFactory.Button(panel.transform, "Close", () => gameObject.SetActive(false), 22, 62, UITheme.AccentDim);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            confirmDelete = false;
            Rebuild();
        }

        void Rebuild()
        {
            UIFactory.Clear(body);
            float vol = AudioListener.volume;
            Stepper("Volume", Mathf.RoundToInt(vol * 100) + "%", () => SetVolume(vol - 0.1f), () => SetVolume(vol + 0.1f));

            int q = QualitySettings.GetQualityLevel();
            var names = QualitySettings.names;
            Stepper("Graphics quality", names.Length > 0 ? names[q] : "-", () => SetQuality(q - 1), () => SetQuality(q + 1));

            float cal = RhythmMiniGame.CalibrationMs;
            Stepper("Tap calibration", (cal >= 0 ? "+" : "") + cal.ToString("0") + " ms", () => SetCal(cal - 10), () => SetCal(cal + 10));
            var help = UIFactory.Label(body, "Raise calibration if the rhythm game says you are always LATE while you feel on the beat (Bluetooth headphones add delay).", 15, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(help, 40);

            if (gm.InGame)
            {
                var row = UIFactory.Row(body, 58);
                var l = UIFactory.Label(row, "Play style", 20); UIFactory.Size(l, -1, 200);
                var director = UIFactory.Button(row, "Director", () => { gm.SetPlayStyle(PlayStyle.Director); Rebuild(); }, 19, 58);
                var hands = UIFactory.Button(row, "Hands-on", () => { gm.SetPlayStyle(PlayStyle.HandsOn); Rebuild(); }, 19, 58);
                UIFactory.SetSelected(director, gm.State.playStyle == (int)PlayStyle.Director);
                UIFactory.SetSelected(hands, gm.State.playStyle == (int)PlayStyle.HandsOn);
                var ps = UIFactory.Label(body, "Director: musicians follow their daily plans automatically when days pass (watch & manage).\nHands-on: they only improve through the practice you run yourself.", 15, TextAnchor.UpperLeft, UITheme.TextDim);
                UIFactory.Size(ps, 44);
            }

            var del = UIFactory.Button(body, confirmDelete ? "Tap again to permanently delete the saved game" : "Delete saved game", () =>
            {
                if (!confirmDelete) { confirmDelete = true; Rebuild(); return; }
                SaveSystem.Delete();
                gm.Notify("Saved game deleted.");
                confirmDelete = false;
                Rebuild();
            }, 18, 54, new Color(0.35f, 0.14f, 0.12f));
            UIFactory.Size(del, 54);
            var path = UIFactory.Label(body, "Save file: " + SaveSystem.PathMain, 13, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(path, 36);
        }

        void Stepper(string label, string value, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus)
        {
            var row = UIFactory.Row(body, 56);
            var l = UIFactory.Label(row, label, 20); UIFactory.Size(l, -1, 220);
            UIFactory.Size(UIFactory.Button(row, "−", minus, 26, 56), -1, 70, 0);
            var v = UIFactory.Label(row, value, 20, TextAnchor.MiddleCenter, UITheme.Accent); UIFactory.Size(v, -1, 200, 1);
            UIFactory.Size(UIFactory.Button(row, "+", plus, 26, 56), -1, 70, 0);
        }

        void SetVolume(float v) { AudioListener.volume = Mathf.Clamp01(v); PlayerPrefs.SetFloat("volume", AudioListener.volume); Rebuild(); }

        void SetQuality(int q)
        {
            q = Mathf.Clamp(q, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(q, true);
            PlayerPrefs.SetInt("quality", q);
            Rebuild();
        }

        void SetCal(float ms) { RhythmMiniGame.CalibrationMs = Mathf.Clamp(ms, -150, 300); PlayerPrefs.SetFloat("calibration", RhythmMiniGame.CalibrationMs); Rebuild(); }
    }
}
