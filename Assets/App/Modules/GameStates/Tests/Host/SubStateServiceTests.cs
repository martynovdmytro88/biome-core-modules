using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>The sub-state level: switching inside a base state, its events, log and release.</summary>
    [TestFixture]
    public sealed class SubStateServiceTests
    {
        [UnityTest]
        public IEnumerator Switch_Sub_PublishesTheSwitchEventMarkedSub() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());

            await f.Subs.SwitchAsync<AuthSub>(default);
            await f.Subs.SwitchAsync<ShopSub>(default);

            List<StateSwitchedEvent> sub = f.H.Bus.Published<StateSwitchedEvent>().Where(e => e.Level == StateLevel.Sub).ToList();
            Assert.That(sub.Select(e => e.From), Is.EqualTo(new Type[] { null, typeof(AuthSub) }));
            Assert.That(sub.Select(e => e.To), Is.EqualTo(new[] { typeof(AuthSub), typeof(ShopSub) }));
            Assert.That(f.H.Bus.Published<StateSwitchedEvent>().Count(e => e.Level == StateLevel.Base), Is.EqualTo(2));
            Assert.That(f.Subs.Current, Is.EqualTo(typeof(ShopSub)));
        });

        [UnityTest]
        public IEnumerator Switch_Sub_ExitsTheCurrentThenEntersTheNext() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            await f.Subs.SwitchAsync<AuthSub>(default);
            f.H.Journal.Clear();

            await f.Subs.SwitchAsync<ShopSub>(default);

            Assert.That(f.H.Journal, Is.EqualTo(new[] { "Auth.Exit", "Shop.Enter" }));
        });

        [UnityTest]
        public IEnumerator Switch_Sub_IsLoggedUnderTheSubStateTag() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());

            await f.Subs.SwitchAsync<AuthSub>(default);

            string line = f.H.Log.Infos.Last();
            Assert.That(line, Does.StartWith(StateSwitchLogger.SubTag));
            Assert.That(line, Does.Contain("MenuState: none -> AuthSub"));
            Assert.That(f.H.Log.Infos.Count(l => l.StartsWith(StateSwitchLogger.BaseTag, StringComparison.Ordinal)), Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator Switch_Sub_QueuedDuringARunningSwitch_RunsAfterIt() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            f.Auth.EnterGate = new UniTaskCompletionSource();

            UniTask first = f.Subs.SwitchAsync<AuthSub>(default);
            UniTask second = f.Subs.SwitchAsync<ShopSub>(default);

            Assert.That(f.Shop.EnterCount, Is.EqualTo(0));
            f.Auth.EnterGate.TrySetResult();
            await first;
            await second;

            Assert.That(f.Auth.ExitCount, Is.EqualTo(1));
            Assert.That(f.Subs.Current, Is.EqualTo(typeof(ShopSub)));
        });

        [UnityTest]
        public IEnumerator Switch_Sub_WithAnApplyingTransition_RunsItBetweenExitAndEntry() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var transition = new RecordingStateTransition("T", journal, (from, to) => to == typeof(ShopSub));
            Fixture f = await BootAsync(new GameStateMachineTests.Harness(journal, transition));
            await f.Subs.SwitchAsync<AuthSub>(default);
            f.H.Journal.Clear();

            await f.Subs.SwitchAsync<ShopSub>(default);

            Assert.That(f.H.Journal, Is.EqualTo(new[] { "Auth.Exit", "T.Run", "Shop.Enter" }));
            Assert.That(transition.Runs.Single(), Is.EqualTo((typeof(AuthSub), typeof(ShopSub))));
        });

        [UnityTest]
        public IEnumerator Switch_AwayFromTheBaseState_ExitsTheCurrentSubState() => UniTask.ToCoroutine(async () =>
        {
            Fixture f = await BootAsync(new GameStateMachineTests.Harness());
            f.H.AddBase(new GameStateMachineTests.GameplayState(f.H.Journal));
            await f.Subs.SwitchAsync<AuthSub>(default);

            await f.H.Machine.SwitchAsync<GameStateMachineTests.GameplayState>(default);

            Assert.That(f.Auth.ExitCount, Is.EqualTo(1));
            Assert.That(f.Subs.Current, Is.Null);
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
    }
}
