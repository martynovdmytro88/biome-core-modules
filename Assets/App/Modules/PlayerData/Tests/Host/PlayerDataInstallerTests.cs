using Biome.EventBus.Installers.Mock;
using Biome.LocalStore;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Installers.Mock;
using Biome.PlayerData.Abstract;
using Biome.PlayerData.Data;
using Biome.PlayerData.Fakes;
using Biome.PlayerData.Installers;
using Biome.PlayerData.Installers.Mock;
using NUnit.Framework;
using VContainer;

namespace Biome.PlayerData.Tests.Host
{
    /// <summary>The installers resolve the entry contract to the production service and to the fake.</summary>
    [TestFixture]
    public sealed class PlayerDataInstallerTests
    {
        [Test]
        public void Resolve_ProductionInstallerOverMemoryStore_ReturnsPlayerDataService()
        {
            ILocalStoreRegistry registry = new DictionaryLocalStoreRegistry()
                .Add<PlayerDataRecord>(PlayerDataStoreEntry.Key, PlayerDataStoreEntry.Version);
            var builder = new ContainerBuilder();
            new LocalStoreMockInstaller(registry).Install(builder);
            new RecordingEventBusInstaller().Install(builder);
            new PlayerDataInstaller().Install(builder);

            using (IObjectResolver container = builder.Build())
            {
                IPlayerDataService service = container.Resolve<IPlayerDataService>();

                Assert.IsInstanceOf<PlayerDataService>(service);
                Assert.AreSame(service, container.Resolve<IPlayerDataService>());
            }
        }

        [Test]
        public void Resolve_MockInstaller_ReturnsFake()
        {
            var builder = new ContainerBuilder();
            new PlayerDataMockInstaller().Install(builder);

            using (IObjectResolver container = builder.Build())
            {
                Assert.IsInstanceOf<InMemoryPlayerDataService>(container.Resolve<IPlayerDataService>());
            }
        }

        [Test]
        public void Resolve_MockInstallerHasLaunched_FakeIsNotFirstLaunch()
        {
            var builder = new ContainerBuilder();
            new PlayerDataMockInstaller(true).Install(builder);

            using (IObjectResolver container = builder.Build())
            {
                Assert.IsFalse(container.Resolve<IPlayerDataService>().IsFirstLaunch);
            }
        }
    }
}
