using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using VContainer;
using VContainer.Unity;

namespace Biome.LocalStore.Installers
{
    /// <summary>
    /// One <see cref="ILocalStoreService"/> per scope over PlayerPrefs (<see cref="LocalStoreFactory.CreatePlayerPrefs"/>),
    /// built from the caller's registry and limits. A thin passthrough of <see cref="IContainerBuilder"/>.
    /// </summary>
    public sealed class LocalStoreInstaller : IInstaller
    {
        private readonly ILocalStoreRegistry _registry;
        private readonly LocalStoreLimits _limits;

        public LocalStoreInstaller(ILocalStoreRegistry registry, LocalStoreLimits limits)
        {
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _limits = limits;
        }

        public void Install(IContainerBuilder builder)
        {
            var registry = _registry;
            var limits = _limits;
            builder.Register<ILocalStoreService>(_ => LocalStoreFactory.CreatePlayerPrefs(registry, limits), Lifetime.Singleton);
        }
    }
}
