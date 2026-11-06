using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>Failure routing: sub-state to its base state's handler, base state to the Init handler, then the error event.</summary>
    [TestFixture]
    public sealed class StateErrorRoutingTests
    {
        [UnityTest]
        public IEnumerator Route_SubEnterFails_ReachesBaseHandlerThenErrorEvent() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            f.Auth.EnterFailure = new InvalidOperationException("boom");

            await f.Subs.SwitchAsync<AuthSub>(default);

            GameStateMachineTests.RecordingHandler handler = f.H.SetupOf<GameStateMachineTests.MenuState>().Handler;
            StateFailure failure = handler.Failures.Single();
            Assert.That(failure.State, Is.EqualTo(typeof(AuthSub)));
            Assert.That(failure.Level, Is.EqualTo(StateLevel.Sub));
            Assert.That(failure.Phase, Is.EqualTo(StatePhase.Start));
            Assert.That(handler.ErrorEventsAtCall.Single(), Is.EqualTo(0));
            StateErrorEvent error = f.H.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.State, Is.EqualTo(typeof(AuthSub)));
            Assert.That(error.Level, Is.EqualTo(StateLevel.Sub));
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Start));
            Assert.That(error.Exception.StackTrace, Is.Not.Null.And.Not.Empty);
            Assert.That(f.H.SetupOf<GameStateMachineTests.InitState>().Handler.Failures, Is.Empty);
            Assert.That(f.Subs.Current, Is.Null);
        });

        [UnityTest]
        public IEnumerator Route_SubExitFails_UsesTheStopPhase() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            await f.Subs.SwitchAsync<AuthSub>(default);
            f.Auth.ExitFailure = new InvalidOperationException("boom");

            await f.Subs.SwitchAsync<ShopSub>(default);

            StateErrorEvent error = f.H.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.State, Is.EqualTo(typeof(AuthSub)));
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Stop));
            Assert.That(f.Shop.EnterCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Route_SubNotResolvable_UsesTheInitPhase() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());

            await f.Subs.SwitchAsync<UnregisteredSub>(default);

            StateFailure failure = f.H.SetupOf<GameStateMachineTests.MenuState>().Handler.Failures.Single();
            Assert.That(failure.State, Is.EqualTo(typeof(UnregisteredSub)));
            Assert.That(failure.Phase, Is.EqualTo(StatePhase.Init));
        });

        [UnityTest]
        public IEnumerator Route_SubTransitionFails_UsesTheTransitionPhase() => UniTask.ToCoroutine(async () =>
        {
            var transition = new RecordingStateTransition(applies: (from, to) => to == typeof(ShopSub))
            {
                Failure = new InvalidOperationException("boom"),
            };
            Fixture f = await BootAsync(new GameStateMachineTests.Harness(null, transition));
            await f.Subs.SwitchAsync<AuthSub>(default);

            await f.Subs.SwitchAsync<ShopSub>(default);

            StateErrorEvent error = f.H.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.State, Is.EqualTo(typeof(ShopSub)));
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Transition));
            Assert.That(f.Shop.EnterCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Route_SubFailure_IsLoggedUnderTheSubStateTag() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            f.Auth.EnterFailure = new InvalidOperationException("boom");

            await f.Subs.SwitchAsync<AuthSub>(default);

            Assert.That(f.H.Log.Errors.Single(), Does.StartWith(StateSwitchLogger.SubTag));
        });

        [UnityTest]
        public IEnumerator Route_BaseEnterFails_ReachesInitHandlerThenErrorEvent() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            GameStateMachineTests.MenuState menu = h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            menu.EnterFailure = new InvalidOperationException("boom");
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);

            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);

            GameStateMachineTests.RecordingHandler initHandler = h.SetupOf<GameStateMachineTests.InitState>().Handler;
            StateFailure failure = initHandler.Failures.Single();
            Assert.That(failure.State, Is.EqualTo(typeof(GameStateMachineTests.MenuState)));
            Assert.That(failure.Level, Is.EqualTo(StateLevel.Base));
            Assert.That(failure.Phase, Is.EqualTo(StatePhase.Start));
            Assert.That(initHandler.ErrorEventsAtCall.Single(), Is.EqualTo(0));
            StateErrorEvent error = h.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Start));
            Assert.That(error.Exception.StackTrace, Is.Not.Null.And.Not.Empty);
            Assert.That(h.SetupOf<GameStateMachineTests.MenuState>().Handler.Failures, Is.Empty);
            Assert.That(h.Log.Errors.Single(), Does.StartWith(StateSwitchLogger.BaseTag));
        });

        [UnityTest]
        public IEnumerator Route_BaseEnterFails_ReleasesTheScopeAndLeavesNoCurrent() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            GameStateMachineTests.MenuState menu = h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            menu.EnterFailure = new InvalidOperationException("boom");
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);

            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);

            GameStateMachineTests.TestScope scope = h.Factory.ScopeOf<GameStateMachineTests.MenuState>();
            Assert.That(scope.Disposed, Is.True);
            Assert.That(scope.Assets.IsClosed, Is.True);
            Assert.That(h.Backend.LiveSceneCount(GameStateMachineTests.Harness.SceneOf<GameStateMachineTests.MenuState>()), Is.EqualTo(0));
            Assert.That(h.Machine.Current, Is.Null);
            Assert.That(h.Journal, Does.Contain("Menu.Uninstall"));
        });

        [UnityTest]
        public IEnumerator Route_BaseInstallerFails_UsesTheLoadPhase() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            h.SetupOf<GameStateMachineTests.MenuState>().Installer.LoadFailure = new InvalidOperationException("boom");
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);

            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);

            StateErrorEvent error = h.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.State, Is.EqualTo(typeof(GameStateMachineTests.MenuState)));
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Load));
        });

        [UnityTest]
        public IEnumerator Route_BaseExitFails_StillReleasesTheScope() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            GameStateMachineTests.MenuState menu = h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            GameStateMachineTests.GameplayState gameplay = h.AddBase(new GameStateMachineTests.GameplayState(h.Journal));
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);
            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);
            menu.ExitFailure = new InvalidOperationException("boom");

            await h.Machine.SwitchAsync<GameStateMachineTests.GameplayState>(default);

            StateErrorEvent error = h.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Stop));
            Assert.That(h.Factory.ScopeOf<GameStateMachineTests.MenuState>().Disposed, Is.True);
            Assert.That(gameplay.EnterCount, Is.EqualTo(0));
            Assert.That(h.Machine.Current, Is.Null);
        });

        [UnityTest]
        public IEnumerator Route_BaseTransitionFails_UsesTheTransitionPhase() => UniTask.ToCoroutine(async () =>
        {
            var transition = new RecordingStateTransition(applies: (from, to) => to == typeof(GameStateMachineTests.MenuState))
            {
                Failure = new InvalidOperationException("boom"),
            };
            var h = new GameStateMachineTests.Harness(null, transition);
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            GameStateMachineTests.MenuState menu = h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);

            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);

            StateErrorEvent error = h.Bus.Published<StateErrorEvent>().Single();
            Assert.That(error.Phase, Is.EqualTo(StatePhase.Transition));
            Assert.That(menu.EnterCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Route_HandlerThrows_ErrorEventIsStillPublished() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            f.Auth.EnterFailure = new InvalidOperationException("boom");
            f.H.SetupOf<GameStateMachineTests.MenuState>().Handler.Failure = new InvalidOperationException("handler");

            await f.Subs.SwitchAsync<AuthSub>(default);

            Assert.That(f.H.Bus.Published<StateErrorEvent>(), Has.Count.EqualTo(1));
            Assert.That(f.H.Log.Errors, Has.Count.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator Route_NoHandler_ErrorEventIsStillPublished() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            GameStateMachineTests.MenuState menu = h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            menu.EnterFailure = new InvalidOperationException("boom");
            h.SetupOf<GameStateMachineTests.InitState>().Handler = null;
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);

            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);

            Assert.That(h.Bus.Published<StateErrorEvent>(), Has.Count.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Route_CancelledSwitch_IsNotAFailure() => UniTask.ToCoroutine(async () =>
        {
            var h = new GameStateMachineTests.Harness();
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await GameStateMachineTests.Harness.AssertThrowsAsync<OperationCanceledException>(
                    () => h.Machine.SwitchAsync<GameStateMachineTests.InitState>(cts.Token));
            }

            Assert.That(h.Bus.Published<StateErrorEvent>(), Is.Empty);
            Assert.That(h.Log.Errors, Is.Empty);
        });

        private static async UniTask<Fixture> BootAsync(GameStateMachineTests.Harness h)
        {
            h.AddInit(new GameStateMachineTests.InitState(h.Journal));
            h.AddBase(new GameStateMachineTests.MenuState(h.Journal));
            AuthSub auth = h.AddSub<GameStateMachineTests.MenuState, AuthSub>(new AuthSub(h.Journal));
            ShopSub shop = h.AddSub<GameStateMachineTests.MenuState, ShopSub>(new ShopSub(h.Journal));
            await h.Machine.SwitchAsync<GameStateMachineTests.InitState>(default);
            await h.Machine.SwitchAsync<GameStateMachineTests.MenuState>(default);
            return new Fixture(h, h.Factory.ScopeOf<GameStateMachineTests.MenuState>().SubStates, auth, shop);
        }

        private sealed class Fixture
        {
            public Fixture(GameStateMachineTests.Harness h, ISubStateService subs, AuthSub auth, ShopSub shop)
            {
                H = h;
                Subs = subs;
                Auth = auth;
                Shop = shop;
            }

            public GameStateMachineTests.Harness H { get; }

            public ISubStateService Subs { get; }

            public AuthSub Auth { get; }

            public ShopSub Shop { get; }
        }

        private sealed class AuthSub : FakeGameState
        {
            public AuthSub(IList<string> journal)
                : base("Auth", journal)
            {
            }
        }

        private sealed class ShopSub : FakeGameState
        {
            public ShopSub(IList<string> journal)
                : base("Shop", journal)
            {
            }
        }

        private sealed class UnregisteredSub : FakeGameState
        {
        }
    }
}
