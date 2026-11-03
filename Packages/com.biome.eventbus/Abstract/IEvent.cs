using System;

namespace Biome.EventBus.Abstract
{
    /// <summary>
    /// The typed channel of one event type on one bus — the hot path: non-generic members, no type lookup per call.
    /// Obtain it once via <see cref="IEventBus.Channel{TData}"/> and cache it.
    /// </summary>
    public interface IEvent<TData> where TData : struct
    {
        /// <summary>
        /// Delivers <paramref name="data"/> synchronously to every listener subscribed before this call; returns after
        /// the last one. Listener order is not guaranteed.
        /// </summary>
        void Publish(in TData data);

        /// <summary>
        /// Adds a listener. Disposing the returned handle removes it; once <c>Dispose()</c> returns the listener is never
        /// called again. The subscription's lifetime is the subscriber's.
        /// </summary>
        IDisposable Subscribe(EventListener<TData> listener);
    }
}
