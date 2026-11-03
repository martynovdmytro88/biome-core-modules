using Biome.Assets.Abstract;

namespace Biome.Assets
{
    /// <summary>The public entry of the Runtime layer: builds the production service over Addressables.</summary>
    public static class AssetsFactory
    {
        /// <summary>One <see cref="IAssetsService"/> over Addressables; the returned instance is <see cref="System.IDisposable"/>.</summary>
        public static IAssetsService CreateAddressables() => new AssetsService(new AddressablesBackend());
    }
}
