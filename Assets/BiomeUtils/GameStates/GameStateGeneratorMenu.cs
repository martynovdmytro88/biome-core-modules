using System;
using Biome.Utils.Commands;
using UnityEditor;
using UnityEngine;

namespace Biome.Utils.GameStates
{
    /// <summary>The Editor menu wrapper over <see cref="GameStateGenerator.CreateGameState"/>: asks for the id, then calls it.</summary>
    internal static class GameStateGeneratorMenu
    {
        [MenuItem("Biome/GameStates/Create State…", priority = 10)]
        private static void CreateStateMenu()
        {
            StateIdWindow.Open(stateId =>
            {
                var env = GameStateGenerator.CreateGameState(stateId);
                Debug.Log($"[GameStates] {env.Code}: {env.Message}");
                if (env.Code != ResultCode.Ok)
                {
                    EditorUtility.DisplayDialog(GameStateGenerator.Name, env.Message, "OK");
                }
            });
        }

        /// <summary>Minimal one-field prompt (convenience only; the callable is the contract).</summary>
        internal sealed class StateIdWindow : EditorWindow
        {
            private string _value = string.Empty;
            private Action<string> _onOk;

            public static void Open(Action<string> onOk)
            {
                var window = CreateInstance<StateIdWindow>();
                window.titleContent = new GUIContent("Create game state");
                window._onOk = onOk;
                window.minSize = new Vector2(320f, 70f);
                window.maxSize = new Vector2(320f, 70f);
                window.ShowUtility();
            }

            private void OnGUI()
            {
                _value = EditorGUILayout.TextField("State id", _value);
                using (new EditorGUI.DisabledScope(!GameStateGenerator.IsValidStateId(_value)))
                {
                    if (GUILayout.Button("Create"))
                    {
                        var onOk = _onOk;
                        var value = _value;
                        Close();
                        onOk?.Invoke(value);
                    }
                }
            }
        }
    }
}
