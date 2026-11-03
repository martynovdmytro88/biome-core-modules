using System;
using UnityEngine;

namespace Biome.GameStates.Config
{
    /// <summary>
    /// The asset of one state flow. It holds data only; <see cref="ToConfig"/> hands out a copy, so a caller that
    /// changes the copy never changes the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Biome/GameStates/Flow Config")]
    public sealed class GameStatesFlowConfigSO : ScriptableObject
    {
        [SerializeField] private GameStatesFlowConfig _flow = new GameStatesFlowConfig();

        /// <summary>Builds a copy of the flow held here: the lists and their entries are new objects.</summary>
        public GameStatesFlowConfig ToConfig()
        {
            var states = new GameStatesFlowConfig.StateEntry[_flow.States.Length];
            for (var i = 0; i < states.Length; i++)
            {
                var entry = _flow.States[i];
                states[i] = entry == null ? null : new GameStatesFlowConfig.StateEntry { StateType = entry.StateType, Scene = entry.Scene };
            }

            var transitions = new GameStatesFlowConfig.TransitionEntry[_flow.Transitions.Length];
            for (var i = 0; i < transitions.Length; i++)
            {
                var entry = _flow.Transitions[i];
                transitions[i] = entry == null ? null : new GameStatesFlowConfig.TransitionEntry { From = entry.From, To = entry.To };
            }

            return new GameStatesFlowConfig
            {
                Name = _flow.Name,
                InitState = _flow.InitState,
                States = states,
                Transitions = transitions,
            };
        }
    }
}
