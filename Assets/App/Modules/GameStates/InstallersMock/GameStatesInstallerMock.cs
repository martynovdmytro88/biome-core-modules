using System;
using Biome.GameStates.Abstract;
using VContainer;
using VContainer.Unity;

namespace Biome.GameStates.Installers.Mock
{
    /// <summary>
    /// The Mock profile's twin of <c>GameStatesInstaller</c>: the same <see cref="IGameStateMachine"/> face, backed by the
    /// machine the caller hands in (a fake). It needs no assets, bus or scope tree.
    /// </summary>
    public sealed class GameStatesInstallerMock : IInstaller
    {
        private readonly IGameStateMachine _machine;

        /// <summary>Creates the installer.</summary>
        /// <param name="machine">The machine consumers resolve; a fake from the test.</param>
        public GameStatesInstallerMock(IGameStateMachine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance<IGameStateMachine>(_machine);
        }
    }
}
