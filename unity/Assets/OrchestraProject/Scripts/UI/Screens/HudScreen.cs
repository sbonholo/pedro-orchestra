using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    public enum HudTab { None, Musician, Music, Auditions }

    /// <summary>
    /// In-game interface, kept deliberately small: three tabs on top (Orchestra, Concerts, Recruit),
    /// the active tab on the right, a "Next step" card with one suggested action, and one camera button.
    /// "Hide UI" leaves only the stage. Same layout as the web preview.
    /// </summary>
    public class HudScreen : MonoBehaviour
    {
        UIManager ui;
        GameManager gm;
        RectTransform topBar, rightPanel, bottomBar, tabContent, coachPanel;
        Text tabTitle, statusText, coachText;
        RectTransform progressFill;
        Button stopButton, showUiButton, camButton, coachButton;
        readonly Dictionary<HudTab, Button> navButtons = new Dictionary<HudTab, Button>();
        HudTab currentTab = HudTab.None;
        bool uiHidden;
        bool concertOnStage, peekDuringConcert;   // panels step aside during a concert; any tab brings them back
        NextStep step;
        string shownStep;

        bool PanelsAside { get { return concertOnStage && !peekDuringConcert; } }

        static readonly CameraMode[] CamOrder = { CameraMode.Orchestra, CameraMode.Stage, CameraMode.Audience, CameraMode.CloseUp, CameraMode.Free };

        public readonly MusicianTab MusicianTab = new MusicianTab();
        public readonly MusicTab MusicTab = new MusicTab();
        public readonly AuditionTab AuditionTab = new AuditionTab();

        public void Build(UIManager uiManager, GameManager gameManager)
        {
            ui = uiManager; gm = gameManager;
            bool phone = UIManager.IsPhone;
            float panelTop = 88f, panelBottom = 100f;

            // ---- Top bar ----
            topBar = UIFactory.Panel(transform, "Top Bar", UITheme.Background, false).rectTransform;
            UIFactory.Anchor(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -78), Vector2.zero);
            var h = UIFactory.Horizontal(topBar, 8, 8);
            h.childForceExpandWidth = false;
            if (!phone)
            {
                var title = UIFactory.Label(topBar, "<b>ORCHESTRA</b> PROJECT", 24, TextAnchor.MiddleLeft, UITheme.Accent);
                UIFactory.Size(title, -1, 300);
            }
            AddNav(HudTab.Musician, "Orchestra");
            AddNav(HudTab.Music, "Concerts");
            AddNav(HudTab.Auditions, "Recruit");
            var spacer = UIFactory.Rect("Spacer", topBar); UIFactory.Size(spacer, -1, 0, 1);
            Nav("Next Day", () => gm.AdvanceDays(1), phone ? 130 : 150);
            Nav("+7 Days", () => gm.AdvanceDays(7), phone ? 120 : 140);
            Nav("?", () => ui.Tutorial.Show(), 64);
            Nav("Menu", () => ui.ShowMenu(), phone ? 100 : 120);

            // ---- Active tab (right) ----
            rightPanel = UIFactory.Panel(transform, "Tab Panel", UITheme.Panel).rectTransform;
            float w = 660;
            UIFactory.Anchor(rightPanel, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-w - 10, panelBottom), new Vector2(-10, -panelTop));
            UIFactory.Vertical(rightPanel, 6, 12);
            var header = UIFactory.Row(rightPanel, 44);
            tabTitle = UIFactory.Label(header, "", 26, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Size(tabTitle, -1, -1, 1);
            var close = UIFactory.Button(header, "Close", () => Open(HudTab.None), 18, 40);
            UIFactory.Size(close, 40, 100, 0);
            ScrollRect ts;
            tabContent = UIFactory.ScrollArea(rightPanel, out ts);
            UIFactory.Size(ts, -1, -1, 1).flexibleHeight = 1;

            // ---- Next step card (bottom left) ----
            coachPanel = UIFactory.Panel(transform, "Next Step", UITheme.Panel).rectTransform;
            UIFactory.Anchor(coachPanel, Vector2.zero, Vector2.zero, new Vector2(10, panelBottom), new Vector2(phone ? 640 : 560, panelBottom + 220));
            UIFactory.Vertical(coachPanel, 8, 16);
            UIFactory.Header(coachPanel, "Next step");
            coachText = UIFactory.Label(coachPanel, "", 19, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Size(coachText, 70);
            coachButton = UIFactory.Button(coachPanel, "", DoStep, 22, 66, UITheme.AccentDim);

            // ---- Bottom bar ----
            bottomBar = UIFactory.Panel(transform, "Bottom Bar", UITheme.Background, false).rectTransform;
            UIFactory.Anchor(bottomBar, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 90));
            var bh = UIFactory.Horizontal(bottomBar, 8, 10);
            bh.childForceExpandWidth = false;
            camButton = UIFactory.Button(bottomBar, "Camera: Orchestra", NextCamera, 19, 70);
            UIFactory.Size(camButton, -1, 260);
            statusText = UIFactory.Label(bottomBar, "", 20, TextAnchor.MiddleCenter, UITheme.TextDim);
            UIFactory.Size(statusText, -1, 200, 1);
            stopButton = UIFactory.Button(bottomBar, "Stop", () => gm.StopPerformance(), 20, 70, new Color(0.45f, 0.16f, 0.14f));
            UIFactory.Size(stopButton, -1, 120);
            var hide = UIFactory.Button(bottomBar, "Hide UI", () => SetHidden(true), 20, 70);
            UIFactory.Size(hide, -1, 140);
            progressFill = UIFactory.Bar(bottomBar, 6, UITheme.Accent, 0f);
            var pb = (RectTransform)progressFill.parent;
            pb.GetComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.Anchor(pb, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero);

            showUiButton = UIFactory.Button(transform, "Show UI", () => SetHidden(false), 20, 64);
            var sr = (RectTransform)showUiButton.transform;
            UIFactory.Anchor(sr, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 16), new Vector2(-16, 80));
            showUiButton.gameObject.SetActive(false);

            gm.StateChanged += Refresh;
            gm.SelectionChanged += () => { if (currentTab == HudTab.None && !PanelsAside) currentTab = HudTab.Musician; Refresh(); };   // tap on stage opens the player
            gm.Cam.ModeChanged += m => UpdateCamButton();
        }

        void AddNav(HudTab tab, string label)
        {
            var b = Nav(label, () => Toggle(tab), UIManager.IsPhone ? 150 : 160);
            navButtons[tab] = b;
        }

        Button Nav(string label, UnityEngine.Events.UnityAction a, float width)
        {
            var b = UIFactory.Button(topBar, label, a, 20, 60);
            UIFactory.Size(b, 60, width);
            return b;
        }

        void NextCamera()
        {
            int i = System.Array.IndexOf(CamOrder, gm.Cam.Mode);
            gm.Cam.SetMode(CamOrder[(i + 1) % CamOrder.Length]);
        }

        void UpdateCamButton()
        {
            var m = gm.Cam.Mode;
            UIFactory.SetLabel(camButton, "Camera: " + (m == CameraMode.CloseUp ? "Close-up" : m == CameraMode.MenuOrbit ? "Orchestra" : m.ToString()));
        }

        void Toggle(HudTab tab)
        {
            if (PanelsAside) { peekDuringConcert = true; currentTab = tab; Refresh(); return; }
            Open(currentTab == tab ? HudTab.None : tab);
        }

        public void Open(HudTab tab)
        {
            SetHidden(false);
            currentTab = tab;
            Refresh();
        }

        void SetHidden(bool hidden)
        {
            uiHidden = hidden;
            topBar.gameObject.SetActive(!hidden);
            bottomBar.gameObject.SetActive(!hidden);
            showUiButton.gameObject.SetActive(hidden);
            Refresh();
        }

        public void Refresh()
        {
            if (!gameObject.activeInHierarchy || gm.State == null) return;
            rightPanel.gameObject.SetActive(currentTab != HudTab.None && !uiHidden && !PanelsAside);
            foreach (var kv in navButtons) UIFactory.SetSelected(kv.Value, kv.Key == currentTab);
            UpdateCamButton();
            shownStep = null;
            if (rightPanel.gameObject.activeSelf) BuildTab();
        }

        void BuildTab()
        {
            UIFactory.Clear(tabContent);
            switch (currentTab)
            {
                case HudTab.Musician: tabTitle.text = "Orchestra"; MusicianTab.Build(tabContent, gm, ui); break;
                case HudTab.Music: tabTitle.text = "Concerts"; MusicTab.Build(tabContent, gm, ui); break;
                case HudTab.Auditions: tabTitle.text = "Recruit"; AuditionTab.Build(tabContent, gm, ui); break;
            }
        }

        // ---- Next step card ----
        void UpdateCoach()
        {
            bool show = !uiHidden && !gm.Performance.IsBusy && !(UIManager.IsPhone && currentTab != HudTab.None);
            coachPanel.gameObject.SetActive(show);
            if (!show) return;
            step = NextStepAdvisor.Get(gm.State, MusicLibrary.Get(MusicTab.PieceId));
            if (step.Text == shownStep) return;
            shownStep = step.Text;
            coachText.text = step.Text;
            UIFactory.SetLabel(coachButton, step.Label);
        }

        void DoStep()
        {
            string piece = MusicTab.PieceId;
            switch (step.Action)
            {
                case NextStepAction.Listen: gm.State.heardOrchestra = true; gm.Save(); gm.Listen(piece, MusicTab.Mode); break;
                case NextStepAction.Rehearse: gm.QuickRehearsal(piece); break;
                case NextStepAction.Concert: gm.PerformConcert(piece, MusicTab.Mode); break;
                case NextStepAction.Recruit: Open(HudTab.Auditions); break;
                default: gm.AdvanceDays(1); break;
            }
            shownStep = null;
        }

        void Update()
        {
            if (gm == null || gm.State == null) return;
            var perf = gm.Performance;
            bool concert = perf.IsBusy && perf.Context == PerformanceContext.Concert;
            if (concert != concertOnStage) { concertOnStage = concert; peekDuringConcert = false; Refresh(); }
            stopButton.gameObject.SetActive(perf.IsBusy);
            ((RectTransform)progressFill.parent).gameObject.SetActive(perf.IsBusy);
            if (perf.IsBusy)
            {
                statusText.text = "<color=#" + UITheme.Hex(UITheme.Accent) + ">" + perf.Status + "</color>";
                UIFactory.SetBar(progressFill, perf.Progress);
            }
            else
            {
                var s = gm.State;
                float avg = 0f; foreach (var m in s.roster) avg += m.Overall;
                avg = s.roster.Count > 0 ? avg / s.roster.Count : 0f;
                statusText.text = "Day " + s.day + "   ·   Reputation " + s.reputation.ToString("0") + "   ·   Orchestra level " + avg.ToString("0") + " (" + SkillLevels.TierName(avg) + ")";
            }
            if (uiHidden && InputHelper.KeyDown(GameKey.Escape)) SetHidden(false);
            UpdateCoach();
            UpdateFreeArea();
        }

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>Tells the camera which part of the screen the side panel leaves free, so the stage stays centred in it.</summary>
        void UpdateFreeArea()
        {
            float r = Screen.width;
            if (rightPanel.gameObject.activeInHierarchy) { rightPanel.GetWorldCorners(corners); r = corners[0].x; }
            gm.Cam.FreeLeft = 0f;
            gm.Cam.FreeRight = r / Screen.width;
        }

        void OnDisable()
        {
            if (gm != null && gm.Cam != null) { gm.Cam.FreeLeft = 0f; gm.Cam.FreeRight = 1f; }
        }
    }
}
