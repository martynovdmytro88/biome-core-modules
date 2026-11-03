using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.Assets.Fakes
{
    /// <summary>
    /// The shipped fake of the loading seam: assets are objects registered by the caller, loads complete instantly
    /// unless a <see cref="Delay"/> or a <see cref="Gate"/> says otherwise (to test cancellation), and every load /
    /// release is counted. Behind the host's core it is a working <see cref="IAssetsService"/>
    /// the same contract suite holds to the production behaviour.
    /// </summary>
    public sealed class FakeAssetBackend : IAssetBackend
    {
        private readonly Dictionary<AssetKey, object> _assets = new Dictionary<AssetKey, object>();
        private readonly Dictionary<AssetLabel, List<AssetKey>> _labels = new Dictionary<AssetLabel, List<AssetKey>>();
        private readonly Dictionary<AssetKey, UniTaskCompletionSource> _gates = new Dictionary<AssetKey, UniTaskCompletionSource>();
        private readonly Dictionary<AssetKey, int> _started = new Dictionary<AssetKey, int>();
        private readonly Dictionary<AssetKey, int> _loaded = new Dictionary<AssetKey, int>();
        private readonly Dictionary<AssetKey, int> _released = new Dictionary<AssetKey, int>();
        private readonly Dictionary<AssetKey, int> _cancelled = new Dictionary<AssetKey, int>();
        private readonly HashSet<AssetKey> _scenes = new HashSet<AssetKey>();
        private readonly Dictionary<AssetKey, int> _scenesLoaded = new Dictionary<AssetKey, int>();
        private readonly Dictionary<AssetKey, int> _scenesUnloaded = new Dictionary<AssetKey, int>();

        /// <summary>An artificial per-load delay; zero (default) completes instantly.</summary>
        public TimeSpan Delay { get; set; }

        /// <summary>Registers an asset under <paramref name="key"/>, optionally tagged with labels.</summary>
        public FakeAssetBackend Add(AssetKey key, object asset, params AssetLabel[] labels)
        {
            _assets[key] = asset ?? throw new ArgumentNullException(nameof(asset));
            foreach (AssetLabel label in labels)
            {
                if (!_labels.TryGetValue(label, out List<AssetKey> keys))
                {
                    keys = new List<AssetKey>();
                    _labels.Add(label, keys);
                }

                if (!keys.Contains(key))
                {
                    keys.Add(key);
                }
            }

            return this;
        }

        /// <summary>Registers a scene under <paramref name="key"/>; scenes carry no content, only load / unload counts.</summary>
        public FakeAssetBackend AddScene(AssetKey key)
        {
            _scenes.Add(key);
            return this;
        }

        /// <summary>Makes loads of <paramref name="key"/> (an asset or a scene) wait until <see cref="Open"/> — deterministic in-flight state.</summary>
        public void Gate(AssetKey key) => _gates[key] = new UniTaskCompletionSource();

        /// <summary>Lets the loads waiting on <paramref name="key"/>'s gate finish; later loads pass unhindered.</summary>
        public void Open(AssetKey key)
        {
            if (_gates.TryGetValue(key, out UniTaskCompletionSource gate))
            {
                _gates.Remove(key);
                gate.TrySetResult();
            }
        }

        /// <summary>Loads begun for <paramref name="key"/> (including cancelled ones).</summary>
        public int StartedCount(AssetKey key) => Count(_started, key);

        /// <summary>Loads that completed for <paramref name="key"/>.</summary>
        public int LoadedCount(AssetKey key) => Count(_loaded, key);

        /// <summary>Handles released for <paramref name="key"/>.</summary>
        public int ReleasedCount(AssetKey key) => Count(_released, key);

        /// <summary>Loads of <paramref name="key"/> that ended cancelled.</summary>
        public int CancelledCount(AssetKey key) => Count(_cancelled, key);

        /// <summary>Handles loaded and not yet released for <paramref name="key"/> — what is "in memory".</summary>
        public int LiveCount(AssetKey key) => LoadedCount(key) - ReleasedCount(key);

        /// <summary>Scene loads that completed for <paramref name="key"/>.</summary>
        public int SceneLoadedCount(AssetKey key) => Count(_scenesLoaded, key);

        /// <summary>Scene unloads that completed for <paramref name="key"/>.</summary>
        public int SceneUnloadedCount(AssetKey key) => Count(_scenesUnloaded, key);

        /// <summary>Scene instances loaded and not yet unloaded for <paramref name="key"/> — what is "open".</summary>
        public int LiveSceneCount(AssetKey key) => SceneLoadedCount(key) - SceneUnloadedCount(key);

        async UniTask<IAssetHandle> IAssetBackend.LoadAsync(AssetKey key, Type assetType, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_assets.TryGetValue(key, out object asset))
            {
                throw new KeyNotFoundException($"FakeAssetBackend: no asset registered for '{key}'.");
            }

            Increment(_started, key);
            try
            {
                if (_gates.TryGetValue(key, out UniTaskCompletionSource gate))
                {
                    await gate.Task.AttachExternalCancellation(ct);
                }
                else if (Delay > TimeSpan.Zero)
                {
                    await UniTask.Delay(Delay, DelayType.Realtime, PlayerLoopTiming.Update, ct);
                }

                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Increment(_cancelled, key);
                throw;
            }

            Increment(_loaded, key);
            return new Handle(this, key, asset);
        }

        UniTask<IReadOnlyList<AssetKey>> IAssetBackend.ResolveLabelAsync(AssetLabel label, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<AssetKey> keys = _labels.TryGetValue(label, out List<AssetKey> found)
                ? found.ToArray()
                : Array.Empty<AssetKey>();
            return UniTask.FromResult(keys);
        }

        async UniTask<ISceneInstance> IAssetBackend.LoadSceneAsync(AssetKey key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_scenes.Contains(key))
            {
                throw new KeyNotFoundException($"FakeAssetBackend: no scene registered for '{key}'.");
            }

            if (_gates.TryGetValue(key, out UniTaskCompletionSource gate))
            {
                await gate.Task.AttachExternalCancellation(ct);
            }

            ct.ThrowIfCancellationRequested();
            Increment(_scenesLoaded, key);
            return new SceneInstance(this, key);
        }

        private static int Count(Dictionary<AssetKey, int> map, AssetKey key) => map.TryGetValue(key, out int n) ? n : 0;

        private static void Increment(Dictionary<AssetKey, int> map, AssetKey key) => map[key] = Count(map, key) + 1;

        private sealed class SceneInstance : ISceneInstance
        {
            private readonly FakeAssetBackend _owner;
            private readonly AssetKey _key;
            private bool _unloaded;

            public SceneInstance(FakeAssetBackend owner, AssetKey key)
            {
                _owner = owner;
                _key = key;
            }

            public UniTask UnloadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                if (!_unloaded)
                {
                    _unloaded = true;
                    Increment(_owner._scenesUnloaded, _key);
                }

                return UniTask.CompletedTask;
            }
        }

        private sealed class Handle : IAssetHandle
        {
            private readonly FakeAssetBackend _owner;
            private readonly AssetKey _key;
            private bool _released;

            public Handle(FakeAssetBackend owner, AssetKey key, object asset)
            {
                _owner = owner;
                _key = key;
                Asset = asset;
            }

            public object Asset { get; }

            public void Release()
            {
                if (_released)
                {
                    return;
                }

                _released = true;
                Increment(_owner._released, _key);
            }
        }
    }
}
