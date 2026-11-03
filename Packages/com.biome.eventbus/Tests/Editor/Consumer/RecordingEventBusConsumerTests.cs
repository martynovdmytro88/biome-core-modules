using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using NUnit.Framework;

namespace Biome.EventBus.Editor.Tests.Consumer
{
    /// <summary>
    /// Stands in for a consumer module's test assembly: it references only Abstract and Fakes, yet publishes and asserts
    /// through the recording fake.
    /// </summary>
    [TestFixture]
    public sealed class RecordingEventBusConsumerTests
    {
        [Test]
        public void Consumer_TestsItsProducerThroughTheFake_WithOnlyTheAbstractReference()
        {
            using (var bus = new RecordingEventBus())
            {
                var shown = 0;
                bus.Subscribe((in ConsumerScoreChanged e) => shown = e.Score);

                new ScoreProducer(bus).Add(12);

                Assert.That(bus.Count<ConsumerScoreChanged>(), Is.EqualTo(1));
                Assert.That(bus.Published<ConsumerScoreChanged>()[0].Score, Is.EqualTo(12));
                Assert.That(shown, Is.EqualTo(12));
            }
        }

        private readonly struct ConsumerScoreChanged
        {
            public readonly int Score;

            public ConsumerScoreChanged(int score)
            {
                Score = score;
            }
        }

        private sealed class ScoreProducer
        {
            private readonly IEventPublisher _events;

            public ScoreProducer(IEventPublisher events)
            {
                _events = events;
            }

            public void Add(int score) => _events.Publish(new ConsumerScoreChanged(score));
        }
    }
}
