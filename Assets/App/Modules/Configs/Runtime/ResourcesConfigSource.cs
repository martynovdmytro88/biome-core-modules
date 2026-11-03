using System;
using System.Threading;
using Biome.Configs.Abstract;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Biome.Configs
{
    /// <summary>
    /// Reads a config asset from a <c>Resources/</c> folder for <see cref="ConfigSourceKind.Resources"/>. Main
    /// thread only.
    /// </summary>
    public sealed class ResourcesConfigSource : IConfigSourceReader
    {
        /// <summary>
        /// Loads the asset at the source path as <typeparamref name="T"/>. Throws <see cref="ArgumentException"/>
        /// for another source kind, <see cref="InvalidOperationException"/> when no asset of that type is at the
        /// path, and <see cref="OperationCanceledException"/> on a cancelled token.
        /// </summary>
        public async UniTask<T> ReadAsync<T>(ConfigSource source, CancellationToken ct) where T : class
        {
            if (source.Kind != ConfigSourceKind.Resources)
            {
                throw new ArgumentException($"A Resources source cannot read a '{source.Kind}' source.", nameof(source));
            }

            ct.ThrowIfCancellationRequested();
            var loaded = await Resources.LoadAsync(source.Path, typeof(T)).ToUniTask(cancellationToken: ct);
            if (loaded is T asset)
            {
                return asset;
            }

            throw new InvalidOperationException($"No asset of type '{typeof(T).FullName}' at Resources path '{source.Path}'.");
        }
    }
}
