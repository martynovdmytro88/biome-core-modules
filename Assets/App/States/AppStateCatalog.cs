using System;
using System.Collections.Generic;
using Biome.GameStates.Abstract;
using Biome.GameStates.Config;
using Biome.GameStates.Installers;
using VContainer;

namespace Biome.App
{
    /// <summary>
    /// The table of the base states the app can declare, by state type. A flow lists states by type name and the generic
    /// <c>AddState</c> needs the types at compile time, so each state is one entry here; <see cref="Declare"/> declares
    /// the entries the flow lists. Nothing here looks at the flow's name.
    /// </summary>
    internal static class AppStateCatalog
    {
        /// <summary>One catalogue entry: the state type and the call that declares it.</summary>
        internal readonly struct Entry
        {
            internal Entry(Type state, Action<GameStatesInstaller> declare)
            {
                State = state;
                Declare = declare;
            }

            internal Type State { get; }

            internal Action<GameStatesInstaller> Declare { get; }
        }

        private static readonly Entry[] s_app =
        {
            new Entry(typeof(InitState), states => states.AddState<InitState, InitStateInstaller>(scope =>
                scope.Register<EmergencyScenarioHandler>(Lifetime.Scoped).As<IStateScenarioHandler>())),
            new Entry(typeof(EmergencyState), states => states.AddState<EmergencyState, EmergencyStateInstaller>()),
        };

        /// <summary>
        /// Declares on <paramref name="states"/> exactly the states <paramref name="flow"/> lists, taken from the app
        /// entries and from <paramref name="variantEntries"/>. Throws <see cref="InvalidOperationException"/> naming the
        /// flow and the state when a listed state has no entry.
        /// </summary>
        internal static void Declare(GameStatesFlowConfig flow, GameStatesInstaller states, IReadOnlyList<Entry> variantEntries = null)
        {
            var table = new Dictionary<Type, Entry>();
            foreach (var entry in s_app)
            {
                table[entry.State] = entry;
            }

            if (variantEntries != null)
            {
                foreach (var entry in variantEntries)
                {
                    table[entry.State] = entry;
                }
            }

            foreach (var listed in flow.States)
            {
                var type = GameStatesFlowConfig.ResolveState(listed.StateType);
                if (type == null || !table.TryGetValue(type, out var entry))
                {
                    throw new InvalidOperationException($"Flow '{flow.Name}': the state '{listed.StateType}' has no entry in the app state catalogue.");
                }

                entry.Declare(states);
            }
        }
    }
}
