using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>Binds the service contract to <see cref="FakeConfigsService"/>, plus the fake's presets and its attempt limit.</summary>
    [TestFixture]
    public sealed class FakeConfigsServiceContractTests : ConfigsServiceContractTests
    {
        [Test]
        public void WithConfigs_Preset_IsReadableAndAlreadyInitialised()
        {
            var config = new TestConfigA(1);

            FakeConfigsService service = FakeConfigsService.WithConfigs(config);

            Assert.IsTrue(service.IsInitialized);
            Assert.IsTrue(service.IsLoaded);
            Assert.IsTrue(service.TryGet<TestConfigA>(out var found));
            Assert.AreSame(config, found);
        }

        [Test]
        public void WithConfigs_TypeNotPreset_ReturnsFalse()
        {
            FakeConfigsService service = FakeConfigsService.WithConfigs(new TestConfigA(1));

            Assert.IsFalse(service.TryGet<TestConfigB>(out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void WithConfigs_TwoOfOneType_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => FakeConfigsService.WithConfigs(new TestConfigA(1), new TestConfigA(2)));
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            IConfigLoader[] loaders = Array.Empty<IConfigLoader>();
            object[] baseConfigs = Array.Empty<object>();

            Assert.Throws<ArgumentNullException>(() => new FakeConfigsService(null, baseConfigs));
            Assert.Throws<ArgumentNullException>(() => new FakeConfigsService(loaders, null));
            Assert.Throws<ArgumentNullException>(() => new FakeConfigsService(new IConfigLoader[] { null }, baseConfigs));
            Assert.Throws<ArgumentNullException>(() => new FakeConfigsService(loaders, new object[] { null }));
        }

        [UnityTest]
        public IEnumerator LoadAllAsync_LoaderKeepsFailing_RunsExactlyMaxLoadAttempts() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader loader = FakeConfigLoader.AlwaysFailing(typeof(TestConfigA));
            var service = new FakeConfigsService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(new FakeConfigsEnvironment("test").Add<TestConfigA>("a"), CancellationToken.None);

            ConfigLoadException failure = await ConfigsTestAsync.CatchAsync<ConfigLoadException>(() => service.LoadAllAsync(CancellationToken.None));

            Assert.AreEqual(FakeConfigsService.MaxLoadAttempts, loader.Attempts);
            Assert.AreEqual(FakeConfigsService.MaxLoadAttempts, failure.Attempts);
        });

        protected override IConfigsService CreateService(IReadOnlyList<IConfigLoader> loaders, IReadOnlyList<object> baseConfigs)
        {
            return new FakeConfigsService(loaders, baseConfigs);
        }
    }
}
