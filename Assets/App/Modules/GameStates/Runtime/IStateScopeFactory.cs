using System;
using Biome.Assets.Abstract;
using Biome.GameStates.Abstract;

namespace Biome.GameStates
{
    /// <summary>
    /// The seam that hides the DI container from the machine: creates the scope of a base state. The scope registers
    /// the given sub-state service so the state can inject it. Main thread only.
    /// </summary>
    public interface IStateScopeFactory
    {
        /// <summary>Creates the persistent scope of the Init state; the machine never disposes it.</summary>
        /// <param name="assets">The assets context of the Init state.</param>
        /// <param name="subStates">The sub-state service of the Init state.</param>
        IStateScope CreateInitScope(IAssetsContext assets, ISubStateService subStates);

        /// <summary>Creates the child scope of a base state under the Init scope.</summary>
        /// <param name="baseState">The base state type.</param>
        /// <param name="assets">The assets context of the base state.</param>
        /// <param name="subStates">The sub-state service of the base state.</param>
        IStateScope CreateChildScope(Type baseState, IAssetsContext assets, ISubStateService subStates);
    }
}
