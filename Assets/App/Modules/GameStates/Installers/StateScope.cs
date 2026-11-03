using System;
using Biome.GameStates.Abstract;
using VContainer;

namespace Biome.GameStates.Installers
{
    /// <summary>One state scope over a VContainer scope; disposing it disposes everything the scope created.</summary>
    internal sealed class StateScope : IStateScope
    {
        private readonly IScopedObjectResolver _resolver;
        private readonly Type _installerType;
        private IStateInstaller _installer;

        public StateScope(IScopedObjectResolver resolver, Type installerType)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _installerType = installerType;
        }

        public IObjectResolver Resolver => _resolver;

        public IStateScenarioHandler ScenarioHandler =>
            _resolver.TryResolve<IStateScenarioHandler>(out var handler) ? handler : null;

        public IStateInstaller Installer
        {
            get
            {
                if (_installerType == null)
                {
                    return null;
                }

                return _installer ??= (IStateInstaller)_resolver.Resolve(_installerType);
            }
        }

        public IGameState ResolveState(Type state)
        {
            return (IGameState)_resolver.Resolve(state);
        }

        public void Dispose()
        {
            _resolver.Dispose();
        }
    }
}
