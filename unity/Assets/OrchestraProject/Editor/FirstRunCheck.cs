using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OrchestraProject.EditorTools
{
    /// <summary>
    /// First import: checks the project setup and offers to create and open the game scene,
    /// so a new user only has to press Play. Also available as "Orchestra Project > Check Setup".
    /// </summary>
    [InitializeOnLoad]
    public static class FirstRunCheck
    {
        const string ScenePath = "Assets/OrchestraProject/Scenes/Orchestra.unity";
        const string AskedKey = "OrchestraProject.FirstRunAsked";

        static FirstRunCheck()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AskedKey, false)) return;
                SessionState.SetBool(AskedKey, true);
                if (File.Exists(ScenePath)) return;
                string problems = Problems();
                if (problems.Length > 0) { Debug.LogWarning("[Orchestra Project] Setup problems:\n" + problems); return; }
                if (EditorUtility.DisplayDialog("Orchestra Project",
                        "Welcome! Create the game scene now?\n\nAfterwards just press Play (the ▶ button at the top).",
                        "Create Game Scene", "Later"))
                    OrchestraSetup.CreateScene();
            };
        }

        [MenuItem("Orchestra Project/Check Setup", priority = 1)]
        public static void CheckSetup()
        {
            string problems = Problems();
            if (problems.Length == 0)
            {
                bool scene = File.Exists(ScenePath);
                if (scene && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath &&
                    EditorUtility.DisplayDialog("Orchestra Project", "Setup is OK.\n\nOpen the game scene?", "Open", "Cancel"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
                }
                else if (!scene) OrchestraSetup.CreateScene();
                else EditorUtility.DisplayDialog("Orchestra Project", "Setup is OK. Press Play.", "OK");
                return;
            }
            Debug.LogWarning("[Orchestra Project] Setup problems:\n" + problems);
            EditorUtility.DisplayDialog("Orchestra Project: fix these first", problems, "OK");
        }

        static string Problems()
        {
            var sb = new StringBuilder();
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                sb.AppendLine("• The project does not use URP. Create a new project from the \"Universal 3D\" template and copy Assets/OrchestraProject into it again.");
            if (Resources.Load<TextAsset>("OrchestraSamples/Violin/samples") == null)
                sb.AppendLine("• Recorded samples are missing. Copy the whole folder Assets/OrchestraProject, including Resources/OrchestraSamples.");
            return sb.ToString();
        }
    }
}
