using System;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates
{
    /// <summary>
    /// The orchestrator of the base level: one child scope per base state under the persistent Init scope, never two at
    /// once. A switch runs the exit of the current state, the applicable transitions, then the entry of the next.
    /// A phase failure is routed (log, the handler of the Init scope, the error event) and ends the switch with no
    /// current state; a scenario handler must not await a machine switch (start it with Forget instead), the queue is
    /// busy until the handler returns. Main thread only.
    /// </summary>
    public sealed class GameStateMachine : IGameStateMachine
    {
        private readonly IStateScopeFactory _factory;
        private readonly IBaseStateDescriptorProvider _descriptors;
        private readonly IAssetsContext _rootAssets;
        private readonly IEventPublisher _publisher;
        private readonly IReadOnlyList<IStateTransition> _transitions;
        private readonly StateErrorRouter _router;
        private readonly StateSwitchLogger _logger;
        private readonly StateSwitchQueue _queue = new StateSwitchQueue();
        private BaseStateScope _initScope;
        private BaseStateScope _current;
        private bool _initStarted;

        /// <summary>Creates the machine.</summary>
        /// <param name="factory">Creates the scope of each base state.</param>
        /// <param name="descriptors">Tells which state is Init and where the scenes are.</param>
        /// <param name="rootAssets">The root assets context; every base state gets a child of it.</param>
        /// <param name="publisher">Publishes the switch and error events.</param>
        /// <param name="logSink">Receives the tagged switch lines.</param>
        /// <param name="transitions">The pluggable steps between exit and entry; may be null.</param>
        public GameStateMachine(
            IStateScopeFactory factory,
            IBaseStateDescriptorProvider descriptors,
            IAssetsContext rootAssets,
            IEventPublisher publisher,
            IStateLogSink logSink,
            IReadOnlyList<IStateTransition> transitions)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            _rootAssets = rootAssets ?? throw new ArgumentNullException(nameof(rootAssets));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _transitions = transitions ?? Array.Empty<IStateTransition>();
            _logger = new StateSwitchLogger(logSink);
            _router = new StateErrorRouter(_publisher, _logger);
        }

        /// <inheritdoc />
        public Type Current => _current?.State;

        private IStateScenarioHandler InitHandler => _initScope?.ScenarioHandler;

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Surfaces through the task: the Init state is requested twice, or before it any other state.
        /// </exception>
        public UniTask SwitchAsync<TState>(CancellationToken ct)
            where TState : IGameState
        {
            var target = typeof(TState);
            return _queue.EnqueueAsync(token => SwitchCoreAsync(target, token), ct);
        }

        private async UniTask SwitchCoreAsync(Type target, CancellationToken ct)
        {
            var isInit = target == _descriptors.InitState;
            if (isInit && _initStarted)
            {
                throw new InvalidOperationException($"{target.Name} is the Init state and is entered once.");
            }

            if (!isInit && !_initStarted)
            {
                throw new InvalidOperationException($"{target.Name} was requested before the Init state.");
            }

            var from = _current?.State;
            if (_current != null)
            {
                var leaving = _current;
                _current = null;
                if (!await ExitAsync(leaving, ct))
                {
                    return;
                }
            }

            if (!await _router.RunTransitionsAsync(_transitions, from, target, StateLevel.Base, InitHandler, ct))
            {
                return;
            }

            await EnterAsync(target, from, isInit, ct);
        }

        private async UniTask<bool> ExitAsync(BaseStateScope scope, CancellationToken ct)
        {
            var ok = true;
            try
            {
                if (!await scope.SubStates.ExitCurrentAsync(ct))
                {
                    ok = false;
                }

                if (!await _router.GuardAsync(scope.State, StateLevel.Base, StatePhase.Stop, InitHandler, scope.Instance.ExitAsync, ct))
                {
                    ok = false;
                }

                if (!scope.IsPersistent
                    && !await _router.GuardAsync(scope.State, StateLevel.Base, StatePhase.Unload, InitHandler, scope.UnloadAsync, ct))
                {
                    ok = false;
                }
            }
            finally
            {
                if (!scope.IsPersistent)
                {
                    var disposed = await _router.GuardAsync(
                        scope.State,
                        StateLevel.Base,
                        StatePhase.Dispose,
                        InitHandler,
                        token =>
                        {
                            scope.Dispose();
                            return UniTask.CompletedTask;
                        },
                        CancellationToken.None);
                    if (!disposed)
                    {
                        ok = false;
                    }
                }
            }

            return ok;
        }

        private async UniTask EnterAsync(Type target, Type from, bool isInit, CancellationToken ct)
        {
            if (isInit)
            {
                _initStarted = true;
            }

            BaseStateScope scope = null;
            var loaded = false;
            var entered = false;
            try
            {
                var opened = await _router.GuardAsync(
                    target,
                    StateLevel.Base,
                    StatePhase.Init,
                    InitHandler,
                    token =>
                    {
                        scope = OpenScope(target, isInit);
                        return UniTask.CompletedTask;
                    },
                    ct);
                if (!opened)
                {
                    return;
                }

                loaded = await _router.GuardAsync(
                    target,
                    StateLevel.Base,
                    StatePhase.Load,
                    InitHandler,
                    token => scope.LoadAsync(ResolveScene(target, isInit), token),
                    ct);
                if (!loaded)
                {
                    return;
                }

                if (!await _router.GuardAsync(target, StateLevel.Base, StatePhase.Start, InitHandler, scope.Instance.EnterAsync, ct))
                {
                    return;
                }

                _current = scope;
                entered = true;
                _logger.BaseSwitched(from, target);
                _publisher.Publish(new StateSwitchedEvent(from, target, StateLevel.Base));
            }
            finally
            {
                if (!entered)
                {
                    await DiscardAsync(scope, loaded);
                }
            }
        }

        private BaseStateScope OpenScope(Type target, bool isInit)
        {
            var subStates = new SubStateService(target, _transitions, _router, _logger, _publisher);
            var scope = new BaseStateScope(target, _rootAssets.CreateChild(), subStates, isInit);
            if (isInit)
            {
                _initScope = scope;
            }

            try
            {
                scope.Open(_factory);
            }
            catch
            {
                if (!isInit)
                {
                    scope.Dispose();
                }

                throw;
            }

            return scope;
        }

        private AssetKey? ResolveScene(Type target, bool isInit)
        {
            if (isInit)
            {
                return null;
            }

            return _descriptors.TryGetScene(target, out var sceneKey) ? sceneKey : (AssetKey?)null;
        }

        private async UniTask DiscardAsync(BaseStateScope scope, bool loaded)
        {
            if (scope == null || scope.IsPersistent)
            {
                return;
            }

            if (loaded)
            {
                try
                {
                    await scope.UnloadAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.CleanupFailed(scope.State, exception);
                }
            }

            try
            {
                scope.Dispose();
            }
            catch (Exception exception)
            {
                _logger.CleanupFailed(scope.State, exception);
            }
        }
    }
}
