#nullable enable
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Headless first-open helper. Run via
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.FirstOpenSetup.CreateFloodScene -quit
    /// Creates the empty Milestone-1 scene (the bootstrap builds everything else at Play)
    /// and registers it in Build Settings so device builds have a scene 0.
    /// Idempotent: safe to rerun.
    /// </summary>
    public static class FirstOpenSetup
    {
        private const string ScenePath = "Assets/Scenes/Flood.unity";

        [MenuItem("Cipher/Create Flood Scene")]
        public static void CreateFloodScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);

            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"[Cipher] Created {ScenePath}");
            }

            var scenes = EditorBuildSettings.scenes;
            bool registered = false;
            foreach (var s in scenes) if (s.path == ScenePath) registered = true;
            if (!registered)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
                {
                    new EditorBuildSettingsScene(ScenePath, true)
                };
                EditorBuildSettings.scenes = list.ToArray();
                Debug.Log("[Cipher] Registered Flood scene in Build Settings");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
