using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.App
{
    /// <summary>The Emergency state: a shell with no scene; a host adds what it shows.</summary>
    public sealed class EmergencyState : IGameState
    {
        /// <inheritdoc />
        public UniTask EnterAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask ExitAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
