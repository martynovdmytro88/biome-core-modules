using System;
using System.Collections.Generic;

namespace Biome.GameStates.Config
{
    /// <summary>
    /// One state flow: the first state the machine runs (Init), the base states with their optional scene addresses, and
    /// which state may follow which. Which state follows is the machine's business, not the flow's. State types are
    /// assembly-qualified names, resolved by
    /// <see cref="ResolveState"/>. Immutable after load; fields are public so the engine serialises them.
    /// </summary>
    [Serializable]
    public sealed class GameStatesFlowConfig
    {
        public string Name = string.Empty;
        public string InitState = string.Empty;
        public StateEntry[] States = Array.Empty<StateEntry>();
        public TransitionEntry[] Transitions = Array.Empty<TransitionEntry>();

        /// <summary>Resolves an assembly-qualified state type name; null when blank or unknown.</summary>
        public static Type ResolveState(string typeName)
        {
            return string.IsNullOrWhiteSpace(typeName) ? null : Type.GetType(typeName, false);
        }

        /// <summary>The first state type; null when the name does not resolve.</summary>
        public Type ResolveInitState() => ResolveState(InitState);

        /// <summary>Gives the scene address of a base state. False when the state is not in the flow or has no scene.</summary>
        public bool TryGetScene(Type state, out string scene)
        {
            foreach (var entry in States)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Scene) && ResolveState(entry.StateType) == state)
                {
                    scene = entry.Scene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        /// <summary>True when the flow lets <paramref name="to"/> follow <paramref name="from"/>.</summary>
        public bool Allows(Type from, Type to)
        {
            foreach (var entry in Transitions)
            {
                if (entry != null && ResolveState(entry.From) == from && ResolveState(entry.To) == to)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the flow and throws <see cref="InvalidOperationException"/> naming the flow and the fault: a blank
        /// name, an unknown state type, a first state that is not in the list,
        /// or a transition between states that are not in the list.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new InvalidOperationException("A state flow has no name.");
            }

            var init = RequireType(InitState, "the first state");
            var listed = new HashSet<Type>();
            foreach (var entry in States)
            {
                if (entry == null)
                {
                    throw new InvalidOperationException($"Flow '{Name}': a state entry is null.");
                }

                var type = RequireType(entry.StateType, "a state entry");
                if (!listed.Add(type))
                {
                    throw new InvalidOperationException($"Flow '{Name}': state '{entry.StateType}' is listed twice.");
                }
            }

            if (!listed.Contains(init))
            {
                throw new InvalidOperationException($"Flow '{Name}': the first state '{InitState}' is not in the state list.");
            }

            foreach (var entry in Transitions)
            {
                if (entry == null)
                {
                    throw new InvalidOperationException($"Flow '{Name}': a transition entry is null.");
                }

                if (!listed.Contains(RequireType(entry.From, "a transition")) || !listed.Contains(RequireType(entry.To, "a transition")))
                {
                    throw new InvalidOperationException($"Flow '{Name}': transition '{entry.From}' to '{entry.To}' names a state that is not in the list.");
                }
            }
        }

        private Type RequireType(string typeName, string role)
        {
            var type = ResolveState(typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Flow '{Name}': {role} '{typeName}' is blank or is not a known type.");
            }

            return type;
        }

        /// <summary>A base state of the flow. The scene is optional; blank means the state loads none.</summary>
        [Serializable]
        public sealed class StateEntry
        {
            public string StateType = string.Empty;
            public string Scene = string.Empty;
        }

        /// <summary>One allowed switch.</summary>
        [Serializable]
        public sealed class TransitionEntry
        {
            public string From = string.Empty;
            public string To = string.Empty;
        }
    }
}
