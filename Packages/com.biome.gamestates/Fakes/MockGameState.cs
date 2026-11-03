using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Fakes
{
    /// <summary>The one state of <see cref="MockGameStateMachine"/>; it does nothing.</summary>
    public sealed class MockGameState : IGameState
    {
        // Test logic can be added in this file: count the calls, write to a journal, hold the entry at a gate or throw.
        // See FakeGameState in this folder for a recording state that already does all of that.

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
