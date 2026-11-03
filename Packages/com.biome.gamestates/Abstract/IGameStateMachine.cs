using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>The public transition API of the base level. Main thread only.</summary>
    public interface IGameStateMachine
    {
        /// <summary>The current base state, or null before the first switch.</summary>
        Type Current { get; }

        /// <summary>Switches to a base state. Requests are queued and never interleave.</summary>
        /// <typeparam name="TState">The state to enter.</typeparam>
        /// <param name="ct">Cancels the switch.</param>
        UniTask SwitchAsync<TState>(CancellationToken ct)
            where TState : IGameState;
    }
}
