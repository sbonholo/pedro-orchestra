using UnityEngine;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Title screen over a slowly orbiting view of the hall.</summary>
    public class MainMenuScreen : MonoBehaviour
    {
        UIManager ui;
        GameManager gm;
        Button continueButton, newGameButton;
        bool confirmNew;

        public void Build(UIManager uiManager, GameManager gameManager)
        {
            ui = uiManager; gm = gameManager;
            var shade = UIFactory.Panel(transform, "Shade", new Color(0.02f, 0.02f, 0.03f, 0.72f), false);
            UIFactory.Anchor(shade.rectTransform, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(UIManager.IsPhone ? 560 : 640, 0));

            var col = UIFactory.Rect("Column", shade.transform);
            UIFactory.Stretch(col, 56, 40, 56, 30);
            UIFactory.Vertical(col, UIManager.IsPhone ? 6 : 10, 0, TextAnchor.MiddleLeft);

            var title = UIFactory.Label(col, "ORCHESTRA\nPROJECT", UIManager.IsPhone ? 46 : 60, TextAnchor.LowerLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Size(title, UIManager.IsPhone ? 110 : 150);
            var sub = UIFactory.Label(col, "Build a world-class orchestra, one musician at a time.", 20, TextAnchor.UpperLeft, UITheme.TextDim);
            UIFactory.Size(sub, 40);

            float h = UIManager.IsPhone ? 50 : 58;
            continueButton = UIFactory.Button(col, "Continue", () =>
            {
                if (gm.InGame) { gm.Cam.SetMode(CameraMode.Orchestra); ui.ShowGame(HudTab.Musician); }
                else if (gm.Continue()) ui.ShowGame(HudTab.Musician);
                else gm.Notify("No saved game found.");
            }, 24, h, UITheme.AccentDim);
            newGameButton = UIFactory.Button(col, "New Game", NewGame, 24, h);
            UIFactory.Button(col, "How to Play", () => ui.Tutorial.Show(), 21, h);
            UIFactory.Button(col, "Orchestra Management", () => Enter(HudTab.Musician), 21, h);
            UIFactory.Button(col, "Music Library", () => Enter(HudTab.Music), 21, h);
            UIFactory.Button(col, "Practice", () => Enter(HudTab.Practice), 21, h);
            UIFactory.Button(col, "Auditions", () => Enter(HudTab.Auditions), 21, h);
            UIFactory.Button(col, "Concerts", () => Enter(HudTab.Music), 21, h);
            UIFactory.Button(col, "Settings", () => ui.Settings.Show(), 21, h);
            if (!Application.isMobilePlatform) UIFactory.Button(col, "Quit", Application.Quit, 21, h);

            var note = UIFactory.Label(transform, "Real string recordings (VSCO 2 CE, CC0)  ·  procedural 3D musicians", 15, TextAnchor.LowerRight, UITheme.TextDim);
            UIFactory.Anchor(note.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-700, 12), new Vector2(-16, 40));
        }

        public void Show()
        {
            gameObject.SetActive(true);
            confirmNew = false;
            UIFactory.SetLabel(newGameButton, "New Game");
            continueButton.interactable = gm.InGame || SaveSystem.HasSave;
            gm.Cam.SetMode(CameraMode.MenuOrbit);
        }

        void NewGame()
        {
            if ((gm.InGame || SaveSystem.HasSave) && !confirmNew)
            {
                confirmNew = true;
                UIFactory.SetLabel(newGameButton, "Tap again to overwrite the saved game");
                return;
            }
            gm.NewGame();
            ui.ShowGame(HudTab.Musician);
            if (!TutorialScreen.Seen) ui.Tutorial.Show();
        }

        void Enter(HudTab tab)
        {
            if (!gm.InGame && !gm.Continue()) gm.NewGame();
            else if (gm.InGame) gm.Cam.SetMode(CameraMode.Orchestra);
            ui.ShowGame(tab);
        }
    }
}
