using System.Collections;
using System.Threading;
using Biome.GameStates.Abstract;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Editor.Tests.Consumer
{
    /// <summary>
    /// Stands in for a consumer module's test assembly: it references only Abstract and Fakes, and asserts the switch its
    /// code requested through the mock machine.
    /// </summary>
    [TestFixture]
    public sealed class MockGameStateMachineConsumerTests
    {
        [UnityTest]
        public IEnumerator Consumer_TestsItsSwitchThroughTheMock_WithOnlyTheAbstractReference() => UniTask.ToCoroutine(async () =>
        {
            var machine = new MockGameStateMachine();

            await new MenuFlow(machine).StartGameAsync(CancellationToken.None);

            Assert.That(machine.Current, Is.EqualTo(typeof(ConsumerGameplayState)));
            Assert.That(machine.Switches.Count, Is.EqualTo(1));
        });

        private sealed class ConsumerGameplayState : FakeGameState
        {
        }

        private sealed class MenuFlow
        {
            private readonly IGameStateMachine _machine;

            public MenuFlow(IGameStateMachine machine)
            {
                _machine = machine;
            }

            public UniTask StartGameAsync(CancellationToken ct) => _machine.SwitchAsync<ConsumerGameplayState>(ct);
        }
    }
}
