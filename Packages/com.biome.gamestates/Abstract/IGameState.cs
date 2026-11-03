using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>
    /// A base state or a sub-state. The level is the owner's (the machine or the sub-state service), not the state's.
    /// Main thread only.
    /// </summary>
    public interface IGameState
    {
        /// <summary>Enters the state: the machine drives Init, Load and Start.</summary>
        /// <param name="ct">Cancels the entry.</param>
        UniTask EnterAsync(CancellationToken ct);

        /// <summary>Exits the state: the machine drives Stop, Unload and Dispose.</summary>
        /// <param name="ct">Cancels the exit.</param>
        UniTask ExitAsync(CancellationToken ct);
    }
}
