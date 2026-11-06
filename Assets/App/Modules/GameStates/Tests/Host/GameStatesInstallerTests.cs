using System;
using System.Collections;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using Biome.GameStates.Abstract;
using Biome.GameStates.Installers;
using Biome.GameStates.Installers.Mock;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>The module installer and its mock twin resolved in a headless VContainer container.</summary>
    [TestFixture]
    public sealed class GameStatesInstallerTests
    {
        [Test]
        public void Resolve_Installer_ReturnsTheMachineAsItsInterface()
        {
            using (IObjectResolver container = Build(new Probe()))
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();

                Assert.That(machine, Is.InstanceOf<GameStateMachine>());
                Assert.That(container.Resolve<IGameStateMachine>(), Is.SameAs(machine));
            }
        }

        [Test]
        public void Resolve_MockInstaller_ReturnsTheGivenFake()
        {
            var fake = new StubMachine();
            var builder = new ContainerBuilder();
            new GameStatesInstallerMock(fake).Install(builder);

            using (IObjectResolver container = builder.Build())
            {
                Assert.That(container.Resolve<IGameStateMachine>(), Is.SameAs(fake));
            }
        }

        [Test]
        public void Create_MockInstallerWithoutMachine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameStatesInstallerMock(null));
        }

        [UnityTest]
        public IEnumerator Switch_BaseState_ChildScopeSeesTheInitScope() => UniTask.ToCoroutine(async () =>
        {
            var probe = new Probe();
            using (IObjectResolver container = Build(probe))
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();

                await machine.SwitchAsync<InitState>(default);
                await machine.SwitchAsync<MenuState>(default);

                Assert.That(probe.Menu, Is.Not.Null);
                Assert.That(probe.Menu.Init, Is.SameAs(probe.InitService));
            }
        });

        [UnityTest]
        public IEnumerator Switch_Sub_UsesTheServiceOfTheStateScope() => UniTask.ToCoroutine(async () =>
        {
            var probe = new Probe();
            using (IObjectResolver container = Build(probe))
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();
                await machine.SwitchAsync<InitState>(default);
                await machine.SwitchAsync<MenuState>(default);

                await probe.Menu.SubStates.SwitchAsync<MenuSubState>(default);

                Assert.That(probe.Menu.SubStates.Current, Is.EqualTo(typeof(MenuSubState)));
                Assert.That(probe.MenuSub.SubStates, Is.SameAs(probe.Menu.SubStates));
            }
        });

        [UnityTest]
        public IEnumerator Switch_Away_DisposesTheChildScopeAndKeepsTheInitScope() => UniTask.ToCoroutine(async () =>
        {
            var probe = new Probe();
            using (IObjectResolver container = Build(probe))
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();
                await machine.SwitchAsync<InitState>(default);
                await machine.SwitchAsync<MenuState>(default);

                await machine.SwitchAsync<OtherState>(default);

                Assert.That(probe.Menu.Tracked.Disposed, Is.True);
                Assert.That(probe.InitService.Disposed, Is.False);
            }
        });

        [UnityTest]
        public IEnumerator Switch_UnregisteredState_FailsTheSwitch() => UniTask.ToCoroutine(async () =>
        {
            var probe = new Probe();
            using (IObjectResolver container = Build(probe))
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();
                await machine.SwitchAsync<InitState>(default);

                await machine.SwitchAsync<UnknownState>(default);

                Assert.That(machine.Current, Is.Null);
            }
        });

        private static IObjectResolver Build(Probe probe)
        {
            var builder = new ContainerBuilder();
            var backend = new FakeAssetBackend()
                .AddScene(SceneOf(typeof(MenuState)))
                .AddScene(SceneOf(typeof(OtherState)))
                .AddScene(SceneOf(typeof(UnknownState)));
            builder.Register<IAssetsService>(_ => FakeAssets.CreateService(backend), Lifetime.Singleton);
            builder.RegisterInstance<IEventPublisher>(new RecordingEventBus());
            builder.RegisterInstance(probe);
            new GameStatesInstaller(new Descriptors(), new SilentSink())
                .AddState<InitState>(b => b.Register<InitService>(Lifetime.Singleton))
                .AddState<MenuState>(b =>
                {
                    b.Register<MenuSubState>(Lifetime.Scoped);
                    b.Register<TrackedService>(Lifetime.Scoped);
                })
                .AddState<OtherState>()
                .Install(builder);
            return builder.Build();
        }

        private static AssetKey SceneOf(Type state)
        {
            return new AssetKey("scene/" + state.Name);
        }

        private sealed class Probe
        {
            public InitService InitService { get; set; }

            public MenuState Menu { get; set; }

            public MenuSubState MenuSub { get; set; }
        }

        private sealed class InitService : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private sealed class TrackedService : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private abstract class StateBase : IGameState
        {
            public UniTask EnterAsync(CancellationToken ct)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ExitAsync(CancellationToken ct)
            {
                return UniTask.CompletedTask;
            }
        }

        private sealed class InitState : StateBase
        {
            public InitState(Probe probe, InitService service)
            {
                probe.InitService = service;
            }
        }

        private sealed class MenuState : StateBase
        {
            public MenuState(Probe probe, InitService init, ISubStateService subStates, TrackedService tracked)
            {
                Init = init;
                SubStates = subStates;
                Tracked = tracked;
                probe.Menu = this;
            }

            public InitService Init { get; }

            public ISubStateService SubStates { get; }

            public TrackedService Tracked { get; }
        }

        private sealed class MenuSubState : StateBase
        {
            public MenuSubState(Probe probe, ISubStateService subStates)
            {
                SubStates = subStates;
                probe.MenuSub = this;
            }

            public ISubStateService SubStates { get; }
        }

        private sealed class OtherState : StateBase
        {
        }

        private sealed class UnknownState : StateBase
        {
        }

        private sealed class Descriptors : IBaseStateDescriptorProvider
        {
            public Type InitState => typeof(GameStatesInstallerTests.InitState);

            public bool TryGetScene(Type state, out AssetKey sceneKey)
            {
                sceneKey = SceneOf(state);
                return state != InitState;
            }
        }

        private sealed class SilentSink : IStateLogSink
        {
            public void Info(string message)
            {
            }

            public void Error(string message)
            {
            }
        }

        private sealed class StubMachine : IGameStateMachine
        {
            public Type Current => null;

            public UniTask SwitchAsync<TState>(CancellationToken ct)
                where TState : IGameState
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
