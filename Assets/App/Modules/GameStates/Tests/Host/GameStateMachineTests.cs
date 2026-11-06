using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Biome.EventBus.Fakes;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>The base-level behaviour of the machine over fake scopes, scenes, bus and log.</summary>
    [TestFixture]
    public sealed class GameStateMachineTests
    {
        [UnityTest]
        public IEnumerator Switch_Init_EntersWithoutSceneLoad() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            InitState init = h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));

            await h.Machine.SwitchAsync<InitState>(default);

            Assert.That(h.Machine.Current, Is.EqualTo(typeof(InitState)));
            Assert.That(init.EnterCount, Is.EqualTo(1));
            Assert.That(h.Backend.SceneLoadedCount(Harness.SceneOf<MenuState>()), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Switch_InitTwice_Throws() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);

            await Harness.AssertThrowsAsync<InvalidOperationException>(() => h.Machine.SwitchAsync<InitState>(default));
        });

        [UnityTest]
        public IEnumerator Switch_BeforeInit_Throws() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));

            await Harness.AssertThrowsAsync<InvalidOperationException>(() => h.Machine.SwitchAsync<MenuState>(default));
        });

        [UnityTest]
        public IEnumerator Switch_Base_PublishesTheSwitchEventMarkedBase() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            h.AddBase(new GameplayState(h.Journal));

            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);
            await h.Machine.SwitchAsync<GameplayState>(default);

            IReadOnlyList<StateSwitchedEvent> events = h.Bus.Published<StateSwitchedEvent>();
            Assert.That(events.Select(e => e.Level), Is.All.EqualTo(StateLevel.Base));
            Assert.That(events.Select(e => e.From), Is.EqualTo(new Type[] { null, typeof(InitState), typeof(MenuState) }));
            Assert.That(events.Select(e => e.To), Is.EqualTo(new[] { typeof(InitState), typeof(MenuState), typeof(GameplayState) }));
        });

        [UnityTest]
        public IEnumerator Switch_Base_IsLoggedUnderTheStateTag() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));

            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);

            Assert.That(h.Log.Infos, Has.Count.EqualTo(2));
            Assert.That(h.Log.Infos, Is.All.StartsWith(StateSwitchLogger.BaseTag));
            Assert.That(h.Log.Infos[1], Does.Contain("InitState -> MenuState"));
        });

        [UnityTest]
        public IEnumerator Switch_QueuedDuringARunningSwitch_RunsAfterIt() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            MenuState menu = h.AddBase(new MenuState(h.Journal));
            GameplayState gameplay = h.AddBase(new GameplayState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);
            h.Journal.Clear();
            menu.EnterGate = new UniTaskCompletionSource();

            UniTask first = h.Machine.SwitchAsync<MenuState>(default);
            UniTask second = h.Machine.SwitchAsync<GameplayState>(default);

            Assert.That(menu.ExitCount, Is.EqualTo(0));
            Assert.That(gameplay.EnterCount, Is.EqualTo(0));
            menu.EnterGate.TrySetResult();
            await first;
            await second;

            Assert.That(
                h.Journal,
                Is.EqualTo(new[] { "Init.Exit", "Menu.Install", "Menu.Enter", "Menu.Exit", "Menu.Uninstall", "Gameplay.Install", "Gameplay.Enter" }));
            Assert.That(h.Machine.Current, Is.EqualTo(typeof(GameplayState)));
        });

        [UnityTest]
        public IEnumerator Switch_BetweenBaseStates_NeverOverlapsTheirScopes() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            h.AddBase(new GameplayState(h.Journal));

            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);
            await h.Machine.SwitchAsync<GameplayState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);

            Assert.That(h.Factory.MaxLiveChildScopes, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Switch_AwayFromABaseState_ReleasesScopeSceneAssetsAndSubStates() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            h.AddBase(new GameplayState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);
            TestScope menuScope = h.Factory.ScopeOf<MenuState>();
            Assert.That(menuScope.Disposed, Is.False);
            Assert.That(h.Backend.LiveSceneCount(Harness.SceneOf<MenuState>()), Is.EqualTo(1));

            await h.Machine.SwitchAsync<GameplayState>(default);

            Assert.That(menuScope.Disposed, Is.True);
            Assert.That(menuScope.Assets.IsClosed, Is.True);
            Assert.That(h.Backend.LiveSceneCount(Harness.SceneOf<MenuState>()), Is.EqualTo(0));
            await Harness.AssertThrowsAsync<ObjectDisposedException>(() => menuScope.SubStates.SwitchAsync<InitState>(default));
        });

        [UnityTest]
        public IEnumerator Switch_AwayFromInit_KeepsTheInitScopeAndInstaller() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            InitState init = h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);

            await h.Machine.SwitchAsync<MenuState>(default);

            TestScope initScope = h.Factory.ScopeOf<InitState>();
            Assert.That(init.ExitCount, Is.EqualTo(1));
            Assert.That(initScope.Disposed, Is.False);
            Assert.That(initScope.Assets.IsClosed, Is.False);
            Assert.That(h.Journal, Does.Not.Contain("Init.Uninstall"));
        });

        [UnityTest]
        public IEnumerator Switch_WithAnApplyingTransition_RunsItBetweenExitAndEntry() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var transition = new RecordingStateTransition(
                "T",
                journal,
                (from, to) => from == typeof(MenuState) && to == typeof(GameplayState));
            var h = new Harness(journal, transition);
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            h.AddBase(new GameplayState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);
            Assert.That(transition.Runs, Is.Empty);
            h.Journal.Clear();

            await h.Machine.SwitchAsync<GameplayState>(default);

            Assert.That(h.Journal, Is.EqualTo(new[] { "Menu.Exit", "Menu.Uninstall", "T.Run", "Gameplay.Install", "Gameplay.Enter" }));
            Assert.That(transition.Runs.Single(), Is.EqualTo((typeof(MenuState), typeof(GameplayState))));
        });

        [UnityTest]
        public IEnumerator Switch_Base_LoadsTheInstallerBeforeEntryAndUnloadsItAfterExit() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            h.AddBase(new MenuState(h.Journal));
            h.AddBase(new GameplayState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);
            await h.Machine.SwitchAsync<MenuState>(default);
            h.Journal.Clear();

            await h.Machine.SwitchAsync<GameplayState>(default);

            Assert.That(h.Journal, Is.EqualTo(new[] { "Menu.Exit", "Menu.Uninstall", "Gameplay.Install", "Gameplay.Enter" }));
        });

        [UnityTest]
        public IEnumerator Switch_BaseStateWithoutScene_EntersWithoutSceneLoad() => UniTask.ToCoroutine(async () =>
        {
            var h = new Harness();
            h.AddInit(new InitState(h.Journal));
            GameplayState gameplay = h.AddBaseWithoutScene(new GameplayState(h.Journal));
            await h.Machine.SwitchAsync<InitState>(default);

            await h.Machine.SwitchAsync<GameplayState>(default);

            Assert.That(h.Machine.Current, Is.EqualTo(typeof(GameplayState)));
            Assert.That(gameplay.EnterCount, Is.EqualTo(1));
            Assert.That(h.Backend.SceneLoadedCount(Harness.SceneOf<GameplayState>()), Is.EqualTo(0));
            Assert.That(h.Factory.ScopeOf<GameplayState>().Disposed, Is.False);
        });

        internal sealed class InitState : FakeGameState
        {
            public InitState(IList<string> journal)
                : base("Init", journal)
            {
            }
        }

        internal sealed class MenuState : FakeGameState
        {
            public MenuState(IList<string> journal)
                : base("Menu", journal)
            {
            }
        }

        internal sealed class GameplayState : FakeGameState
        {
            public GameplayState(IList<string> journal)
                : base("Gameplay", journal)
            {
            }
        }

        /// <summary>The wiring shared by the GameStates fixtures: the machine over fake scopes, scenes, bus and log.</summary>
        internal sealed class Harness
        {
            public Harness(IList<string> journal = null, params IStateTransition[] transitions)
            {
                Journal = journal ?? new List<string>();
                Backend = new FakeAssetBackend();
                Bus = new RecordingEventBus();
                Log = new TestLogSink();
                Descriptors = new TestDescriptors();
                Factory = new TestScopeFactory(Journal, Bus);
                IAssetsService service = FakeAssets.CreateService(Backend);
                Machine = new GameStateMachine(Factory, Descriptors, service.GlobalContext, Bus, Log, transitions);
            }

            public IList<string> Journal { get; }

            public FakeAssetBackend Backend { get; }

            public RecordingEventBus Bus { get; }

            public TestLogSink Log { get; }

            public TestDescriptors Descriptors { get; }

            public TestScopeFactory Factory { get; }

            public GameStateMachine Machine { get; }

            public static AssetKey SceneOf<TState>()
            {
                return new AssetKey("scene/" + typeof(TState).Name);
            }

            public static async UniTask AssertThrowsAsync<TException>(Func<UniTask> action)
                where TException : Exception
            {
                var thrown = false;
                try
                {
                    await action();
                }
                catch (TException)
                {
                    thrown = true;
                }

                Assert.That(thrown, Is.True, $"{typeof(TException).Name} was expected.");
            }

            public ScopeSetup SetupOf<TState>()
            {
                return Factory.SetupOf(typeof(TState));
            }

            public TState AddInit<TState>(TState state)
                where TState : IGameState
            {
                Descriptors.InitState = typeof(TState);
                Factory.InitType = typeof(TState);
                Factory.SetupOf(typeof(TState)).States[typeof(TState)] = state;
                return state;
            }

            public TState AddBase<TState>(TState state)
                where TState : IGameState
            {
                AssetKey key = SceneOf<TState>();
                Backend.AddScene(key);
                Descriptors.Scenes[typeof(TState)] = key;
                Factory.SetupOf(typeof(TState)).States[typeof(TState)] = state;
                return state;
            }

            public TState AddBaseWithoutScene<TState>(TState state)
                where TState : IGameState
            {
                Factory.SetupOf(typeof(TState)).States[typeof(TState)] = state;
                return state;
            }

            public TSub AddSub<TBase, TSub>(TSub sub)
                where TSub : IGameState
            {
                Factory.SetupOf(typeof(TBase)).States[typeof(TSub)] = sub;
                return sub;
            }
        }

        internal sealed class TestLogSink : IStateLogSink
        {
            public List<string> Infos { get; } = new List<string>();

            public List<string> Errors { get; } = new List<string>();

            public void Info(string message)
            {
                Infos.Add(message);
            }

            public void Error(string message)
            {
                Errors.Add(message);
            }
        }

        internal sealed class TestDescriptors : IBaseStateDescriptorProvider
        {
            public Type InitState { get; set; }

            public Dictionary<Type, AssetKey> Scenes { get; } = new Dictionary<Type, AssetKey>();

            public bool TryGetScene(Type state, out AssetKey sceneKey)
            {
                return Scenes.TryGetValue(state, out sceneKey);
            }
        }

        internal sealed class RecordingHandler : IStateScenarioHandler
        {
            private readonly RecordingEventBus _bus;

            public RecordingHandler(RecordingEventBus bus)
            {
                _bus = bus;
            }

            public List<StateFailure> Failures { get; } = new List<StateFailure>();

            public List<int> ErrorEventsAtCall { get; } = new List<int>();

            public Exception Failure { get; set; }

            public UniTask HandleAsync(StateFailure failure, System.Threading.CancellationToken ct)
            {
                Failures.Add(failure);
                ErrorEventsAtCall.Add(_bus.Count<StateErrorEvent>());
                if (Failure != null)
                {
                    throw Failure;
                }

                return UniTask.CompletedTask;
            }
        }

        internal sealed class RecordingInstaller : IStateInstaller
        {
            private readonly string _name;
            private readonly IList<string> _journal;

            public RecordingInstaller(string name, IList<string> journal)
            {
                _name = name;
                _journal = journal;
            }

            public Exception LoadFailure { get; set; }

            public UniTask LoadAsync(System.Threading.CancellationToken ct)
            {
                _journal.Add(_name + ".Install");
                if (LoadFailure != null)
                {
                    throw LoadFailure;
                }

                return UniTask.CompletedTask;
            }

            public UniTask UnloadAsync(System.Threading.CancellationToken ct)
            {
                _journal.Add(_name + ".Uninstall");
                return UniTask.CompletedTask;
            }
        }

        internal sealed class ScopeSetup
        {
            public ScopeSetup(string name, IList<string> journal, RecordingEventBus bus)
            {
                Handler = new RecordingHandler(bus);
                Installer = new RecordingInstaller(name, journal);
            }

            public Dictionary<Type, IGameState> States { get; } = new Dictionary<Type, IGameState>();

            public RecordingHandler Handler { get; set; }

            public RecordingInstaller Installer { get; set; }
        }

        internal sealed class TestScope : IStateScope
        {
            private readonly ScopeSetup _setup;

            public TestScope(Type state, ScopeSetup setup, bool isInit, IAssetsContext assets, ISubStateService subStates)
            {
                State = state;
                _setup = setup;
                IsInit = isInit;
                Assets = assets;
                SubStates = subStates;
            }

            public Type State { get; }

            public bool IsInit { get; }

            public bool Disposed { get; private set; }

            public IAssetsContext Assets { get; }

            public ISubStateService SubStates { get; }

            public IStateScenarioHandler ScenarioHandler => _setup.Handler;

            public IStateInstaller Installer => _setup.Installer;

            public IGameState ResolveState(Type state)
            {
                if (!_setup.States.TryGetValue(state, out IGameState instance))
                {
                    throw new KeyNotFoundException($"{state.Name} is not registered in this scope.");
                }

                return instance;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        internal sealed class TestScopeFactory : IStateScopeFactory
        {
            private readonly IList<string> _journal;
            private readonly RecordingEventBus _bus;
            private readonly Dictionary<Type, ScopeSetup> _setups = new Dictionary<Type, ScopeSetup>();
            private readonly List<TestScope> _scopes = new List<TestScope>();

            public TestScopeFactory(IList<string> journal, RecordingEventBus bus)
            {
                _journal = journal;
                _bus = bus;
            }

            public Type InitType { get; set; }

            public int MaxLiveChildScopes { get; private set; }

            public ScopeSetup SetupOf(Type state)
            {
                if (!_setups.TryGetValue(state, out ScopeSetup setup))
                {
                    setup = new ScopeSetup(state.Name.Replace("State", string.Empty), _journal, _bus);
                    _setups[state] = setup;
                }

                return setup;
            }

            public TestScope ScopeOf<TState>()
            {
                return _scopes.Last(s => s.State == typeof(TState));
            }

            public IStateScope CreateInitScope(IAssetsContext assets, ISubStateService subStates)
            {
                var scope = new TestScope(InitType, SetupOf(InitType), true, assets, subStates);
                _scopes.Add(scope);
                return scope;
            }

            public IStateScope CreateChildScope(Type baseState, IAssetsContext assets, ISubStateService subStates)
            {
                var scope = new TestScope(baseState, SetupOf(baseState), false, assets, subStates);
                _scopes.Add(scope);
                MaxLiveChildScopes = Math.Max(MaxLiveChildScopes, _scopes.Count(s => !s.IsInit && !s.Disposed));
                return scope;
            }
        }
    }
}
