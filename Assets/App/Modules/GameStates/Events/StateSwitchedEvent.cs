using System;
using Biome.GameStates.Data;

namespace Biome.GameStates.Events
{
    /// <summary>
    /// The fact that a state switch finished. A payload published through the event bus; it never drives the
    /// transition.
    /// </summary>
    public readonly struct StateSwitchedEvent
    {
        /// <summary>The state that was left.</summary>
        public readonly Type From;

        /// <summary>The state that was entered.</summary>
        public readonly Type To;

        /// <summary>The level the switch happened on.</summary>
        public readonly StateLevel Level;

        /// <summary>Creates the payload.</summary>
        /// <param name="from">The state that was left.</param>
        /// <param name="to">The state that was entered.</param>
        /// <param name="level">The level the switch happened on.</param>
        public StateSwitchedEvent(Type from, Type to, StateLevel level)
        {
            From = from;
            To = to;
            Level = level;
        }
    }
}
