using System;
using Biome.Assets.Abstract;
using Biome.GameStates;
using Biome.GameStates.Config;

namespace Biome.App
{
    /// <summary>
    /// The scene keys of the base states and the first state of the machine, both read from the one flow the app runs.
    /// </summary>
    public sealed class ConfigFlowDescriptorProvider : IBaseStateDescriptorProvider
    {
        private readonly GameStatesFlowConfig _flow;
        private readonly Type _initState;

        /// <summary>Creates the provider over <paramref name="flow"/>.</summary>
        /// <exception cref="ArgumentNullException">The flow is null.</exception>
        /// <exception cref="InvalidOperationException">The flow names no known first state.</exception>
        public ConfigFlowDescriptorProvider(GameStatesFlowConfig flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _initState = flow.ResolveInitState()
                ?? throw new InvalidOperationException($"Flow '{flow.Name}': the first state '{flow.InitState}' is blank or is not a known type.");
        }

        /// <inheritdoc />
        public Type InitState => _initState;

        /// <inheritdoc />
        public bool TryGetScene(Type state, out AssetKey sceneKey)
        {
            if (_flow.TryGetScene(state, out var scene))
            {
                sceneKey = new AssetKey(scene);
                return true;
            }

            sceneKey = default;
            return false;
        }
    }
}
