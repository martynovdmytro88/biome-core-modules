using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>The reaction to a state failure; it never lives in a state body.</summary>
    public interface IStateScenarioHandler
    {
        /// <summary>Handles a failure.</summary>
        /// <param name="failure">What failed, where and why.</param>
        /// <param name="ct">Cancels the handling.</param>
        UniTask HandleAsync(StateFailure failure, CancellationToken ct);
    }
}
