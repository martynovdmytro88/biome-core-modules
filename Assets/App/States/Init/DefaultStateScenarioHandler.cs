using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.App
{
    /// <summary>
    /// The scenario handler of the Init scope. The machine already logs the failure and publishes the error event;
    /// the reaction itself (for example a switch to Emergency) comes with the flow config.
    /// </summary>
    public sealed class DefaultStateScenarioHandler : IStateScenarioHandler
    {
        /// <inheritdoc />
        public UniTask HandleAsync(StateFailure failure, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
