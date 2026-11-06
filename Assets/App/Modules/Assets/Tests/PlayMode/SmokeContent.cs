using Biome.Assets.Abstract;
using UnityEngine.SceneManagement;

namespace Biome.Assets.Tests.PlayMode
{
    /// <summary>
    /// The smoke content the prebuild step makes addressable (<c>Biome.Assets.Tests.Editor.AddressablesTestContent</c>):
    /// its keys, its label and the count of open smoke scenes.
    /// </summary>
    internal static class SmokeContent
    {
        public const string PrebuildStep = "Biome.Assets.Tests.Editor.AddressablesTestContent";

        private const string SceneName = "smoke-scene";

        public static AssetKey KeyA { get; } = new AssetKey("assets-tests/a");

        public static AssetKey KeyB { get; } = new AssetKey("assets-tests/b");

        public static AssetLabel Label { get; } = new AssetLabel("assets-tests");

        public static AssetKey SceneKey { get; } = new AssetKey("assets-tests/scene");

        public static int LiveSceneCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.name == SceneName)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
