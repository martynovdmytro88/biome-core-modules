using System;

namespace Biome.EventBus.Abstract
{
    /// <summary>Receives a listener's exception when the bus isolates listener failures.</summary>
    public interface IEventErrorSink
    {
        void OnListenerException(Exception exception, Type dataType);
    }
}
