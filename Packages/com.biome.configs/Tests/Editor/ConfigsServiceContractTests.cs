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
    /// <summary>
    /// The service contract written once over <see cref="IConfigsService"/>: the two-step init, the read side, the
    /// loaders run with its recovery, its failure and its cancellation. The package binds it to
    /// <see cref="FakeConfigsService"/>; a host binds its own service by overriding <see cref="CreateService"/>. Loaders
    /// and environments are the package's fakes, so the host supplies only the service under test.
    /// </summary>
    public abstract class ConfigsServiceContractTests
    {
        [Test]
        public void TryGet_BeforeInit_ReturnsFalse()
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), new object[] { new TestConfigA(1) });

            Assert.IsFalse(service.TryGet<TestConfigA>(out var config));
            Assert.IsNull(config);
        }

        [Test]
        public void Flags_BeforeInit_AreFalse()
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), Array.Empty<object>());

            Assert.IsFalse(service.IsInitialized);
            Assert.IsFalse(service.IsLoaded);
        }

        [UnityTest]
        public IEnumerator InitializeAsync_BaseConfig_IsReadableAndNotYetLoaded() => UniTask.ToCoroutine(async () =>
        {
            var baseConfig = new TestConfigA(1);
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), new object[] { baseConfig });

            await service.InitializeAsync(NewEnvironment(), CancellationToken.None);

            Assert.IsTrue(service.IsInitialized);
            Assert.IsFalse(service.IsLoaded);
            Assert.IsTrue(service.TryGet<TestConfigA>(out var config));
            Assert.AreSame(baseConfig, config);
        });

        [UnityTest]
        public IEnumerator TryGet_UnregisteredTypeAfterInit_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), new object[] { new TestConfigA(1) });
            await service.InitializeAsync(NewEnvironment(), CancellationToken.None);

            Assert.IsFalse(service.TryGet<TestConfigB>(out var config));
            Assert.IsNull(config);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_CancelledToken_PublishesNothing() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), new object[] { new TestConfigA(1) });
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await ConfigsTestAsync.CatchAsync<OperationCanceledException>(() => service.InitializeAsync(NewEnvironment(), cts.Token));
            }

            Assert.IsFalse(service.IsInitialized);
            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
        });

        [UnityTest]
        public IEnumerator InitializeAsync_Twice_Throws() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment(), CancellationToken.None);

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.InitializeAsync(NewEnvironment(), CancellationToken.None));
        });

        [UnityTest]
        public IEnumerator InitializeAsync_NullEnvironment_Throws() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), Array.Empty<object>());

            await ConfigsTestAsync.CatchAsync<ArgumentNullException>(() => service.InitializeAsync(null, CancellationToken.None));

            Assert.IsFalse(service.IsInitialized);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_GuardReportsADuplicateType_FailsLoudlyAndStaysNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigA(1)) }, Array.Empty<object>());

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.InitializeAsync(new DuplicateTypeEnvironment(), CancellationToken.None));

            Assert.IsFalse(service.IsInitialized);
        });

        [UnityTest]
        public IEnumerator InitializeAsync_LoaderWithoutSource_FailsLoudlyAndPublishesNothing() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigB("b")) },
                new object[] { new TestConfigA(1) });

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None));

            Assert.IsFalse(service.IsInitialized);
            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
        });

        [UnityTest]
        public IEnumerator InitializeAsync_TwoLoadersForOneType_FailsLoudly() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigA(1)), FakeConfigLoader.Succeeding(new TestConfigA(2)) },
                Array.Empty<object>());

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None));
        });

        [UnityTest]
        public IEnumerator InitializeAsync_BaseConfigAndLoaderForOneType_FailsLoudly() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigA(1)) },
                new object[] { new TestConfigA(2) });

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None));

            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_BeforeInit_Throws() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), Array.Empty<object>());

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.LoadAllAsync(CancellationToken.None));

            Assert.IsFalse(service.IsLoaded);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_AfterALoad_Throws() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(Array.Empty<IConfigLoader>(), Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment(), CancellationToken.None);
            await service.LoadAllAsync(CancellationToken.None);

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(() => service.LoadAllAsync(CancellationToken.None));
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_LoadersSucceed_LandTheirConfigs() => UniTask.ToCoroutine(async () =>
        {
            var a = new TestConfigA(1);
            var b = new TestConfigB("b");
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(a), FakeConfigLoader.Succeeding(b) },
                Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a"), (typeof(TestConfigB), "b")), CancellationToken.None);

            await service.LoadAllAsync(CancellationToken.None);

            Assert.IsTrue(service.IsLoaded);
            Assert.IsTrue(service.TryGet<TestConfigA>(out var foundA));
            Assert.IsTrue(service.TryGet<TestConfigB>(out var foundB));
            Assert.AreSame(a, foundA);
            Assert.AreSame(b, foundB);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_RunningLoader_ReceivesTheEnvironmentSourceAndAReader() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader loader = FakeConfigLoader.Succeeding(new TestConfigA(1));
            IConfigsService service = CreateService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "path/a")), CancellationToken.None);

            await service.LoadAllAsync(CancellationToken.None);

            Assert.AreEqual(new ConfigSource(ConfigSourceKind.Resources, "path/a"), loader.LastSource);
            Assert.IsNotNull(loader.LastReader);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_SeveralLoaders_CompletesOnlyWhenAllHave() => UniTask.ToCoroutine(async () =>
        {
            var gateA = new UniTaskCompletionSource<object>();
            var gateB = new UniTaskCompletionSource<object>();
            FakeConfigLoader loaderA = FakeConfigLoader.Gated(typeof(TestConfigA), gateA);
            FakeConfigLoader loaderB = FakeConfigLoader.Gated(typeof(TestConfigB), gateB);
            IConfigsService service = CreateService(new IConfigLoader[] { loaderA, loaderB }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a"), (typeof(TestConfigB), "b")), CancellationToken.None);

            UniTask run = service.LoadAllAsync(CancellationToken.None);

            Assert.AreEqual(1, loaderA.Attempts);
            Assert.AreEqual(1, loaderB.Attempts);
            gateA.TrySetResult(new TestConfigA(1));
            Assert.AreEqual(UniTaskStatus.Pending, run.Status);
            Assert.IsFalse(service.IsLoaded);

            gateB.TrySetResult(new TestConfigB("b"));
            await run;

            Assert.IsTrue(service.IsLoaded);
            Assert.IsTrue(service.TryGet<TestConfigA>(out _));
            Assert.IsTrue(service.TryGet<TestConfigB>(out _));
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_LoaderFailsOnce_Recovers() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            FakeConfigLoader loader = FakeConfigLoader.FailingTimes(1, config);
            IConfigsService service = CreateService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None);

            await service.LoadAllAsync(CancellationToken.None);

            Assert.AreEqual(2, loader.Attempts);
            Assert.IsTrue(service.IsLoaded);
            Assert.IsTrue(service.TryGet<TestConfigA>(out var found));
            Assert.AreSame(config, found);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_LoaderKeepsFailing_ThrowsAndPublishesNothing() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader failing = FakeConfigLoader.AlwaysFailing(typeof(TestConfigA));
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigB("b")), failing },
                Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a"), (typeof(TestConfigB), "b")), CancellationToken.None);

            ConfigLoadException failure = await ConfigsTestAsync.CatchAsync<ConfigLoadException>(() => service.LoadAllAsync(CancellationToken.None));

            Assert.AreEqual(typeof(TestConfigA), failure.ConfigType);
            Assert.IsNotNull(failure.InnerException);
            Assert.That(failing.Attempts, Is.GreaterThan(1), "a failed load is retried");
            Assert.AreEqual(failing.Attempts, failure.Attempts);
            Assert.IsFalse(service.IsLoaded);
            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
            Assert.IsFalse(service.TryGet<TestConfigB>(out _));
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_LoaderReturnsNull_CountsAsAFailure() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader loader = FakeConfigLoader.Returning(typeof(TestConfigA), null);
            IConfigsService service = CreateService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None);

            ConfigLoadException failure = await ConfigsTestAsync.CatchAsync<ConfigLoadException>(() => service.LoadAllAsync(CancellationToken.None));

            Assert.AreEqual(failure.Attempts, loader.Attempts);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_LoaderReturnsAnotherType_CountsAsAFailure() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader loader = FakeConfigLoader.Returning(typeof(TestConfigA), new TestConfigB("b"));
            IConfigsService service = CreateService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None);

            await ConfigsTestAsync.CatchAsync<ConfigLoadException>(() => service.LoadAllAsync(CancellationToken.None));

            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_AfterAFailure_CanRunAgain() => UniTask.ToCoroutine(async () =>
        {
            var loader = new SwitchableLoader(new TestConfigA(1));
            IConfigsService service = CreateService(new IConfigLoader[] { loader }, Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a")), CancellationToken.None);
            await ConfigsTestAsync.CatchAsync<ConfigLoadException>(() => service.LoadAllAsync(CancellationToken.None));

            loader.Fails = false;
            await service.LoadAllAsync(CancellationToken.None);

            Assert.IsTrue(service.IsLoaded);
        });

        [UnityTest]
        public IEnumerator LoadAllAsync_Cancelled_EndsTheRunAndPublishesNothing() => UniTask.ToCoroutine(async () =>
        {
            IConfigsService service = CreateService(
                new IConfigLoader[] { FakeConfigLoader.Succeeding(new TestConfigB("b")), FakeConfigLoader.UntilCancelled(typeof(TestConfigA)) },
                Array.Empty<object>());
            await service.InitializeAsync(NewEnvironment((typeof(TestConfigA), "a"), (typeof(TestConfigB), "b")), CancellationToken.None);
            using (var cts = new CancellationTokenSource())
            {
                UniTask run = service.LoadAllAsync(cts.Token);
                cts.Cancel();

                await ConfigsTestAsync.CatchAsync<OperationCanceledException>(() => run);
            }

            Assert.IsFalse(service.IsLoaded);
            Assert.IsFalse(service.TryGet<TestConfigA>(out _));
            Assert.IsFalse(service.TryGet<TestConfigB>(out _));
        });

        /// <summary>The service under test over these loaders and base configs.</summary>
        protected abstract IConfigsService CreateService(IReadOnlyList<IConfigLoader> loaders, IReadOnlyList<object> baseConfigs);

        private static IConfigsEnvironment NewEnvironment(params (Type Type, string Path)[] sources)
        {
            var environment = new FakeConfigsEnvironment("test");
            foreach ((Type type, string path) in sources)
            {
                environment.Add(type, new ConfigSource(ConfigSourceKind.Resources, path));
            }

            return environment;
        }

        private sealed class DuplicateTypeEnvironment : IConfigsEnvironment, IConfigsEnvironmentGuard
        {
            public string Id => "duplicate";

            public bool TryGetSource(Type configType, out ConfigSource source)
            {
                source = new ConfigSource(ConfigSourceKind.Resources, "a");
                return true;
            }

            public bool TryFindDuplicateType(out string configType)
            {
                configType = typeof(TestConfigA).FullName;
                return true;
            }
        }

        private sealed class SwitchableLoader : IConfigLoader
        {
            private readonly object _config;

            public SwitchableLoader(object config)
            {
                _config = config;
            }

            public bool Fails { get; set; } = true;

            public Type ConfigType => _config.GetType();

            public UniTask<object> LoadAsync(ConfigSource source, IConfigSourceReader reader, CancellationToken ct)
            {
                return Fails
                    ? UniTask.FromException<object>(new InvalidOperationException("scripted failure"))
                    : UniTask.FromResult(_config);
            }
        }
    }
}
