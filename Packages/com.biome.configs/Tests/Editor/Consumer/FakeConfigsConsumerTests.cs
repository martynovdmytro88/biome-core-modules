using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests.Consumer
{
    /// <summary>
    /// Stands in for a consumer module's test assembly: it references only Abstract and Fakes, and reads its config through
    /// the preset fake.
    /// </summary>
    [TestFixture]
    public sealed class FakeConfigsConsumerTests
    {
        [Test]
        public void Consumer_ReadsItsConfigThroughTheFake_WithOnlyTheAbstractReference()
        {
            var config = new ConsumerConfig();
            FakeConfigsService service = FakeConfigsService.WithConfigs(config);

            var reader = new ConfigReadingConsumer(service);
            IConfig<ConsumerConfig> provider = new FakeConfigsService.Provider<ConsumerConfig>(service);

            Assert.That(reader.Read(), Is.SameAs(config));
            Assert.That(provider.Value, Is.SameAs(config));
        }

        private sealed class ConsumerConfig
        {
        }

        private sealed class ConfigReadingConsumer
        {
            private readonly IConfigsReader _configs;

            public ConfigReadingConsumer(IConfigsReader configs)
            {
                _configs = configs;
            }

            public ConsumerConfig Read() => _configs.TryGet<ConsumerConfig>(out var config) ? config : null;
        }
    }
}
