using System;
using System.Collections.Generic;
using Biome.GameStates.Abstract;
using Biome.GameStates.Config;
using Biome.GameStates.Installers;
using VContainer;
using VContainer.Unity;

namespace Biome.App
{
    /// <summary>
    /// The composition of the state machine of the one flow the app runs: the states the flow lists (from the app catalogue
    /// plus the entries the scope variant added) and the starter (not run on build) that starts the first state the flow
    /// names.
    /// </summary>
    public sealed class AppStatesInstaller : IInstaller
    {
        private readonly GameStatesFlowConfig _flow;
        private readonly List<AppStateCatalog.Entry> _variantEntries = new List<AppStateCatalog.Entry>();

        /// <summary>Creates the installer for <paramref name="flow"/>; the container is not built yet, so the flow comes in here.</summary>
        /// <exception cref="ArgumentNullException">The flow is null.</exception>
        public AppStatesInstaller(GameStatesFlowConfig flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
        }

        /// <summary>Adds a state the app catalogue does not know; a scope variant declares its own states through this.</summary>
        /// <param name="install">Extra registrations of the state's own scope, or null.</param>
        public void AddCatalogEntry<TState, TInstaller>(Action<IContainerBuilder> install = null)
            where TState : class, IGameState
            where TInstaller : class, IStateInstaller
        {
            _variantEntries.Add(new AppStateCatalog.Entry(typeof(TState), states => states.AddState<TState, TInstaller>(install)));
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            var flow = _flow;
            var descriptors = new ConfigFlowDescriptorProvider(flow);
            var states = new GameStatesInstaller(descriptors);
            AppStateCatalog.Declare(flow, states, _variantEntries);
            states.Install(builder);
            builder.RegisterInstance(descriptors);
            builder.Register(
                resolver => new StateFlowStarter(resolver.Resolve<IGameStateMachine>(), flow),
                Lifetime.Singleton);
        }
    }
}
