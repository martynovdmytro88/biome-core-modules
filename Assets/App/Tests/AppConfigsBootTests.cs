using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using Biome.Configs.Fakes;
using Biome.Configs.Installers;
using Biome.Configs.Installers.Mock;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace Biome.App.Tests
{
    /// <summary>
    /// The configs init in the Init state: the environment the scope registered is injected and handed to the
    /// service's environment init, the state is not entered until the init completes, the loaders run after it, a
    /// failure surfaces, and a missing environment fails loudly.
    /// </summary>
    public sealed class AppConfigsBootTests
    {
        private readonly List<IObjectResolver> _containers = new List<IObjectResolver>();
        private readonly List<Object> _created = new List<Object>();

        private IObjectResolver Build(Action<IContainerBuilder> register)
        {
            var builder = new ContainerBuilder();
            register(builder);
            var container = builder.Build();
            _containers.Add(container);
            return container;
        }

        [TearDown]
        public void DisposeCreated()
        {
            foreach (var container in _containers)
            {
                container.Dispose();
            }

            _containers.Clear();
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void EnterAsync_ProfileEnvironment_InitialisesTheServiceWithIt()
        {
            var service = new RecordingConfigsService();
            var container = Build(builder =>
            {
                builder.RegisterInstance<IConfigsService>(service);
                builder.RegisterInstance(AppTestEnvironment.NamedByRoot());
                builder.Register<InitState>(Lifetime.Transient);
            });

            var task = container.Resolve<InitState>().EnterAsync(CancellationToken.None);

            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(service.Environment, Is.SameAs(AppTestEnvironment.NamedByRoot()), "the environment the profile registered");
            Assert.That(service.Environment, Is.InstanceOf<EnvironmentConfig>());
            Assert.That(service.Fake.IsInitialized, Is.True, "the environment init ran");
            Assert.That(service.Fake.IsLoaded, Is.True, "the loaders ran after the init");
            Assert.That(service.LoadAllCalls, Is.EqualTo(1));
        }

        [Test]
        public void EnterAsync_ServiceFromTheMockInstaller_IsInitialisedAndLoaded()
        {
            var container = Build(builder =>
            {
                new ConfigsInstallerMock(AppTestEnvironment.NamedByRoot()).Install(builder);
                builder.Register<InitState>(Lifetime.Transient);
            });
            var service = container.Resolve<IConfigsService>();
            Assert.That(service.IsInitialized, Is.False);

            container.Resolve<InitState>().EnterAsync(CancellationToken.None);

            Assert.That(service.IsInitialized, Is.True);
            Assert.That(service.IsLoaded, Is.True, "the Init state loads the configs");
        }

        [Test]
        public void EnterAsync_InitNotCompleted_StaysPendingUntilTheInitCompletes()
        {
            var gate = new UniTaskCompletionSource();
            var service = new RecordingConfigsService { Gate = gate.Task };
            var container = Build(builder =>
            {
                builder.RegisterInstance<IConfigsService>(service);
                builder.RegisterInstance(AppTestEnvironment.NamedByRoot());
                builder.Register<InitState>(Lifetime.Transient);
            });

            var task = container.Resolve<InitState>().EnterAsync(CancellationToken.None);

            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Pending), "the state is not entered while the init runs");
            gate.TrySetResult();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(service.Fake.IsInitialized, Is.True);
        }

        [Test]
        public void EnterAsync_InitFails_TheFailureSurfaces()
        {
            var gate = new UniTaskCompletionSource();
            var service = new RecordingConfigsService { Gate = gate.Task };
            var container = Build(builder =>
            {
                builder.RegisterInstance<IConfigsService>(service);
                builder.RegisterInstance(AppTestEnvironment.NamedByRoot());
                builder.Register<InitState>(Lifetime.Transient);
            });

            var task = container.Resolve<InitState>().EnterAsync(CancellationToken.None);
            gate.TrySetException(new InvalidOperationException("init failed"));

            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Faulted), "no silent continue");
            var thrown = Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult(), "the failure surfaces to the awaiter");
            Assert.That(thrown.Message, Is.EqualTo("init failed"));
            Assert.That(service.Fake.IsInitialized, Is.False);
        }

        [Test]
        public void ResolveInitState_ContainerHoldsNoEnvironment_Throws()
        {
            var service = new RecordingConfigsService();
            var container = Build(builder =>
            {
                builder.RegisterInstance<IConfigsService>(service);
                builder.Register<InitState>(Lifetime.Transient);
            });

            Assert.Throws<VContainerException>(() => container.Resolve<InitState>());
            Assert.That(service.Fake.IsInitialized, Is.False, "no silent default environment");
        }

        [UnityTest]
        public IEnumerator EnterAsync_ProductionEnvironmentNoLoaders_ConfigsAreLoaded() => UniTask.ToCoroutine(async () =>
        {
            var installer = new ConfigsInstaller(AppTestEnvironment.NamedByRoot());
            var container = Build(builder =>
            {
                installer.Install(builder);
                builder.Register<InitState>(Lifetime.Transient);
            });
            var service = container.Resolve<IConfigsService>();

            await container.Resolve<InitState>().EnterAsync(CancellationToken.None);

            Assert.That(service.IsInitialized, Is.True);
            Assert.That(service.IsLoaded, Is.True, "LoadAll completes with zero production loaders");
        });

        /// <summary>Wraps the fake service and records what the boot hands it.</summary>
        private sealed class RecordingConfigsService : IConfigsService
        {
            internal FakeConfigsService Fake { get; } = new FakeConfigsService(Array.Empty<IConfigLoader>(), Array.Empty<object>());

            internal IConfigsEnvironment Environment { get; private set; }

            internal int LoadAllCalls { get; private set; }

            /// <summary>When set, the init waits for it before the fake is initialised.</summary>
            internal UniTask? Gate { get; set; }

            public bool IsInitialized => Fake.IsInitialized;

            public bool IsLoaded => Fake.IsLoaded;

            public bool TryGet<T>(out T config) where T : class => Fake.TryGet(out config);

            public UniTask InitializeAsync(IConfigsEnvironment environment, CancellationToken ct)
            {
                Environment = environment;
                return Gate.HasValue ? InitializeAfterGateAsync(environment, ct) : Fake.InitializeAsync(environment, ct);
            }

            private async UniTask InitializeAfterGateAsync(IConfigsEnvironment environment, CancellationToken ct)
            {
                await Gate.Value;
                await Fake.InitializeAsync(environment, ct);
            }

            public UniTask LoadAllAsync(CancellationToken ct)
            {
                LoadAllCalls++;
                return Fake.LoadAllAsync(ct);
            }
        }
    }
}
