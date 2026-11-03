using System;

namespace Biome.EventBus.Abstract
{
    /// <summary>
    /// One event bus — one instance per DI scope. Delivery is synchronous inside Publish; a Publish from inside a
    /// listener is delivered depth-first; listener order is not guaranteed. Main thread only.
    /// <c>Dispose()</c> drops every subscription; any later Publish / Subscribe / Channel throws
    /// <see cref="ObjectDisposedException"/>, while disposing a subscription stays a no-op.
    /// </summary>
    public interface IEventBus : IEventPublisher, IEventSubscriber, IDisposable
    {
        /// <summary>The typed channel of <typeparamref name="TData"/> — the same instance per type per bus.</summary>
        IEvent<TData> Channel<TData>() where TData : struct;
    }
}
