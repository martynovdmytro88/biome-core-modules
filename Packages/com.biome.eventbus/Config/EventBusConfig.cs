using Biome.EventBus.Data;

namespace Biome.EventBus.Config
{
    /// <summary>Construction-time settings of one bus; read once by the bus constructor.</summary>
    public sealed class EventBusConfig
    {
        public ExceptionPolicy ExceptionPolicy { get; set; } = ExceptionPolicy.Isolate;

        /// <summary>Deepest allowed nesting of Publish-from-a-listener; one level deeper throws (loop guard).</summary>
        public int MaxDispatchDepth { get; set; } = 16;

        /// <summary>Listener slots a channel reserves when it is created.</summary>
        public int InitialListenerCapacity { get; set; } = 4;

        /// <summary>Dev-only: when true and a trace is supplied, every Publish is reported to it.</summary>
        public bool TracingEnabled { get; set; } = false;
    }
}
