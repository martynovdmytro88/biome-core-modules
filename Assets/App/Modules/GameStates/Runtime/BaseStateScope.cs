using System;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates
{
    /// <summary>
    /// Everything one base state owns: its scope, assets context, scene and sub-state service. Disposing releases all
    /// of them; the persistent Init one is never disposed.
    /// </summary>
    internal sealed class BaseStateScope
    {
        private readonly IAssetsContext _assets;
        private IStateScope _scope;
        private SceneHandle _scene;
        private bool _hasScene;
        private bool _disposed;

        public BaseStateScope(Type state, IAssetsContext assets, SubStateService subStates, bool isPersistent)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            SubStates = subStates ?? throw new ArgumentNullException(nameof(subStates));
            IsPersistent = isPersistent;
        }

        public Type State { get; }

        public bool IsPersistent { get; }

        public SubStateService SubStates { get; }

        public IGameState Instance { get; private set; }

        public IStateScenarioHandler ScenarioHandler => _scope?.ScenarioHandler;

        public void Open(IStateScopeFactory factory)
        {
            _scope = IsPersistent
                ? factory.CreateInitScope(_assets, SubStates)
                : factory.CreateChildScope(State, _assets, SubStates);
            SubStates.Bind(_scope);
            Instance = _scope.ResolveState(State);
        }

        public async UniTask LoadAsync(AssetKey? scene, CancellationToken ct)
        {
            if (scene.HasValue)
            {
                _scene = await _assets.LoadSceneAsync(scene.Value, ct);
                _hasScene = true;
            }

            if (_scope.Installer != null)
            {
                await _scope.Installer.LoadAsync(ct);
            }
        }

        public async UniTask UnloadAsync(CancellationToken ct)
        {
            try
            {
                if (_scope.Installer != null)
                {
                    await _scope.Installer.UnloadAsync(ct);
                }
            }
            finally
            {
                if (_hasScene)
                {
                    _hasScene = false;
                    await _assets.UnloadSceneAsync(_scene, ct);
                }
            }
        }

        public void Dispose()
        {
            if (IsPersistent)
            {
                throw new InvalidOperationException("The Init scope is persistent and is never disposed.");
            }

            if (_disposed)
            {
                return;
            }

            _disposed = true;
            SubStates.Release();
            try
            {
                _scope?.Dispose();
            }
            finally
            {
                _assets.Dispose();
            }
        }
    }
}
