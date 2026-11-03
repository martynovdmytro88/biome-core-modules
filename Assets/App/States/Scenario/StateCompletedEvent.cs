using System;

namespace Biome.App
{
    /// <summary>
    /// The fact that a state finished its own work. A payload published through the event bus; it never drives the
    /// transition, a flow scenario that listens to it decides where to go next.
    /// </summary>
    public readonly struct StateCompletedEvent
    {
        /// <summary>The state that completed.</summary>
        public readonly Type State;

        /// <summary>Creates the payload.</summary>
        /// <param name="state">The state that completed.</param>
        public StateCompletedEvent(Type state)
        {
            State = state;
        }
    }
}
