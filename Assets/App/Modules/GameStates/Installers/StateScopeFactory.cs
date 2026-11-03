using System;
using System.Collections.Generic;
using Biome.Assets.Abstract;
using Biome.GameStates.Abstract;
using VContainer;

namespace Biome.GameStates.Installers
{
    /// <summary>
    /// Builds the scope tree over VContainer: the Init scope under the app resolver, every other base state under the
    /// Init scope. Each scope gets the state's assets context, its sub-state service and its own registrations.
    /// </summary>
    internal sealed class StateScopeFactory : IStateScopeFactory
    {
        private readonly IObjectResolver _app;
        private readonly IBaseStateDescriptorProvider _descriptors;
        private readonly IReadOnlyDictionary<Type, StateRegistration> _states;
        private StateScope _init;

        public StateScopeFactory(
            IObjectResolver app,
            IBaseStateDescriptorProvider descriptors,
            IReadOnlyDictionary<Type, StateRegistration> states)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            _states = states ?? throw new ArgumentNullException(nameof(states));
        }

        public IStateScope CreateInitScope(IAssetsContext assets, ISubStateService subStates)
        {
            if (_init != null)
            {
                throw new InvalidOperationException("The Init scope is already created.");
            }

            _init = Create(_app, Find(_descriptors.InitState), assets, subStates);
            return _init;
        }

        public IStateScope CreateChildScope(Type baseState, IAssetsContext assets, ISubStateService subStates)
        {
            if (_init == null)
            {
                throw new InvalidOperationException("The Init scope is not created yet.");
            }

            return Create(_init.Resolver, Find(baseState), assets, subStates);
        }

        private static StateScope Create(
            IObjectResolver parent,
            StateRegistration registration,
            IAssetsContext assets,
            ISubStateService subStates)
        {
            var resolver = parent.CreateScope(builder =>
            {
                builder.RegisterInstance<IAssetsContext>(assets);
                builder.RegisterInstance<ISubStateService>(subStates);
                registration.Register(builder);
            });
            return new StateScope(resolver, registration.Installer);
        }

        private StateRegistration Find(Type state)
        {
            if (state == null || !_states.TryGetValue(state, out var registration))
            {
                throw new InvalidOperationException($"The base state {state?.Name ?? "none"} is not registered.");
            }

            return registration;
        }
    }
}
