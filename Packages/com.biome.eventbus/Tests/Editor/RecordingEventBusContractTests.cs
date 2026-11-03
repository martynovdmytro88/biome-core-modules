using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using NUnit.Framework;

namespace Biome.EventBus.Editor.Tests
{
    /// <summary>Binds the contract to the package's recording fake (fail-fast, so both hooks build the same bus).</summary>
    [TestFixture]
    public sealed class RecordingEventBusContractTests : EventBusContractTests
    {
        protected override IEventBus CreateBus() => new RecordingEventBus();

        protected override IEventBus CreateRethrowBus() => new RecordingEventBus();

        [Test]
        public void Fake_RecordsEveryPublishedValuePerType_UntilClear()
        {
            var bus = (RecordingEventBus)NewBus();
            bus.Publish(new TestDamageDealt(1));
            bus.Publish(new TestDamageDealt(2));
            bus.Publish(new TestUnitSpawned(9));

            Assert.That(bus.Count<TestDamageDealt>(), Is.EqualTo(2));
            Assert.That(bus.Published<TestDamageDealt>()[1].Amount, Is.EqualTo(2));
            Assert.That(bus.Published<TestUnitSpawned>()[0].UnitId, Is.EqualTo(9));
            Assert.That(bus.Count<TestSnapshotTaken>(), Is.EqualTo(0));

            var calls = 0;
            bus.Subscribe((in TestDamageDealt _) => calls++);
            bus.Clear();
            bus.Publish(new TestDamageDealt(3));

            Assert.That(bus.Count<TestDamageDealt>(), Is.EqualTo(1), "Clear forgets the records");
            Assert.That(calls, Is.EqualTo(1), "Clear keeps the subscriptions");
        }
    }
}
