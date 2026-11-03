using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Abstract
{
    /// <summary>One installer for a state: it loads and unloads its content. Load runs base first, unload in reverse.</summary>
    public interface IStateInstaller
    {
        /// <summary>Loads the content the state needs.</summary>
        /// <param name="ct">Cancels the load.</param>
        UniTask LoadAsync(CancellationToken ct);

        /// <summary>Unloads what <see cref="LoadAsync"/> loaded.</summary>
        /// <param name="ct">Cancels the unload.</param>
        UniTask UnloadAsync(CancellationToken ct);
    }
}
