using Biome.PlayerData.Abstract;
using Biome.PlayerData.Fakes;
using VContainer;
using VContainer.Unity;

namespace Biome.PlayerData.Installers.Mock
{
    /// <summary>
    /// One <see cref="IPlayerDataService"/> per scope over memory (<see cref="InMemoryPlayerDataService"/>). The
    /// mock-profile twin of the production installer: the same contract, no store.
    /// </summary>
    public sealed class PlayerDataMockInstaller : IInstaller
    {
        private readonly bool _hasLaunched;

        public PlayerDataMockInstaller(bool hasLaunched = false)
        {
            _hasLaunched = hasLaunched;
        }

        public void Install(IContainerBuilder builder)
        {
            var hasLaunched = _hasLaunched;
            builder.Register<IPlayerDataService>(_ => new InMemoryPlayerDataService(hasLaunched), Lifetime.Singleton);
        }
    }
}
