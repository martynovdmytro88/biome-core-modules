using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biome.App.Editor
{
    /// <summary>
    /// Play mode in the editor starts from the boot scene (Build Settings index 0), whatever scene is
    /// open — the same start path as the player build. A PlayMode test run is left alone: the runner
    /// enters play mode from its own bootstrap scene, and replacing it hangs the run.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeStartSceneHook
    {
        // The test runner's bootstrap scene (com.unity.test-framework 1.7.0, CreateBootstrapSceneTask:
        // "Assets/InitTestScene" + Guid + ".unity"), active when the run enters play mode. Package-internal naming.
        private const string TestRunnerScenePrefix = "Assets/InitTestScene";

        static PlayModeStartSceneHook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                EditorSceneManager.playModeStartScene = IsTestRun() ? null : BootScene();
            }
        }

        private static bool IsTestRun() => SceneManager.GetActiveScene().path.StartsWith(TestRunnerScenePrefix);

        private static SceneAsset BootScene()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogWarning("PlayModeStartSceneHook: Build Settings lists no scene; play mode starts from the open scene.");
                return null;
            }

            var path = scenes[AppCompositionRoot.BootSceneBuildIndex].path;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (scene == null)
            {
                Debug.LogWarning($"PlayModeStartSceneHook: no scene at '{path}'; play mode starts from the open scene.");
            }

            return scene;
        }
    }
}
