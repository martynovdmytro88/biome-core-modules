using System;
using VContainer.Unity;

namespace Biome.App
{
    /// <summary>
    /// A reaction of a flow to the completion of its states: it listens to <see cref="StateCompletedEvent"/> on the
    /// bus and decides the next switch. A state never knows which scenario hears it; the profile registers the scenario
    /// of the flow it runs, and the prod flow registers none.
    /// </summary>
    public interface IFlowScenario : IInitializable, IDisposable
    {
    }
}
