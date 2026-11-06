using Biome.Assets.Abstract;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Assets.Tests.PlayMode
{
    /// <summary>
    /// Binds the package's backend seam contract to the production Addressables backend, in Use Asset Database mode, over
    /// the smoke content the prebuild step makes addressable.
    /// </summary>
    [TestFixture]
    [PrebuildSetup(SmokeContent.PrebuildStep)]
    [PostBuildCleanup(SmokeContent.PrebuildStep)]
    public sealed class AddressablesBackendContractTests : AssetBackendContractTests
    {
        protected override AssetKey KeyA => SmokeContent.KeyA;

        protected override AssetKey KeyB => SmokeContent.KeyB;

        protected override AssetLabel Label => SmokeContent.Label;

        protected override AssetKey SceneKey => SmokeContent.SceneKey;

        protected override int LiveSceneCount => SmokeContent.LiveSceneCount;

        protected override IAssetBackend CreateBackend() => new AddressablesBackend();
    }
}
