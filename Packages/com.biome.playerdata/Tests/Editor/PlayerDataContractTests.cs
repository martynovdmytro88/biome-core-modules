using Biome.PlayerData.Abstract;
using NUnit.Framework;

namespace Biome.PlayerData.Editor.Tests
{
    /// <summary>
    /// The store-free contract of <see cref="IPlayerDataService"/>: the first-launch snapshot is fixed per instance. Every
    /// implementation, fake or production, runs these cases through its own fixture; a host binds its service by
    /// overriding <see cref="CreateFresh"/> and keeps its store-backed cases beside it.
    /// </summary>
    public abstract class PlayerDataContractTests
    {
        [Test]
        public void IsFirstLaunch_FreshService_IsTrue()
        {
            Assert.IsTrue(CreateFresh().IsFirstLaunch);
        }

        [Test]
        public void MarkLaunched_SameInstance_IsFirstLaunchStaysTrue()
        {
            IPlayerDataService service = CreateFresh();

            service.MarkLaunched();

            Assert.IsTrue(service.IsFirstLaunch);
        }

        [Test]
        public void MarkLaunched_CalledTwice_DoesNotThrow()
        {
            IPlayerDataService service = CreateFresh();

            service.MarkLaunched();

            Assert.DoesNotThrow(service.MarkLaunched);
        }

        /// <summary>An implementation with no launch recorded.</summary>
        protected abstract IPlayerDataService CreateFresh();
    }
}
