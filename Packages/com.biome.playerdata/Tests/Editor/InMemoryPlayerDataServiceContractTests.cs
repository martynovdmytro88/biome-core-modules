using Biome.PlayerData.Abstract;
using Biome.PlayerData.Fakes;
using NUnit.Framework;

namespace Biome.PlayerData.Editor.Tests
{
    /// <summary>Binds the contract to the package's fake, plus the fake's own initial state and recording.</summary>
    [TestFixture]
    public sealed class InMemoryPlayerDataServiceContractTests : PlayerDataContractTests
    {
        [Test]
        public void IsFirstLaunch_HasLaunchedTrue_IsFalse()
        {
            Assert.IsFalse(new InMemoryPlayerDataService(true).IsFirstLaunch);
        }

        [Test]
        public void MarkLaunched_HasLaunchedTrue_IsFirstLaunchStaysFalse()
        {
            var service = new InMemoryPlayerDataService(true);

            service.MarkLaunched();

            Assert.IsFalse(service.IsFirstLaunch);
        }

        [Test]
        public void MarkLaunched_Called_IsLaunchMarkedBecomesTrue()
        {
            var service = new InMemoryPlayerDataService();
            Assert.IsFalse(service.IsLaunchMarked);

            service.MarkLaunched();

            Assert.IsTrue(service.IsLaunchMarked);
        }

        protected override IPlayerDataService CreateFresh() => new InMemoryPlayerDataService();
    }
}
