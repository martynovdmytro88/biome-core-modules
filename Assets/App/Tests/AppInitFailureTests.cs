using System;
using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Biome.App.Tests
{
    /// <summary>
    /// The reaction of the game flow to a failed Init state: the Init scope's handler asks the machine for the
    /// Emergency state and returns at once, because the switch queue is busy until the handler returns.
    /// </summary>
    public sealed class AppInitFailureTests
    {
        private sealed class RecordingMachine : IGameStateMachine
        {
            internal List<Type> Requested { get; } = new List<Type>();

            public Type Current => null;

            public UniTask SwitchAsync<TState>(CancellationToken ct)
                where TState : IGameState
            {
                Requested.Add(typeof(TState));
                return UniTask.CompletedTask;
            }
        }

        private static StateFailure InitFailure() =>
            new StateFailure(typeof(InitState), StateLevel.Base, StatePhase.Start, new InvalidOperationException("load failed"));

        [Test]
        public void HandleAsync_InitFailed_RequestsTheEmergencyState()
        {
            var machine = new RecordingMachine();
            var handler = new DefaultStateScenarioHandler(machine);

            handler.HandleAsync(InitFailure(), CancellationToken.None);

            Assert.That(machine.Requested, Is.EqualTo(new[] { typeof(EmergencyState) }));
        }

        [Test]
        public void HandleAsync_InitFailed_ReturnsACompletedTask()
        {
            var handler = new DefaultStateScenarioHandler(new RecordingMachine());

            var task = handler.HandleAsync(InitFailure(), CancellationToken.None);

            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded), "the handler must not await the switch");
        }

        [Test]
        public void Constructor_NullMachine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultStateScenarioHandler(null));
        }
    }
}
