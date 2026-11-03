using Biome.LocalStore.Abstract;
using Biome.LocalStore.Fakes;
using VContainer;
using VContainer.Unity;

namespace Biome.LocalStore.Installers.Mock
{
    /// <summary>
    /// One <see cref="ILocalStoreService"/> per scope over memory (<see cref="InMemoryLocalStore.Create"/>), built from the
    /// caller's registry. The mock-profile twin of the production installer: the same contract, no PlayerPrefs.
    /// </summary>
    public sealed class LocalStoreMockInstaller : IInstaller
    {
        private readonly ILocalStoreRegistry _registry;

        public LocalStoreMockInstaller(ILocalStoreRegistry registry)
        {
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
        }

        public void Install(IContainerBuilder builder)
        {
            var registry = _registry;
            builder.Register<ILocalStoreService>(_ => InMemoryLocalStore.Create(registry), Lifetime.Singleton);
        }
    }
}
