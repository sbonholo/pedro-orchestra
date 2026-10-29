using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OrchestraProject
{
    /// <summary>
    /// The ONLY component you put in the scene. On Play it builds the hall, camera, systems and UI.
    /// Created automatically by the menu item "Orchestra Project > Create Game Scene".
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = Application.isMobilePlatform ? 60 : -1;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SettingsScreen.ApplySaved();

            foreach (var oldCam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (oldCam.GetComponent<CameraController>() == null) oldCam.gameObject.SetActive(false);

            var hall = new GameObject("Concert Hall").AddComponent<ConcertHallBuilder>();
            hall.Build();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.025f, 0.02f);
            camGo.AddComponent<AudioListener>();
            var urp = cam.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;
            urp.antialiasing = Application.isMobilePlatform ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var camController = camGo.AddComponent<CameraController>();

            var stage = new GameObject("Stage (Musicians)").AddComponent<StageManager>();
            var systems = new GameObject("Systems");
            var performance = systems.AddComponent<PerformanceController>();
            var rhythm = systems.AddComponent<RhythmMiniGame>();
            new GameObject("Conductor").AddComponent<ConductorView>().Build(performance);
            var gm = systems.AddComponent<GameManager>();
            gm.Init(stage, performance, camController, rhythm);

            var ui = new GameObject("UI").AddComponent<UIManager>();
            ui.Build(gm);
        }
    }
}
