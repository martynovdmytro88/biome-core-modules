using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Biome.Assets
{
    /// <summary>
    /// The production backend over Addressables. The only place that knows an Addressables type; keys and labels are
    /// plain strings on the way in. Nothing here counts references — one handle per call, released by the store.
    /// </summary>
    internal sealed class AddressablesBackend : IAssetBackend
    {
        private static readonly MethodInfo s_LoadTyped =
            typeof(AddressablesBackend).GetMethod(nameof(LoadTypedAsync), BindingFlags.NonPublic | BindingFlags.Static);

        public UniTask<IAssetHandle> LoadAsync(AssetKey key, Type assetType, CancellationToken ct)
        {
            if (assetType == null)
            {
                throw new ArgumentNullException(nameof(assetType));
            }

            MethodInfo load = s_LoadTyped.MakeGenericMethod(assetType);
            return (UniTask<IAssetHandle>)load.Invoke(null, new object[] { key.Value, ct });
        }

        public async UniTask<IReadOnlyList<AssetKey>> ResolveLabelAsync(AssetLabel label, CancellationToken ct)
        {
            AsyncOperationHandle<IList<IResourceLocation>> op = Addressables.LoadResourceLocationsAsync(label.Value);
            try
            {
                await op.ToUniTask(cancellationToken: ct);
                var keys = new List<AssetKey>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (IResourceLocation location in op.Result)
                {
                    if (seen.Add(location.PrimaryKey))
                    {
                        keys.Add(new AssetKey(location.PrimaryKey));
                    }
                }

                return keys;
            }
            finally
            {
                if (op.IsValid())
                {
                    Addressables.Release(op);
                }
            }
        }

        public async UniTask<ISceneInstance> LoadSceneAsync(AssetKey key, CancellationToken ct)
        {
            AsyncOperationHandle<SceneInstance> op = Addressables.LoadSceneAsync(key.Value, LoadSceneMode.Additive);
            try
            {
                await op.ToUniTask(cancellationToken: ct);
                if (op.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Assets: Addressables failed to load scene '{key}'.", op.OperationException);
                }
            }
            catch
            {
                if (op.IsValid())
                {
                    Addressables.Release(op);
                }

                throw;
            }

            return new AddressablesScene(op);
        }

        private static async UniTask<IAssetHandle> LoadTypedAsync<T>(string key, CancellationToken ct) where T : class
        {
            AsyncOperationHandle<T> op = Addressables.LoadAssetAsync<T>(key);
            try
            {
                await op.ToUniTask(cancellationToken: ct);
                if (op.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Assets: Addressables failed to load '{key}' as {typeof(T).Name}.", op.OperationException);
                }
            }
            catch
            {
                if (op.IsValid())
                {
                    Addressables.Release(op);
                }

                throw;
            }

            return new Handle<T>(op);
        }

        private sealed class AddressablesScene : ISceneInstance
        {
            private readonly AsyncOperationHandle<SceneInstance> _op;

            public AddressablesScene(AsyncOperationHandle<SceneInstance> op) => _op = op;

            public async UniTask UnloadAsync(CancellationToken ct)
            {
                if (!_op.IsValid())
                {
                    return;
                }

                AsyncOperationHandle unload = Addressables.UnloadSceneAsync(_op, true);
                await unload.ToUniTask(cancellationToken: ct);
            }
        }

        private sealed class Handle<T> : IAssetHandle where T : class
        {
            private readonly AsyncOperationHandle<T> _op;

            public Handle(AsyncOperationHandle<T> op) => _op = op;

            public object Asset => _op.Result;

            public void Release()
            {
                if (_op.IsValid())
                {
                    Addressables.Release(_op);
                }
            }
        }
    }
}
