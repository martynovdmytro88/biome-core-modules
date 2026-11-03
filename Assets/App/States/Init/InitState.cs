using System;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.App
{
    /// <summary>
    /// The first state of the app flow: entered once on Boot, loads no scene; its scope lives for the whole
    /// application. It initialises the configs service with the profile's environment and loads the configs, so nothing
    /// in the flow reads a config before this state has loaded it.
    /// </summary>
    public sealed class InitState : IGameState
    {
        private readonly IConfigsService _configs;
        private readonly IConfigsEnvironment _environment;

        /// <summary>Creates the state over the configs service and the environment the scope registered.</summary>
        /// <exception cref="ArgumentNullException">A dependency is null.</exception>
        public InitState(IConfigsService configs, IConfigsEnvironment environment)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Initialises the configs service and loads all configs. The service's failures surface from the returned
        /// task; nothing is swallowed.
        /// </summary>
        /// <param name="ct">Cancels the init.</param>
        public async UniTask EnterAsync(CancellationToken ct)
        {
            await _configs.InitializeAsync(_environment, ct);
            if (!_configs.IsLoaded)
            {
                await _configs.LoadAllAsync(ct);
            }

            // A host requests its first state here, through IGameStateMachine and with Forget: the machine queues the
            // switch behind the one that is still finishing.
        }

        /// <inheritdoc />
        public UniTask ExitAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}
