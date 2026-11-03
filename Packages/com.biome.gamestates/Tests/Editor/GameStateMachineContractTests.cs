using System.Collections;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>
    /// The contract of <see cref="IGameStateMachine"/> as a consumer sees it: the current state and the order of queued
    /// switches. Every test starts from <see cref="TestStateA"/> and moves on to <see cref="TestStateB"/>, then
    /// <see cref="TestStateC"/>, so a host whose machine needs an Init state first maps A to it.
    /// </summary>
    public abstract class GameStateMachineContractTests
    {
        private IGameStateMachine _machine;

        [SetUp]
        public void SetUpMachine() => _machine = CreateMachine();

        [Test]
        public void Current_BeforeTheFirstSwitch_IsNull()
        {
            Assert.That(_machine.Current, Is.Null);
        }

        [UnityTest]
        public IEnumerator SwitchAsync_FirstState_BecomesCurrent() => UniTask.ToCoroutine(async () =>
        {
            await _machine.SwitchAsync<TestStateA>(CancellationToken.None);

            Assert.That(_machine.Current, Is.EqualTo(typeof(TestStateA)));
        });

        [UnityTest]
        public IEnumerator SwitchAsync_OneAfterAnother_CurrentFollows() => UniTask.ToCoroutine(async () =>
        {
            await _machine.SwitchAsync<TestStateA>(CancellationToken.None);
            await _machine.SwitchAsync<TestStateB>(CancellationToken.None);
            Assert.That(_machine.Current, Is.EqualTo(typeof(TestStateB)));

            await _machine.SwitchAsync<TestStateC>(CancellationToken.None);

            Assert.That(_machine.Current, Is.EqualTo(typeof(TestStateC)));
        });

        [UnityTest]
        public IEnumerator SwitchAsync_RequestedWithoutWaiting_RunInRequestOrder() => UniTask.ToCoroutine(async () =>
        {
            UniTask first = _machine.SwitchAsync<TestStateA>(CancellationToken.None);
            UniTask second = _machine.SwitchAsync<TestStateB>(CancellationToken.None);
            UniTask third = _machine.SwitchAsync<TestStateC>(CancellationToken.None);

            await UniTask.WhenAll(first, second, third);

            Assert.That(_machine.Current, Is.EqualTo(typeof(TestStateC)), "the last request ends last");
        });

        /// <summary>A fresh machine under test, before its first switch.</summary>
        protected abstract IGameStateMachine CreateMachine();
    }
}
