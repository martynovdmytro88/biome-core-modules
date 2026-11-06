using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using Biome.Configs.Data;
using Biome.Configs.Editor.Tests;
using Biome.Configs.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biome.Configs.Tests.Host
{
    /// <summary>The production <see cref="ConfigProvider{T}"/> over the production <see cref="ConfigsService"/> and <see cref="EnvironmentConfig"/>.</summary>
    [TestFixture]
    public sealed class ConfigProviderServiceTests
    {
        private EnvironmentConfig _environment;

        [SetUp]
        public void CreateEnvironment()
        {
            _environment = ConfigsTestEnvironment.CreateAsset("test");
        }

        [TearDown]
        public void DestroyEnvironment()
        {
            Object.DestroyImmediate(_environment);
        }

        [Test]
        public void Value_BeforeInit_ThrowsConfigNotInitialized()
        {
            ConfigsService service = NewService(new TestConfigA(1));
            var provider = new ConfigProvider<TestConfigA>(service);

            ConfigNotInitializedException failure = Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);

            Assert.AreEqual(typeof(TestConfigA), failure.ConfigType);
        }

        [UnityTest]
        public IEnumerator Value_AfterInit_ReturnsTheInstance() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            ConfigsService service = NewService(config);
            await service.InitializeAsync(_environment, CancellationToken.None);

            var provider = new ConfigProvider<TestConfigA>(service);

            Assert.AreSame(config, provider.Value);
        });

        [UnityTest]
        public IEnumerator Value_ProviderMadeBeforeInit_ReadsTheConfigOnceInitialized() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            ConfigsService service = NewService(config);
            var provider = new ConfigProvider<TestConfigA>(service);
            Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);

            await service.InitializeAsync(_environment, CancellationToken.None);

            Assert.AreSame(config, provider.Value);
        });

        [UnityTest]
        public IEnumerator Value_TypeTheServiceDoesNotHold_ThrowsEvenAfterInit() => UniTask.ToCoroutine(async () =>
        {
            ConfigsService service = NewService(new TestConfigA(1));
            await service.InitializeAsync(_environment, CancellationToken.None);
            var provider = new ConfigProvider<TestConfigB>(service);

            Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);
        });

        private static ConfigsService NewService(object baseConfig)
        {
            return new ConfigsService(new ConfigStore(), new ResourcesConfigSource(), new IConfigLoader[0], new[] { baseConfig });
        }
    }
}
