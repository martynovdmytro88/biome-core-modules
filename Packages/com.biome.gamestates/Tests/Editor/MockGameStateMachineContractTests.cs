using System.Collections;
using System.Threading;
using Biome.GameStates.Abstract;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>Binds the machine contract to <see cref="MockGameStateMachine"/>, plus the record of its switches.</summary>
    [TestFixture]
    public sealed class MockGameStateMachineContractTests : GameStateMachineContractTests
    {
        [UnityTest]
        public IEnumerator Switches_RecordsEveryRequestInOrder() => UniTask.ToCoroutine(async () =>
        {
            var machine = new MockGameStateMachine();

            await machine.SwitchAsync<TestStateA>(CancellationToken.None);
            await machine.SwitchAsync<TestStateB>(CancellationToken.None);

            Assert.That(machine.Switches, Is.EqualTo(new[] { typeof(TestStateA), typeof(TestStateB) }));
        });

        [UnityTest]
        public IEnumerator MockState_EnterAndExit_DoNothing() => UniTask.ToCoroutine(async () =>
        {
            var machine = new MockGameStateMachine();

            await machine.State.EnterAsync(CancellationToken.None);
            await machine.State.ExitAsync(CancellationToken.None);

            Assert.That(machine.Current, Is.Null, "the state does not drive the machine");
        });

        protected override IGameStateMachine CreateMachine() => new MockGameStateMachine();
    }
}
