using UnityEngine;

namespace Biome.GameStates.Installers
{
    /// <summary>The host log sink: the machine's tagged lines go to the Unity console.</summary>
    internal sealed class DebugStateLogSink : IStateLogSink
    {
        public void Info(string message)
        {
            Debug.Log(message);
        }

        public void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}
