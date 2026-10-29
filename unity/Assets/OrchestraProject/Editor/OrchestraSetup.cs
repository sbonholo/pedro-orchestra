using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrchestraProject.EditorTools
{
    /// <summary>Editor menu: one click creates the game scene and adds it to Build Settings.</summary>
    public static class OrchestraSetup
    {
        const string ScenePath = "Assets/OrchestraProject/Scenes/Orchestra.unity";

        [MenuItem("Orchestra Project/Create Game Scene", priority = 0)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder("Assets/OrchestraProject/Scenes"))
                AssetDatabase.CreateFolder("Assets/OrchestraProject", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Orchestra Game").AddComponent<GameBootstrap>();

            // Store fog in the scene so builds keep the fog shader variants.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.075f, 0.06f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            EditorUtility.DisplayDialog("Orchestra Project",
                "Scene created at " + ScenePath + " and added to Build Settings.\n\nPress Play to start the game.", "OK");
        }

        [MenuItem("Orchestra Project/Open Save Folder", priority = 20)]
        public static void OpenSaveFolder() { EditorUtility.RevealInFinder(Application.persistentDataPath); }

        [MenuItem("Orchestra Project/Delete Save (start fresh)", priority = 21)]
        public static void DeleteSave()
        {
            if (EditorUtility.DisplayDialog("Delete save", "Delete the saved game?", "Delete", "Cancel")) SaveSystem.Delete();
        }
    }
}
