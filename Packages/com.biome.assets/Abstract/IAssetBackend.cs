using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Assets.Abstract
{
    /// <summary>
    /// The module's seam: what actually loads. The reference-counting store sits above it and never touches
    /// Addressables. Implementations: the host's production backend and the shipped fake.
    /// </summary>
    public interface IAssetBackend
    {
        /// <summary>Loads one asset; cancelling <paramref name="ct"/> cancels the load (throws <see cref="OperationCanceledException"/>).</summary>
        UniTask<IAssetHandle> LoadAsync(AssetKey key, Type assetType, CancellationToken ct);

        /// <summary>The keys tagged with <paramref name="label"/>; empty when none.</summary>
        UniTask<IReadOnlyList<AssetKey>> ResolveLabelAsync(AssetLabel label, CancellationToken ct);

        /// <summary>Loads one scene additively; cancelling <paramref name="ct"/> cancels the load and leaves no scene behind.</summary>
        UniTask<ISceneInstance> LoadSceneAsync(AssetKey key, CancellationToken ct);
    }

    /// <summary>One loaded scene instance; unloaded once, by the context that owns it.</summary>
    public interface ISceneInstance
    {
        UniTask UnloadAsync(CancellationToken ct);
    }

    /// <summary>One handle per key; released once, when the last context lets go.</summary>
    public interface IAssetHandle
    {
        object Asset { get; }

        void Release();
    }
}
