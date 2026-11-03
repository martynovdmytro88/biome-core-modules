using Biome.PlayerData;
using Biome.PlayerData.Abstract;
using NUnit.Framework;
using VContainer;

namespace Biome.App.Tests
{
    /// <summary>
    /// The production profile wires PlayerData: the real start path has raised the default profile, so resolving the
    /// service proves the registry entry (the service reads its record through the real LocalStore) and the event
    /// publisher from EventBusInstaller. Read-only: nothing here marks a launch.
    /// </summary>
    public sealed class AppPlayerDataWiringTests
    {
        [Test]
        public void Prod_PlayerDataService_Resolves()
        {
            var container = AppCompositionRoot.Active.Container;

            var service = container.Resolve<IPlayerDataService>();

            Assert.That(service, Is.Not.Null);
            Assert.That(service, Is.InstanceOf<PlayerDataService>());
            Assert.That(container.Resolve<IPlayerDataService>(), Is.SameAs(service), "one service per root scope");
        }
    }
}
