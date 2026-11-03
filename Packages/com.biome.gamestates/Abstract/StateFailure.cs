using System;
using Biome.GameStates.Data;

namespace Biome.GameStates.Abstract
{
    /// <summary>What a scenario handler receives: which state failed, on which level, in which phase, and why.</summary>
    public readonly struct StateFailure : IEquatable<StateFailure>
    {
        /// <summary>The state that failed.</summary>
        public readonly Type State;

        /// <summary>The level of the failed state.</summary>
        public readonly StateLevel Level;

        /// <summary>The phase in which the failure happened.</summary>
        public readonly StatePhase Phase;

        /// <summary>The exception that was thrown.</summary>
        public readonly Exception Exception;

        /// <summary>Creates the failure description.</summary>
        /// <param name="state">The state that failed.</param>
        /// <param name="level">The level of the failed state.</param>
        /// <param name="phase">The phase in which the failure happened.</param>
        /// <param name="exception">The exception that was thrown.</param>
        public StateFailure(Type state, StateLevel level, StatePhase phase, Exception exception)
        {
            State = state;
            Level = level;
            Phase = phase;
            Exception = exception;
        }

        /// <inheritdoc />
        public bool Equals(StateFailure other)
        {
            return State == other.State && Level == other.Level && Phase == other.Phase
                && ReferenceEquals(Exception, other.Exception);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is StateFailure other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(State, Level, Phase, Exception);
        }
    }
}
