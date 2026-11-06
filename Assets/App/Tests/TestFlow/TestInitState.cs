using System;
using System.Threading;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.App.Tests
{
    /// <summary>
    /// The first state of the test flow: it reports its completion on the bus and goes nowhere on its own; the flow leaves it
    /// only for <see cref="EmergencyState"/> on a failure.
    /// </summary>
    public sealed class TestInitState : IGameState
    {
        private readonly IEventPublisher _events;

        public TestInitState(IEventPublisher events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc />
        public UniTask EnterAsync(CancellationToken ct)
        {
            _events.Publish(new StateCompletedEvent(typeof(TestInitState)));
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask ExitAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
