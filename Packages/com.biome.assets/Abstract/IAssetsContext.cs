using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.Assets.Abstract
{
    /// <summary>
    /// A lifetime scope for assets: whatever it loads stays in memory until it lets go. The pair
    /// (context, key) is a <b>set</b> — loading a key twice in one context holds one reference, and one
    /// <see cref="Release"/> drops it. An asset leaves memory when no context holds it; the same key held by several
    /// contexts is loaded once.
    /// <para>
    /// A load already in flight for a key is shared. One waiter's token cancelling does not cancel the shared load;
    /// it is cancelled when its last waiter is gone. Loading a held key completes synchronously.
    /// </para>
    /// <para>
    /// A closed context throws <see cref="ObjectDisposedException"/> on both <c>LoadAsync</c> overloads,
    /// <see cref="PreloadByLabelAsync"/>, <see cref="LoadSceneAsync"/> and <see cref="CreateChild"/>; <see cref="Release"/>, <see cref="Unload"/> and <see cref="Dispose"/> on a closed
    /// context are no-ops. Main thread only.
    /// </para>
    /// </summary>
    public interface IAssetsContext : IDisposable
    {
        /// <summary>A unique id of this context.</summary>
        Guid Id { get; }

        /// <summary>True after <see cref="Unload"/> / <see cref="Dispose"/>.</summary>
        bool IsClosed { get; }

        /// <summary>A window / screen / tab context; closed with its parent.</summary>
        /// <exception cref="ObjectDisposedException">This context is closed.</exception>
        IAssetsContext CreateChild();

        /// <summary>Loads and holds the asset under <paramref name="key"/> as <typeparamref name="T"/>.</summary>
        /// <exception cref="ObjectDisposedException">This context is closed; thrown synchronously.</exception>
        /// <exception cref="InvalidCastException">
        /// The asset is not a <typeparamref name="T"/>, or, for a <c>GameObject</c>, it has no <typeparamref name="T"/> component.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled.</exception>
        UniTask<T> LoadAsync<T>(AssetKey key, CancellationToken ct) where T : class;

        /// <summary>
        /// Loads every key (concurrently) and returns the assets in key order. <paramref name="progress"/> (may be
        /// null) gets the finished fraction, 0..1, after each key completes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This context is closed; thrown synchronously.</exception>
        /// <exception cref="InvalidCastException">
        /// An asset is not a <typeparamref name="T"/>, or, for a <c>GameObject</c>, it has no <typeparamref name="T"/> component.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled.</exception>
        UniTask<IReadOnlyList<T>> LoadAsync<T>(IReadOnlyList<AssetKey> keys, IProgress<float> progress, CancellationToken ct)
            where T : class;

        /// <summary>Loads and holds every asset tagged with <paramref name="label"/>; returns their keys.</summary>
        /// <exception cref="ObjectDisposedException">This context is closed; thrown synchronously.</exception>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="ct"/> is cancelled, or this context was unloaded while the label was being resolved.
        /// </exception>
        UniTask<IReadOnlyList<AssetKey>> PreloadByLabelAsync(AssetLabel label, IProgress<float> progress, CancellationToken ct);

        /// <summary>
        /// Loads the scene under <paramref name="key"/> additively and owns it: the scene is unloaded with this
        /// context. Every call loads one new instance with its own handle (scenes are not shared or ref-counted).
        /// </summary>
        /// <exception cref="ObjectDisposedException">This context is closed; thrown synchronously.</exception>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="ct"/> is cancelled, or this context was unloaded while the scene was loading (the scene is
        /// then unloaded again).
        /// </exception>
        UniTask<SceneHandle> LoadSceneAsync(AssetKey key, CancellationToken ct);

        /// <summary>
        /// Unloads a scene this context loaded. A handle this context does not own (already unloaded, another
        /// context's, default) is a no-op, as is any call on a closed context.
        /// </summary>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled.</exception>
        UniTask UnloadSceneAsync(SceneHandle scene, CancellationToken ct);

        /// <summary>Drops this context's reference to <paramref name="key"/>; idempotent.</summary>
        void Release(AssetKey key);

        /// <summary>
        /// Children first, then self; cancels pending waiters; releases every held key; unloads every scene this
        /// context loaded (not awaited — the unloads finish in the following frames).
        /// </summary>
        void Unload();
    }
}
