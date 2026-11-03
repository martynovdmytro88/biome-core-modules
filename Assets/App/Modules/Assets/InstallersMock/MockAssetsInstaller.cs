using System;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using VContainer;
using VContainer.Unity;

namespace Biome.Assets.Installers.Mock
{
    /// <summary>
    /// The Mock profile's twin of <see cref="AssetsInstaller"/>: the same service over the shipped fake backend. The
    /// caller keeps the <see cref="FakeAssetBackend"/> to register assets and read its counters.
    /// </summary>
    public sealed class MockAssetsInstaller : IInstaller
    {
        private readonly FakeAssetBackend _backend;

        public MockAssetsInstaller(FakeAssetBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public void Install(IContainerBuilder builder)
        {
            FakeAssetBackend backend = _backend;
            builder.Register<IAssetsService>(_ => FakeAssets.CreateService(backend), Lifetime.Singleton);
        }
    }
}
