using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Assets
{
    /// <summary>
    /// A node of the context tree over the shared <see cref="RefStore"/>. Owns no assets itself — the store holds the
    /// (context, key) pairs; the context owns its children and the closing rules. Main thread only.
    /// </summary>
    internal sealed class AssetsContext : IAssetsContext
    {
        // Every key is loaded once, by the asset's own kind; the caller's T is only a view taken by Cast<T>.
        private static readonly Type s_CanonicalType = typeof(UnityEngine.Object);

        private static int s_nextSceneId;

        private readonly RefStore _store;
        private readonly IAssetBackend _backend;
        private readonly AssetsContext _parent;
        private readonly List<AssetsContext> _children = new List<AssetsContext>();
        private readonly Dictionary<SceneHandle, ISceneInstance> _scenes = new Dictionary<SceneHandle, ISceneInstance>();

        internal AssetsContext(RefStore store, IAssetBackend backend, AssetsContext parent)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _parent = parent;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsClosed { get; private set; }

        public IAssetsContext CreateChild()
        {
            ThrowIfClosed();
            var child = new AssetsContext(_store, _backend, this);
            _children.Add(child);
            return child;
        }

        public UniTask<T> LoadAsync<T>(AssetKey key, CancellationToken ct) where T : class
        {
            ThrowIfClosed();
            if (_store.TryGetHeld(Id, key, out object held))
            {
                return UniTask.FromResult(Cast<T>(key, held));
            }

            return LoadSharedAsync<T>(key, ct);
        }

        public UniTask<IReadOnlyList<T>> LoadAsync<T>(IReadOnlyList<AssetKey> keys, IProgress<float> progress, CancellationToken ct)
            where T : class
        {
            ThrowIfClosed();
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            return LoadManyAsync<T>(keys, progress, ct);
        }

        public UniTask<IReadOnlyList<AssetKey>> PreloadByLabelAsync(AssetLabel label, IProgress<float> progress, CancellationToken ct)
        {
            ThrowIfClosed();
            return PreloadAsync(label, progress, ct);
        }

        public UniTask<SceneHandle> LoadSceneAsync(AssetKey key, CancellationToken ct)
        {
            ThrowIfClosed();
            return LoadSceneCoreAsync(key, ct);
        }

        public UniTask UnloadSceneAsync(SceneHandle scene, CancellationToken ct)
        {
            if (IsClosed || !_scenes.TryGetValue(scene, out ISceneInstance instance))
            {
                return UniTask.CompletedTask;
            }

            _scenes.Remove(scene);
            return instance.UnloadAsync(ct);
        }

        public void Release(AssetKey key)
        {
            if (IsClosed)
            {
                return;
            }

            _store.Release(Id, key);
        }

        public void Unload()
        {
            if (IsClosed)
            {
                return;
            }

            IsClosed = true;
            foreach (AssetsContext child in _children.ToArray())
            {
                child.Unload();
            }

            _children.Clear();
            foreach (ISceneInstance instance in _scenes.Values)
            {
                instance.UnloadAsync(CancellationToken.None).Forget();
            }

            _scenes.Clear();
            _store.ReleaseAll(Id);
            _parent?._children.Remove(this);
        }

        public void Dispose() => Unload();

        private async UniTask<T> LoadSharedAsync<T>(AssetKey key, CancellationToken ct) where T : class
        {
            bool heldBefore = _store.TryGetHeld(Id, key, out _);
            object asset = await _store.AcquireAsync(Id, key, s_CanonicalType, ct);
            try
            {
                return Cast<T>(key, asset);
            }
            catch
            {
                // A failed view must not leave a hold this call took.
                if (!heldBefore)
                {
                    _store.Release(Id, key);
                }

                throw;
            }
        }

        private async UniTask<SceneHandle> LoadSceneCoreAsync(AssetKey key, CancellationToken ct)
        {
            ISceneInstance instance = await _backend.LoadSceneAsync(key, ct);
            if (IsClosed)
            {
                // The context closed while the scene was loading: nobody owns it any more.
                instance.UnloadAsync(CancellationToken.None).Forget();
                throw new OperationCanceledException("Assets: the context was unloaded while loading the scene.");
            }

            var handle = new SceneHandle(key, ++s_nextSceneId);
            _scenes.Add(handle, instance);
            return handle;
        }

        private async UniTask<IReadOnlyList<T>> LoadManyAsync<T>(IReadOnlyList<AssetKey> keys, IProgress<float> progress, CancellationToken ct)
            where T : class
        {
            int total = keys.Count;
            if (total == 0)
            {
                progress?.Report(1f);
                return Array.Empty<T>();
            }

            var result = new T[total];
            var done = 0;
            var tasks = new UniTask[total];
            for (var i = 0; i < total; i++)
            {
                tasks[i] = LoadOneAsync(i);
            }

            await UniTask.WhenAll(tasks);
            return result;

            async UniTask LoadOneAsync(int index)
            {
                result[index] = await LoadAsync<T>(keys[index], ct);
                progress?.Report(++done / (float)total);
            }
        }

        private async UniTask<IReadOnlyList<AssetKey>> PreloadAsync(AssetLabel label, IProgress<float> progress, CancellationToken ct)
        {
            IReadOnlyList<AssetKey> keys = await _store.ResolveLabelAsync(label, ct);
            ct.ThrowIfCancellationRequested();
            if (IsClosed)
            {
                throw new OperationCanceledException("Assets: the context was unloaded while resolving the label.");
            }

            int total = keys.Count;
            if (total == 0)
            {
                progress?.Report(1f);
                return keys;
            }

            var done = 0;
            var tasks = new UniTask[total];
            for (var i = 0; i < total; i++)
            {
                tasks[i] = PreloadOneAsync(keys[i]);
            }

            await UniTask.WhenAll(tasks);
            return keys;

            async UniTask PreloadOneAsync(AssetKey key)
            {
                if (!_store.TryGetHeld(Id, key, out _))
                {
                    await _store.AcquireAsync(Id, key, s_CanonicalType, ct);
                }

                progress?.Report(++done / (float)total);
            }
        }

        private void ThrowIfClosed()
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(IAssetsContext), $"Assets: context {Id} is closed.");
            }
        }

        private static T Cast<T>(AssetKey key, object asset) where T : class
        {
            if (asset is T typed)
            {
                return typed;
            }

            if (asset is UnityEngine.GameObject go)
            {
                Type requested = typeof(T);
                if (typeof(UnityEngine.Component).IsAssignableFrom(requested))
                {
                    var component = go.GetComponent(requested) as T;
                    if (component != null)
                    {
                        return component;
                    }
                }
                else if (requested.IsInterface)
                {
                    foreach (UnityEngine.Component candidate in go.GetComponents<UnityEngine.Component>())
                    {
                        if (candidate is T match)
                        {
                            return match;
                        }
                    }
                }
            }

            throw new InvalidCastException($"Assets: '{key}' is a {asset?.GetType().Name ?? "null"}, not a {typeof(T).Name}.");
        }
    }
}
