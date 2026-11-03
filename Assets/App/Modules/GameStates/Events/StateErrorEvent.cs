using System;
using Biome.GameStates.Data;

namespace Biome.GameStates.Events
{
    /// <summary>
    /// The fact that a state failed in one of its phases. A payload published through the event bus; the stack trace
    /// is <see cref="Exception"/>.StackTrace.
    /// </summary>
    public readonly struct StateErrorEvent
    {
        /// <summary>The state that failed.</summary>
        public readonly Type State;

        /// <summary>The level of the failed state.</summary>
        public readonly StateLevel Level;

        /// <summary>The phase in which the failure happened.</summary>
        public readonly StatePhase Phase;

        /// <summary>The exception that was thrown.</summary>
        public readonly Exception Exception;

        /// <summary>Creates the payload.</summary>
        /// <param name="state">The state that failed.</param>
        /// <param name="level">The level of the failed state.</param>
        /// <param name="phase">The phase in which the failure happened.</param>
        /// <param name="exception">The exception that was thrown.</param>
        public StateErrorEvent(Type state, StateLevel level, StatePhase phase, Exception exception)
        {
            State = state;
            Level = level;
            Phase = phase;
            Exception = exception;
        }
    }
}
