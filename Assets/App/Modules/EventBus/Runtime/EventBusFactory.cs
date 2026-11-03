using Biome.EventBus.Abstract;
using Biome.EventBus.Config;

namespace Biome.EventBus
{
    /// <summary>
    /// The creation point of the production bus: <see cref="EventBus"/> is internal, so every caller outside the
    /// <c>Runtime</c> assembly builds a bus here. Pure — no container, no state; the caller owns the returned bus and
    /// disposes it (the container does so for a registered instance).
    /// </summary>
    public static class EventBusFactory
    {
        /// <summary>
        /// Creates a synchronous production bus.
        /// </summary>
        /// <param name="config">The bus settings.</param>
        /// <param name="errorSink">Receives listener exceptions; <c>null</c> logs them to the Unity console.</param>
        /// <param name="trace">Receives dispatch traces; used only when <see cref="EventBusConfig.TracingEnabled"/> is set.</param>
        /// <returns>A new bus bound to the calling thread.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><see cref="EventBusConfig.MaxDispatchDepth"/> is below 1.</exception>
        public static IEventBus Create(EventBusConfig config, IEventErrorSink errorSink = null, IEventTrace trace = null)
        {
            return new EventBus(config, errorSink, trace);
        }
    }
}
