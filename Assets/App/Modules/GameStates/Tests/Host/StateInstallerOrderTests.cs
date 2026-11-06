using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using Biome.GameStates.Abstract;
using Biome.GameStates.Installers;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>Load runs the base then its dependents, unload runs in reverse; also through the machine.</summary>
    [TestFixture]
    public sealed class StateInstallerOrderTests
    {
        [UnityTest]
        public IEnumerator Load_WithDependents_RunsSelfThenDependentsInOrder() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var installer = new RootInstaller(journal, new LeafInstaller("a", journal), new LeafInstaller("b", journal));

            await installer.LoadAsync(default);

            Assert.That(journal, Is.EqualTo(new[] { "root.load", "a.load", "b.load" }));
        });

        [UnityTest]
        public IEnumerator Unload_WithDependents_RunsDependentsReversedThenSelf() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var installer = new RootInstaller(journal, new LeafInstaller("a", journal), new LeafInstaller("b", journal));

            await installer.UnloadAsync(default);

            Assert.That(journal, Is.EqualTo(new[] { "b.unload", "a.unload", "root.unload" }));
        });

        [UnityTest]
        public IEnumerator Unload_WhenADependentFails_StillUnloadsSelf() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var installer = new RootInstaller(journal, new LeafInstaller("a", journal), new LeafInstaller("b", journal, true));
            var thrown = false;

            try
            {
                await installer.UnloadAsync(default);
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }

            Assert.That(thrown, Is.True);
            Assert.That(journal, Is.EqualTo(new[] { "root.unload" }));
        });

        [UnityTest]
        public IEnumerator Switch_ThroughTheMachine_LoadsAndUnloadsInOrder() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var builder = new ContainerBuilder();
            var backend = new FakeAssetBackend()
                .AddScene(SceneOf(typeof(MenuState)))
                .AddScene(SceneOf(typeof(OtherState)));
            builder.Register<IAssetsService>(_ => FakeAssets.CreateService(backend), Lifetime.Singleton);
            builder.RegisterInstance<IEventPublisher>(new RecordingEventBus());
            builder.RegisterInstance(journal);
            new GameStatesInstaller(new Descriptors())
                .AddState<InitState>()
                .AddState<MenuState, MenuInstaller>(b =>
                {
                    b.Register<LeafA>(Lifetime.Scoped);
                    b.Register<LeafB>(Lifetime.Scoped);
                })
                .AddState<OtherState>()
                .Install(builder);

            using (IObjectResolver container = builder.Build())
            {
                IGameStateMachine machine = container.Resolve<IGameStateMachine>();

                await machine.SwitchAsync<InitState>(default);
                await machine.SwitchAsync<MenuState>(default);
                await machine.SwitchAsync<OtherState>(default);

                Assert.That(
                    journal,
                    Is.EqualTo(new[] { "menu.load", "a.load", "b.load", "b.unload", "a.unload", "menu.unload" }));
            }
        });

        private static AssetKey SceneOf(Type state)
        {
            return new AssetKey("scene/" + state.Name);
        }

        private sealed class RootInstaller : StateInstallerBase
        {
            private readonly List<string> _journal;
            private readonly IStateInstaller[] _dependents;

            public RootInstaller(List<string> journal, params IStateInstaller[] dependents)
            {
                _journal = journal;
                _dependents = dependents;
            }

            protected override IReadOnlyList<IStateInstaller> Dependents => _dependents;

            protected override UniTask LoadSelfAsync(CancellationToken ct)
            {
                _journal.Add("root.load");
                return UniTask.CompletedTask;
            }

            protected override UniTask UnloadSelfAsync(CancellationToken ct)
            {
                _journal.Add("root.unload");
                return UniTask.CompletedTask;
            }
        }

        private class LeafInstaller : StateInstallerBase
        {
            private readonly string _name;
            private readonly List<string> _journal;
            private readonly bool _failOnUnload;

            public LeafInstaller(string name, List<string> journal, bool failOnUnload = false)
            {
                _name = name;
                _journal = journal;
                _failOnUnload = failOnUnload;
            }

            protected override IReadOnlyList<IStateInstaller> Dependents => Array.Empty<IStateInstaller>();

            protected override UniTask LoadSelfAsync(CancellationToken ct)
            {
                _journal.Add(_name + ".load");
                return UniTask.CompletedTask;
            }

            protected override UniTask UnloadSelfAsync(CancellationToken ct)
            {
                if (_failOnUnload)
                {
                    throw new InvalidOperationException("unload failed");
                }

                _journal.Add(_name + ".unload");
                return UniTask.CompletedTask;
            }
        }

        private sealed class LeafA : LeafInstaller
        {
            public LeafA(List<string> journal)
                : base("a", journal)
            {
            }
        }

        private sealed class LeafB : LeafInstaller
        {
            public LeafB(List<string> journal)
                : base("b", journal)
            {
            }
        }

        private sealed class MenuInstaller : StateInstallerBase
        {
            private readonly List<string> _journal;
            private readonly IStateInstaller[] _dependents;

            public MenuInstaller(List<string> journal, LeafA a, LeafB b)
            {
                _journal = journal;
                _dependents = new IStateInstaller[] { a, b };
            }

            protected override IReadOnlyList<IStateInstaller> Dependents => _dependents;

            protected override UniTask LoadSelfAsync(CancellationToken ct)
            {
                _journal.Add("menu.load");
                return UniTask.CompletedTask;
            }

            protected override UniTask UnloadSelfAsync(CancellationToken ct)
            {
                _journal.Add("menu.unload");
                return UniTask.CompletedTask;
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
        }

        private sealed class MenuState : StateBase
        {
        }

        private sealed class OtherState : StateBase
        {
        }

        private sealed class Descriptors : IBaseStateDescriptorProvider
        {
            public Type InitState => typeof(StateInstallerOrderTests.InitState);

            public bool TryGetScene(Type state, out AssetKey sceneKey)
            {
                sceneKey = SceneOf(state);
                return state != InitState;
            }
        }
    }
}
