using Biome.PlayerData.Abstract;
using VContainer;
using VContainer.Unity;

namespace Biome.PlayerData.Installers
{
    /// <summary>
    /// One <see cref="IPlayerDataService"/> per scope, built by constructor injection over the scope's
    /// <c>ILocalStoreService</c>. The store itself is registered by LocalStore's installer.
    /// </summary>
    public sealed class PlayerDataInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<PlayerDataService>(Lifetime.Singleton).As<IPlayerDataService>();
        }
    }
}
