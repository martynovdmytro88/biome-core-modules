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
    /// Guards a phase and routes its failure: log, then the scenario handler, then the error event. A cancellation is not
    /// a failure and passes through.
    /// </summary>
    internal sealed class StateErrorRouter
    {
        private readonly IEventPublisher _publisher;
        private readonly StateSwitchLogger _logger;

        public StateErrorRouter(IEventPublisher publisher, StateSwitchLogger logger)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async UniTask<bool> GuardAsync(
            Type state,
            StateLevel level,
            StatePhase phase,
            IStateScenarioHandler handler,
            Func<CancellationToken, UniTask> action,
            CancellationToken ct)
        {
            try
            {
                await action(ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                await RouteAsync(new StateFailure(state, level, phase, exception), handler, ct);
                return false;
            }
        }

        public async UniTask<bool> RunTransitionsAsync(
            IReadOnlyList<IStateTransition> transitions,
            Type from,
            Type to,
            StateLevel level,
            IStateScenarioHandler handler,
            CancellationToken ct)
        {
            for (var i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if (!transition.Applies(from, to))
                {
                    continue;
                }

                var ok = await GuardAsync(to, level, StatePhase.Transition, handler, token => transition.RunAsync(from, to, token), ct);
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        private async UniTask RouteAsync(StateFailure failure, IStateScenarioHandler handler, CancellationToken ct)
        {
            _logger.Failed(failure);
            try
            {
                if (handler != null)
                {
                    await handler.HandleAsync(failure, ct);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception handlerException)
            {
                _logger.HandlerFailed(failure, handlerException);
            }
            finally
            {
                _publisher.Publish(new StateErrorEvent(failure.State, failure.Level, failure.Phase, failure.Exception));
            }
        }
    }
}
