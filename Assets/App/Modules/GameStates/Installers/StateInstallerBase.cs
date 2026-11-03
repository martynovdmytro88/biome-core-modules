using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Installers
{
    /// <summary>
    /// The installer of one base state: it loads its own content first, then its dependents in declared order, and unloads
    /// in reverse (dependents last to first, then itself). A concrete installer declares its pieces and never orders them.
    /// Main thread only.
    /// </summary>
    public abstract class StateInstallerBase : IStateInstaller
    {
        /// <summary>The installers that load after this one, in load order; unloaded in reverse.</summary>
        protected abstract IReadOnlyList<IStateInstaller> Dependents { get; }

        /// <inheritdoc />
        public async UniTask LoadAsync(CancellationToken ct)
        {
            await LoadSelfAsync(ct);
            var dependents = Dependents;
            for (var i = 0; i < dependents.Count; i++)
            {
                await dependents[i].LoadAsync(ct);
            }
        }

        /// <inheritdoc />
        public async UniTask UnloadAsync(CancellationToken ct)
        {
            try
            {
                var dependents = Dependents;
                for (var i = dependents.Count - 1; i >= 0; i--)
                {
                    await dependents[i].UnloadAsync(ct);
                }
            }
            finally
            {
                await UnloadSelfAsync(ct);
            }
        }

        /// <summary>Loads this installer's own content; runs before the dependents.</summary>
        /// <param name="ct">Cancels the load.</param>
        protected virtual UniTask LoadSelfAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>Unloads what <see cref="LoadSelfAsync"/> loaded; runs after the dependents are unloaded.</summary>
        /// <param name="ct">Cancels the unload.</param>
        protected virtual UniTask UnloadSelfAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
