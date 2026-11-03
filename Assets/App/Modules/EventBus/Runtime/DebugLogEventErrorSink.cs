using System;
using Biome.EventBus.Abstract;
using UnityEngine;

namespace Biome.EventBus
{
    /// <summary>The Runtime default error sink: every isolated listener exception goes to the Unity console.</summary>
    public sealed class DebugLogEventErrorSink : IEventErrorSink
    {
        public void OnListenerException(Exception exception, Type dataType)
        {
            Debug.LogException(exception);
        }
    }
}
