using System;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.App
{
    /// <summary>
    /// The scenario handler of the Init scope in the game flow: a failure of Init sends the machine to
    /// <see cref="EmergencyState"/>. The machine already logs the failure and publishes the error event.
    /// </summary>
    public sealed class EmergencyScenarioHandler : IStateScenarioHandler
    {
        private readonly IGameStateMachine _machine;

        public EmergencyScenarioHandler(IGameStateMachine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// <inheritdoc />
        public UniTask HandleAsync(StateFailure failure, CancellationToken ct)
        {
            // The switch queue is busy until this handler returns, so the switch is queued, not awaited.
            _machine.SwitchAsync<EmergencyState>(ct).Forget();
            return UniTask.CompletedTask;
        }
    }
}
