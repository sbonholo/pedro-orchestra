using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>
    /// Creates the canvas, event system and every screen. Scales the interface per device so
    /// buttons stay comfortable to tap on iPhone and iPad.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static bool IsPhone { get; private set; }
        public static bool IsTouchDevice { get; private set; }

        public RectTransform Root { get; private set; }
        public HudScreen Hud { get; private set; }
        public MainMenuScreen Menu { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public RhythmGameScreen RhythmScreen { get; private set; }
        public ResultScreen Results { get; private set; }
        public Toast Toast { get; private set; }
        public TutorialScreen Tutorial { get; private set; }

        public void Build(GameManager gm)
        {
            DetectDevice();

            var canvasGo = new GameObject("UI Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = IsPhone ? new Vector2(1280, 720) : IsTouchDevice ? new Vector2(1600, 1000) : new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem").AddComponent<EventSystem>();
                es.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
                var module = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                module.AssignDefaultActions();
#else
                es.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }

            Root = UIFactory.Stretch(UIFactory.Rect("Safe Area", canvasGo.transform));
            Root.gameObject.AddComponent<SafeArea>();

            Hud = UIFactory.Stretch(UIFactory.Rect("HUD", Root)).gameObject.AddComponent<HudScreen>();
            Hud.Build(this, gm);
            Results = UIFactory.Stretch(UIFactory.Rect("Results", Root)).gameObject.AddComponent<ResultScreen>();
            Results.Build(this, gm);
            RhythmScreen = UIFactory.Stretch(UIFactory.Rect("Rhythm Practice", Root)).gameObject.AddComponent<RhythmGameScreen>();
            RhythmScreen.Build(this, gm);
            Menu = UIFactory.Stretch(UIFactory.Rect("Main Menu", Root)).gameObject.AddComponent<MainMenuScreen>();
            Menu.Build(this, gm);
            Settings = UIFactory.Stretch(UIFactory.Rect("Settings", Root)).gameObject.AddComponent<SettingsScreen>();
            Settings.Build(this, gm);
            Tutorial = UIFactory.Stretch(UIFactory.Rect("Tutorial", Root)).gameObject.AddComponent<TutorialScreen>();
            Tutorial.Build();
            Toast = UIFactory.Rect("Toast", Root).gameObject.AddComponent<Toast>();
            Toast.Build();

            gm.Message += msg => Toast.Show(msg);
            gm.PerformanceFinished += (result, gains) => Results.Show(result, gains);

            ShowMenu();
        }

        static void DetectDevice()
        {
            IsTouchDevice = Application.isMobilePlatform;
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float inches = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height) / dpi;
            IsPhone = IsTouchDevice && inches < 7.5f;
        }

        public void ShowMenu()
        {
            Hud.gameObject.SetActive(false);
            Menu.Show();
        }

        public void ShowGame(HudTab tab)
        {
            Menu.gameObject.SetActive(false);
            Hud.gameObject.SetActive(true);
            Hud.Open(tab);
        }
    }

    /// <summary>Short message at the top of the screen that fades out.</summary>
    public class Toast : MonoBehaviour
    {
        Text text;
        CanvasGroup group;
        float hideAt;

        public void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -110);
            rt.sizeDelta = new Vector2(760, 10);
            var bg = gameObject.AddComponent<Image>();
            bg.sprite = UITheme.Rounded; bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.07f, 0.06f, 0.95f);
            bg.raycastTarget = false;
            UIFactory.Vertical(bg, 0, 16);
            var fit = gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            text = UIFactory.Label(transform, "", 21, TextAnchor.MiddleCenter);
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0f;
        }

        public void Show(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            text.text = msg;
            transform.SetAsLastSibling();
            int lines = msg.Split('\n').Length;
            hideAt = Time.unscaledTime + 3f + lines * 0.6f;
            group.alpha = 1f;
        }

        void Update()
        {
            if (group.alpha <= 0f) return;
            if (Time.unscaledTime > hideAt) group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 2f);
        }
    }
}
