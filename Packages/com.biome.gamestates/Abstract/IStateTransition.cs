using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>A pluggable step that runs between the exit of one state and the entry of the next.</summary>
    public interface IStateTransition
    {
        /// <summary>Tells whether this step runs for the given pair of states.</summary>
        /// <param name="from">The state being left.</param>
        /// <param name="to">The state being entered.</param>
        bool Applies(Type from, Type to);

        /// <summary>Runs the step.</summary>
        /// <param name="from">The state being left.</param>
        /// <param name="to">The state being entered.</param>
        /// <param name="ct">Cancels the step.</param>
        UniTask RunAsync(Type from, Type to, CancellationToken ct);
    }
}
