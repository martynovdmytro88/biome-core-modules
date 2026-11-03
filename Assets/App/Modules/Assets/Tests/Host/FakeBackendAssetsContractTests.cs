using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace Biome.Assets.Tests.Host
{
    /// <summary>Binds the package's service contract to the production store and contexts over the package's fake backend.</summary>
    [TestFixture]
    public sealed class FakeBackendAssetsContractTests : AssetsContractTests
    {
        private FakeAssetBackend _backend;

        protected override AssetKey KeyA { get; } = new AssetKey("fake/a");

        protected override AssetKey KeyB { get; } = new AssetKey("fake/b");

        protected override AssetLabel Label { get; } = new AssetLabel("fake-label");

        protected override AssetKey SceneKey { get; } = new AssetKey("fake/scene");

        protected override int LiveSceneCount => _backend.LiveSceneCount(SceneKey);

        protected override IAssetsService CreateService()
        {
            _backend = new FakeAssetBackend()
                .Add(KeyA, new TextAsset("a"), Label)
                .Add(KeyB, new TextAsset("b"), Label)
                .AddScene(SceneKey);
            return FakeAssets.CreateService(_backend);
        }

        protected override void HoldSceneLoads() => _backend.Gate(SceneKey);

        protected override void ReleaseSceneLoads() => _backend.Open(SceneKey);
    }
}
