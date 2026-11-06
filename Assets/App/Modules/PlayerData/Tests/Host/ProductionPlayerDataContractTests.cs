using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using Biome.LocalStore;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Fakes;
using Biome.PlayerData.Abstract;
using Biome.PlayerData.Data;
using Biome.PlayerData.Editor.Tests;
using Biome.PlayerData.Events;
using NUnit.Framework;

namespace Biome.PlayerData.Tests.Host
{
    /// <summary>
    /// Binds the package's store-free contract suite to the production service over LocalStore, and keeps the
    /// store-backed cases beside it: the first-launch snapshot read on start, the one-shot write, and the failed-read
    /// policy.
    /// </summary>
    [TestFixture]
    public sealed class ProductionPlayerDataContractTests : PlayerDataContractTests
    {
        [Test]
        public void MarkLaunched_EmptyStore_NextServiceIsNotFirstLaunch()
        {
            ILocalStoreService store = InMemoryLocalStore.Create(Registry());
            IPlayerDataService first = Create(store);
            Assert.IsTrue(first.IsFirstLaunch);

            first.MarkLaunched();

            Assert.IsFalse(Create(store).IsFirstLaunch);
        }

        [Test]
        public void IsFirstLaunch_CorruptRecord_IsTrue()
        {
            Assert.IsTrue(Create(new StatusStore(ReadStatus.Corrupt)).IsFirstLaunch);
        }

        [Test]
        public void IsFirstLaunch_VersionMismatchRecord_IsTrue()
        {
            Assert.IsTrue(Create(new StatusStore(ReadStatus.VersionMismatch)).IsFirstLaunch);
        }

        [Test]
        public void MarkLaunched_CalledTwice_WritesAndFlushesOnce()
        {
            var store = new StatusStore(ReadStatus.Missing);
            IPlayerDataService service = Create(store);

            service.MarkLaunched();
            service.MarkLaunched();

            Assert.AreEqual(1, store.SetCount);
            Assert.AreEqual(1, store.FlushCount);
        }

        [Test]
        public void MarkLaunched_EmptyStore_PublishesChangedOnce()
        {
            var bus = new RecordingEventBus();
            IPlayerDataService service = Create(InMemoryLocalStore.Create(Registry()), bus);

            service.MarkLaunched();

            Assert.AreEqual(1, bus.Count<PlayerDataChangedEvent>());
        }

        [Test]
        public void MarkLaunched_CalledTwice_PublishesChangedOnce()
        {
            var bus = new RecordingEventBus();
            IPlayerDataService service = Create(InMemoryLocalStore.Create(Registry()), bus);

            service.MarkLaunched();
            service.MarkLaunched();

            Assert.AreEqual(1, bus.Count<PlayerDataChangedEvent>());
        }

        [Test]
        public void MarkLaunched_AlreadyLaunchedStore_PublishesNothing()
        {
            ILocalStoreService store = InMemoryLocalStore.Create(Registry());
            Create(store).MarkLaunched();
            var bus = new RecordingEventBus();
            IPlayerDataService service = Create(store, bus);

            service.MarkLaunched();

            Assert.AreEqual(0, bus.Count<PlayerDataChangedEvent>());
        }

        [Test]
        public void MarkLaunched_CorruptRecord_PublishesChangedOnce()
        {
            var bus = new RecordingEventBus();
            IPlayerDataService service = Create(new StatusStore(ReadStatus.Corrupt), bus);

            service.MarkLaunched();

            Assert.AreEqual(1, bus.Count<PlayerDataChangedEvent>());
        }

        [Test]
        public void Entry_PlayerDataRecord_ResolvesToOneKeyAtVersionOne()
        {
            bool resolved = Registry().TryResolve(typeof(PlayerDataRecord), out LocalStoreEntry entry);

            Assert.IsTrue(resolved);
            Assert.AreEqual(PlayerDataStoreEntry.Entry, entry);
            Assert.AreEqual(1, entry.Version);
            Assert.IsNotEmpty(entry.Key);
        }

        protected override IPlayerDataService CreateFresh()
        {
            return Create(InMemoryLocalStore.Create(Registry()));
        }

        private static IPlayerDataService Create(ILocalStoreService store)
        {
            return Create(store, new RecordingEventBus());
        }

        private static IPlayerDataService Create(ILocalStoreService store, IEventPublisher events)
        {
            return new PlayerDataService(store, events);
        }

        private static ILocalStoreRegistry Registry()
        {
            return new DictionaryLocalStoreRegistry()
                .Add<PlayerDataRecord>(PlayerDataStoreEntry.Key, PlayerDataStoreEntry.Version);
        }

        /// <summary>
        /// A test-local store double for the read failures the shipped fake cannot seed: every read returns the given
        /// status, and the writes are counted.
        /// </summary>
        private sealed class StatusStore : ILocalStoreService
        {
            private readonly ReadStatus _status;

            public StatusStore(ReadStatus status)
            {
                _status = status;
            }

            public int SetCount { get; private set; }

            public int FlushCount { get; private set; }

            public bool TryGet<T>(out T value, out ReadStatus status)
            {
                value = default;
                status = _status;
                return false;
            }

            public void Set<T>(T value)
            {
                SetCount++;
            }

            public bool Has<T>()
            {
                return false;
            }

            public void Remove<T>()
            {
            }

            public void Clear()
            {
            }

            public void Flush()
            {
                FlushCount++;
            }
        }
    }
}
