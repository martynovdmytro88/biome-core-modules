using Biome.Assets.Abstract;

namespace Biome.Assets.Fakes
{
    /// <summary>Builds the module's shipped test double: the real store and contexts over a <see cref="FakeAssetBackend"/>.</summary>
    public static class FakeAssets
    {
        /// <summary>One <see cref="IAssetsService"/> over <paramref name="backend"/>; the instance is <see cref="System.IDisposable"/>.</summary>
        public static IAssetsService CreateService(FakeAssetBackend backend) => new AssetsService(backend);
    }
}
