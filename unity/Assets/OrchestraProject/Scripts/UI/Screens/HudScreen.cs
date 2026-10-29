using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    public enum HudTab { None, Roster, Musician, Music, Practice, Auditions }

    /// <summary>
    /// In-game interface: navigation bar on top, roster on the left, the active tab on the right,
    /// camera controls and performance status at the bottom, and an optional "Next step" card. "Hide UI" leaves only the stage.
    /// </summary>
    public class HudScreen : MonoBehaviour
    {
        UIManager ui;
        GameManager gm;
        RectTransform topBar, rosterPanel, rightPanel, bottomBar, rosterContent, tabContent, coachPanel;
        Text coachText;
        Button coachButton;
        NextStep step;
        string shownStep;
        const float CoachHeight = 220f;
        Text tabTitle, statusText;
        RectTransform progressFill;
        Button stopButton, showUiButton;
        readonly Dictionary<HudTab, Button> navButtons = new Dictionary<HudTab, Button>();
        readonly Dictionary<CameraMode, Button> camButtons = new Dictionary<CameraMode, Button>();
        HudTab currentTab = HudTab.None;
        bool rosterOpen;
        bool uiHidden;
        bool concertOnStage, peekDuringConcert;   // panels step aside during a concert; any tab brings them back

        bool PanelsAside { get { return concertOnStage && !peekDuringConcert; } }

        public readonly MusicianTab MusicianTab = new MusicianTab();
        public readonly MusicTab MusicTab = new MusicTab();
        public readonly PracticeTab PracticeTab = new PracticeTab();
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
            AddNav(HudTab.Roster, "Roster");
            AddNav(HudTab.Musician, "Musician");
            AddNav(HudTab.Music, "Music & Concerts");
            AddNav(HudTab.Practice, "Practice");
            AddNav(HudTab.Auditions, "Auditions");
            var spacer = UIFactory.Rect("Spacer", topBar); UIFactory.Size(spacer, -1, 0, 1);
            Nav("Next Day", () => gm.AdvanceDays(1), phone ? 130 : 150);
            Nav("+7 Days", () => gm.AdvanceDays(7), phone ? 120 : 140);
            Nav("?", () => ui.Tutorial.Show(), 64);
            Nav("Menu", () => ui.ShowMenu(), phone ? 100 : 120);

            // ---- Roster (left) ----
            rosterPanel = UIFactory.Panel(transform, "Roster Panel", UITheme.Panel).rectTransform;
            UIFactory.Anchor(rosterPanel, new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, panelBottom), new Vector2(phone ? 420 : 400, -panelTop));
            UIFactory.Vertical(rosterPanel, 6, 12);
            var rosterHeader = UIFactory.Row(rosterPanel, 40);
            UIFactory.Header(rosterHeader, "Orchestra");
            ScrollRect rs;
            rosterContent = UIFactory.ScrollArea(rosterPanel, out rs);
            UIFactory.Size(rs, -1, -1, 1).flexibleHeight = 1;

            // ---- Next step card (bottom left, under the roster): an optional hint with one button ----
            coachPanel = UIFactory.Panel(transform, "Next Step", UITheme.Panel).rectTransform;
            UIFactory.Anchor(coachPanel, Vector2.zero, Vector2.zero, new Vector2(10, panelBottom), new Vector2(phone ? 640 : 400, panelBottom + CoachHeight - 10));
            UIFactory.Vertical(coachPanel, 8, 14);
            UIFactory.Header(coachPanel, "Next step");
            coachText = UIFactory.Label(coachPanel, "", 18, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Size(coachText, 80);
            coachButton = UIFactory.Button(coachPanel, "", DoStep, 21, 60, UITheme.AccentDim);
            coachPanel.gameObject.SetActive(false);

            // ---- Active tab (right) ----
            rightPanel = UIFactory.Panel(transform, "Tab Panel", UITheme.Panel).rectTransform;
            float w = phone ? 660 : 660;
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

            // ---- Bottom bar ----
            bottomBar = UIFactory.Panel(transform, "Bottom Bar", UITheme.Background, false).rectTransform;
            UIFactory.Anchor(bottomBar, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 90));
            var bh = UIFactory.Horizontal(bottomBar, 8, 10);
            bh.childForceExpandWidth = false;
            AddCam(CameraMode.Stage, "Stage");
            AddCam(CameraMode.Audience, "Audience");
            AddCam(CameraMode.Orchestra, "Orchestra");
            AddCam(CameraMode.CloseUp, "Close-up");
            AddCam(CameraMode.Free, "Free");
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
            gm.SelectionChanged += Refresh;
            gm.Cam.ModeChanged += m => UpdateCamButtons();
            rosterOpen = !phone;
        }

        void AddNav(HudTab tab, string label)
        {
            var b = Nav(label, () => Toggle(tab), UIManager.IsPhone ? (tab == HudTab.Music ? 190 : 140) : (tab == HudTab.Music ? 210 : 150));
            navButtons[tab] = b;
        }

        Button Nav(string label, UnityEngine.Events.UnityAction a, float width)
        {
            var b = UIFactory.Button(topBar, label, a, 20, 60);
            UIFactory.Size(b, 60, width);
            return b;
        }

        void AddCam(CameraMode mode, string label)
        {
            var b = UIFactory.Button(bottomBar, label, () => gm.Cam.SetMode(mode), 19, 70);
            UIFactory.Size(b, -1, UIManager.IsPhone ? 118 : 130);
            camButtons[mode] = b;
        }

        void Toggle(HudTab tab)
        {
            if (PanelsAside)
            {
                peekDuringConcert = true;
                if (tab == HudTab.Roster) rosterOpen = true; else currentTab = tab;
                Refresh();
                return;
            }
            if (tab == HudTab.Roster)
            {
                rosterOpen = !rosterOpen;
                if (rosterOpen && UIManager.IsPhone) currentTab = HudTab.None;   // one panel at a time on phones
                Refresh();
                return;
            }
            Open(currentTab == tab ? HudTab.None : tab);
        }

        public void Open(HudTab tab)
        {
            SetHidden(false);
            if (tab == HudTab.Roster) { rosterOpen = true; tab = currentTab; }
            currentTab = tab;
            if (tab != HudTab.None && UIManager.IsPhone) rosterOpen = false;
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
            rosterPanel.gameObject.SetActive(rosterOpen && !uiHidden && !PanelsAside);
            rightPanel.gameObject.SetActive(currentTab != HudTab.None && !uiHidden && !PanelsAside);
            foreach (var kv in navButtons)
                UIFactory.SetSelected(kv.Value, kv.Key == HudTab.Roster ? rosterOpen : kv.Key == currentTab);
            UpdateCamButtons();
            shownStep = null;
            if (rosterPanel.gameObject.activeSelf) BuildRoster();
            if (rightPanel.gameObject.activeSelf) BuildTab();
        }

        void UpdateCamButtons()
        {
            foreach (var kv in camButtons) UIFactory.SetSelected(kv.Value, kv.Key == gm.Cam.Mode);
        }

        void BuildRoster()
        {
            UIFactory.Clear(rosterContent);
            var s = gm.State;
            var list = new List<MusicianData>(s.roster);
            list.Sort((a, b) => a.Instrument.SeatingOrder != b.Instrument.SeatingOrder
                ? a.Instrument.SeatingOrder.CompareTo(b.Instrument.SeatingOrder) : b.Overall.CompareTo(a.Overall));
            foreach (var m in list)
            {
                var mm = m;
                var b = UIFactory.Button(rosterContent, "", () => { gm.Select(mm.id); if (currentTab == HudTab.None || currentTab == HudTab.Auditions) Open(HudTab.Musician); }, 20, 92);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.text = "<b>" + m.FullName + "</b>\n<size=17><color=#" + UITheme.Hex(UITheme.Accent) + ">" + m.Instrument.PlayerTitle + "</color>  ·  "
                         + m.LevelName + " " + m.Overall.ToString("0") + "  ·  Energy " + m.energy.ToString("0") + "</size>";
                UIFactory.SetSelected(b, m.id == gm.SelectedId);
            }
            var info = UIFactory.Label(rosterContent, s.roster.Count + " / " + OrchestraState.MaxRosterSize + " musicians.  Tap a musician on stage or in this list to select.", 16, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(info, 50);
        }

        void BuildTab()
        {
            UIFactory.Clear(tabContent);
            switch (currentTab)
            {
                case HudTab.Musician: tabTitle.text = "Musician"; MusicianTab.Build(tabContent, gm, ui); break;
                case HudTab.Music: tabTitle.text = "Music & Concerts"; MusicTab.Build(tabContent, gm, ui); break;
                case HudTab.Practice: tabTitle.text = "Practice"; PracticeTab.Build(tabContent, gm, ui); break;
                case HudTab.Auditions: tabTitle.text = "Auditions & Invitations"; AuditionTab.Build(tabContent, gm, ui); break;
            }
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

        // ---- Next step card ----
        void UpdateCoach()
        {
            bool phone = UIManager.IsPhone;
            bool show = !uiHidden && !gm.Performance.IsBusy && !PanelsAside && !(phone && (rosterOpen || currentTab != HudTab.None));
            if (coachPanel.gameObject.activeSelf != show)
            {
                coachPanel.gameObject.SetActive(show);
                // The roster panel makes room for the card.
                var om = rosterPanel.offsetMin; om.y = show && !phone ? 100f + CoachHeight : 100f; rosterPanel.offsetMin = om;
            }
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

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>Tells the camera which part of the screen the side panels leave free, so the stage stays centred in it.</summary>
        void UpdateFreeArea()
        {
            float l = 0f, r = Screen.width;
            if (rosterPanel.gameObject.activeInHierarchy) { rosterPanel.GetWorldCorners(corners); l = corners[2].x; }
            if (rightPanel.gameObject.activeInHierarchy) { rightPanel.GetWorldCorners(corners); r = corners[0].x; }
            gm.Cam.FreeLeft = l / Screen.width;
            gm.Cam.FreeRight = r / Screen.width;
        }

        void OnDisable()
        {
            if (gm != null && gm.Cam != null) { gm.Cam.FreeLeft = 0f; gm.Cam.FreeRight = 1f; }
        }
    }
}
