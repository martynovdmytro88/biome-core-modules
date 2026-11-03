using System;
using System.Collections.Generic;
using System.Threading;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates
{
    /// <summary>
    /// The sub-state switcher of one base state. The machine creates it, hands it to the scope factory and releases it
    /// with the base state's scope; a failure goes to the scenario handler of that scope.
    /// </summary>
    internal sealed class SubStateService : ISubStateService
    {
        private readonly Type _owner;
        private readonly IReadOnlyList<IStateTransition> _transitions;
        private readonly StateErrorRouter _router;
        private readonly StateSwitchLogger _logger;
        private readonly IEventPublisher _publisher;
        private readonly StateSwitchQueue _queue = new StateSwitchQueue();
        private IStateScope _scope;
        private IGameState _currentInstance;
        private bool _released;

        public SubStateService(
            Type owner,
            IReadOnlyList<IStateTransition> transitions,
            StateErrorRouter router,
            StateSwitchLogger logger,
            IEventPublisher publisher)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public Type Current { get; private set; }

        private IStateScenarioHandler Handler => _scope?.ScenarioHandler;

        public UniTask SwitchAsync<TSubState>(CancellationToken ct)
            where TSubState : IGameState
        {
            ThrowIfReleased();
            var target = typeof(TSubState);
            return _queue.EnqueueAsync(token => SwitchCoreAsync(target, token), ct);
        }

        public void Bind(IStateScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public void Release()
        {
            _released = true;
            Current = null;
            _currentInstance = null;
        }

        public async UniTask<bool> ExitCurrentAsync(CancellationToken ct)
        {
            var ok = true;
            await _queue.EnqueueAsync(async token => ok = await ExitCoreAsync(token), ct);
            return ok;
        }

        private async UniTask SwitchCoreAsync(Type target, CancellationToken ct)
        {
            ThrowIfReleased();
            var from = Current;
            if (!await ExitCoreAsync(ct))
            {
                return;
            }

            if (!await _router.RunTransitionsAsync(_transitions, from, target, StateLevel.Sub, Handler, ct))
            {
                return;
            }

            await EnterCoreAsync(target, from, ct);
        }

        private async UniTask<bool> ExitCoreAsync(CancellationToken ct)
        {
            if (_currentInstance == null)
            {
                return true;
            }

            var state = Current;
            var instance = _currentInstance;
            Current = null;
            _currentInstance = null;
            return await _router.GuardAsync(state, StateLevel.Sub, StatePhase.Stop, Handler, instance.ExitAsync, ct);
        }

        private async UniTask EnterCoreAsync(Type target, Type from, CancellationToken ct)
        {
            IGameState instance = null;
            var resolved = await _router.GuardAsync(
                target,
                StateLevel.Sub,
                StatePhase.Init,
                Handler,
                token =>
                {
                    instance = _scope.ResolveState(target);
                    return UniTask.CompletedTask;
                },
                ct);
            if (!resolved)
            {
                return;
            }

            if (!await _router.GuardAsync(target, StateLevel.Sub, StatePhase.Start, Handler, instance.EnterAsync, ct))
            {
                return;
            }

            Current = target;
            _currentInstance = instance;
            _logger.SubSwitched(_owner, from, target);
            _publisher.Publish(new StateSwitchedEvent(from, target, StateLevel.Sub));
        }

        private void ThrowIfReleased()
        {
            if (_released)
            {
                throw new ObjectDisposedException(nameof(SubStateService), "The base state of this sub-state service has left.");
            }
        }
    }
}
