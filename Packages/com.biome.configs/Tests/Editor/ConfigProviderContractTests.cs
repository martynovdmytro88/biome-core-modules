using System;
using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>
    /// The provider contract written once over <see cref="IConfig{T}"/>: it throws before the config is available and
    /// returns the instance after, never null. A host binds its provider by overriding <see cref="CreateProvider"/>.
    /// </summary>
    public abstract class ConfigProviderContractTests
    {
        [Test]
        public void Constructor_NullReader_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateProvider(null));
        }

        [Test]
        public void Value_ReaderHoldsNoSuchConfig_ThrowsConfigNotInitialized()
        {
            IConfig<TestConfigA> provider = CreateProvider(FakeConfigsService.WithConfigs(new TestConfigB("b")));

            ConfigNotInitializedException failure = Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);

            Assert.AreEqual(typeof(TestConfigA), failure.ConfigType);
        }

        [Test]
        public void Value_ReaderHoldsTheConfig_ReturnsTheInstance()
        {
            var config = new TestConfigA(1);
            IConfig<TestConfigA> provider = CreateProvider(FakeConfigsService.WithConfigs(config));

            Assert.AreSame(config, provider.Value);
        }

        [UnityTest]
        public IEnumerator Value_ConfigLandsAfterTheProviderWasMade_ReturnsTheInstance() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            var service = new FakeConfigsService(Array.Empty<IConfigLoader>(), new object[] { config });
            IConfig<TestConfigA> provider = CreateProvider(service);
            Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);

            await service.InitializeAsync(new FakeConfigsEnvironment("test"), CancellationToken.None);

            Assert.AreSame(config, provider.Value);
        });

        /// <summary>The provider under test over <paramref name="reader"/>.</summary>
        protected abstract IConfig<TestConfigA> CreateProvider(IConfigsReader reader);
    }
}
