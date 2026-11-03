using System;
using System.Threading;
using Biome.GameStates.Abstract;
using Biome.GameStates.Config;
using Cysharp.Threading.Tasks;

namespace Biome.App
{
    /// <summary>
    /// Starts the state flow of the app: takes the first state the flow names and switches the machine to it. The same
    /// for every flow; what the first state does and where it leads is its own business. Nothing calls it when the
    /// scope is built; the entry point that starts the game (and a test) resolves it from the app scope and awaits
    /// <see cref="RunAsync"/> with its own token.
    /// </summary>
    public sealed class StateFlowStarter
    {
        private readonly IGameStateMachine _machine;
        private readonly GameStatesFlowConfig _flow;

        /// <summary>Creates the starter over the machine and the flow it runs.</summary>
        /// <exception cref="ArgumentNullException">A dependency is null.</exception>
        public StateFlowStarter(IGameStateMachine machine, GameStatesFlowConfig flow)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
        }

        /// <summary>
        /// Completes when the machine entered the first state of the flow or the token was cancelled. Throws
        /// <see cref="InvalidOperationException"/> when the flow names no known first state.
        /// </summary>
        /// <param name="ct">Cancels the run.</param>
        public UniTask RunAsync(CancellationToken ct)
        {
            var first = _flow.ResolveInitState()
                ?? throw new InvalidOperationException($"Flow '{_flow.Name}': the first state '{_flow.InitState}' is blank or is not a known type.");
            return SwitchToAsync(first, ct);
        }

        // The machine contract is generic only; the first state is data, so the call is closed over the resolved type.
        private UniTask SwitchToAsync(Type state, CancellationToken ct)
        {
            var method = typeof(IGameStateMachine).GetMethod(nameof(IGameStateMachine.SwitchAsync));
            return (UniTask)method.MakeGenericMethod(state).Invoke(_machine, new object[] { ct });
        }
    }
}
