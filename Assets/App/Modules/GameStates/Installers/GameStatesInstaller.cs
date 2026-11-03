using System;
using System.Collections.Generic;
using Biome.Assets.Abstract;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using VContainer;
using VContainer.Unity;

namespace Biome.GameStates.Installers
{
    /// <summary>
    /// Registers the state machine in the app scope as <see cref="IGameStateMachine"/> and the scope factory under it.
    /// Each base state is declared with <c>AddState</c>: its own scope registers the state, its optional installer and
    /// whatever the install action adds (sub-states, the scenario handler of the Init scope). The app scope must provide
    /// <see cref="IAssetsService"/> and <see cref="IEventPublisher"/>; <see cref="IStateTransition"/> registrations are
    /// picked up when present.
    /// </summary>
    public sealed class GameStatesInstaller : IInstaller
    {
        private readonly IBaseStateDescriptorProvider _descriptors;
        private readonly IStateLogSink _logSink;
        private readonly List<StateRegistration> _states = new List<StateRegistration>();

        /// <summary>Creates the installer.</summary>
        /// <param name="descriptors">Tells which state is Init and where each scene is.</param>
        /// <param name="logSink">Receives the machine's lines; the Unity console when null.</param>
        public GameStatesInstaller(IBaseStateDescriptorProvider descriptors, IStateLogSink logSink = null)
        {
            _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            _logSink = logSink ?? new DebugStateLogSink();
        }

        /// <summary>Declares a base state that needs no installer.</summary>
        /// <typeparam name="TState">The base state.</typeparam>
        /// <param name="install">Extra registrations of the state's own scope, or null.</param>
        public GameStatesInstaller AddState<TState>(Action<IContainerBuilder> install = null)
            where TState : class, IGameState
        {
            _states.Add(new StateRegistration(typeof(TState), null, install));
            return this;
        }

        /// <summary>Declares a base state with its installer, registered in the state's own scope.</summary>
        /// <typeparam name="TState">The base state.</typeparam>
        /// <typeparam name="TInstaller">The installer that loads and unloads the state.</typeparam>
        /// <param name="install">Extra registrations of the state's own scope (the installer's dependents too), or null.</param>
        public GameStatesInstaller AddState<TState, TInstaller>(Action<IContainerBuilder> install = null)
            where TState : class, IGameState
            where TInstaller : class, IStateInstaller
        {
            _states.Add(new StateRegistration(typeof(TState), typeof(TInstaller), install));
            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">A base state is declared twice.</exception>
        public void Install(IContainerBuilder builder)
        {
            var states = new Dictionary<Type, StateRegistration>();
            for (var i = 0; i < _states.Count; i++)
            {
                states.Add(_states[i].State, _states[i]);
            }

            var descriptors = _descriptors;
            var logSink = _logSink;
            builder.Register<IStateScopeFactory>(
                resolver => new StateScopeFactory(resolver, descriptors, states),
                Lifetime.Singleton);
            builder.Register<IGameStateMachine>(
                resolver => new GameStateMachine(
                    resolver.Resolve<IStateScopeFactory>(),
                    descriptors,
                    resolver.Resolve<IAssetsService>().GlobalContext,
                    resolver.Resolve<IEventPublisher>(),
                    logSink,
                    ResolveTransitions(resolver)),
                Lifetime.Singleton);
        }

        private static IReadOnlyList<IStateTransition> ResolveTransitions(IObjectResolver resolver)
        {
            return resolver.TryResolve<IReadOnlyList<IStateTransition>>(out var transitions) ? transitions : null;
        }
    }
}
