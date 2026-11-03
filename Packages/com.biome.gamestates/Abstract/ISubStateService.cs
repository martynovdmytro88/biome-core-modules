using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>
    /// The sub-state switcher of one base state. It lives in the scope of that base state and is released with it.
    /// Main thread only.
    /// </summary>
    public interface ISubStateService
    {
        /// <summary>The current sub-state, or null before the first switch.</summary>
        Type Current { get; }

        /// <summary>Switches to a sub-state of the owning base state.</summary>
        /// <typeparam name="TSubState">The sub-state to enter.</typeparam>
        /// <param name="ct">Cancels the switch.</param>
        UniTask SwitchAsync<TSubState>(CancellationToken ct)
            where TSubState : IGameState;
    }
}
