using Biome.EventBus.Config;
using Biome.EventBus.Data;
using Biome.EventBus.Fakes;
using NUnit.Framework;

namespace Biome.EventBus.Editor.Tests
{
    /// <summary>The package-side defaults a bus is built from; the fake's loop guard follows the same default.</summary>
    [TestFixture]
    public sealed class EventBusConfigTests
    {
        [Test]
        public void Defaults_AreIsolateWithTracingOff()
        {
            var config = new EventBusConfig();

            Assert.That(config.ExceptionPolicy, Is.EqualTo(ExceptionPolicy.Isolate));
            Assert.That(config.MaxDispatchDepth, Is.EqualTo(16));
            Assert.That(config.InitialListenerCapacity, Is.EqualTo(4));
            Assert.That(config.TracingEnabled, Is.False);
        }

        [Test]
        public void FakeLoopGuard_MatchesTheDefaultDepth()
        {
            Assert.That(RecordingEventBus.MaxDispatchDepth, Is.EqualTo(new EventBusConfig().MaxDispatchDepth));
        }
    }
}
