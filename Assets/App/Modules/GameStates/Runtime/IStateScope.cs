using System;
using Biome.GameStates.Abstract;

namespace Biome.GameStates
{
    /// <summary>
    /// The seam to one state scope: the machine drives it, the installers layer implements it over the DI container.
    /// Disposing it releases everything the scope holds. Main thread only.
    /// </summary>
    public interface IStateScope : IDisposable
    {
        /// <summary>The scenario handler of this scope, or null when none is registered.</summary>
        IStateScenarioHandler ScenarioHandler { get; }

        /// <summary>The installer of the base state of this scope, or null when the state needs none.</summary>
        IStateInstaller Installer { get; }

        /// <summary>Resolves a state (the base state itself or one of its sub-states) from this scope.</summary>
        /// <param name="state">The state type.</param>
        IGameState ResolveState(Type state);
    }
}
