using System;

namespace Biome.EventBus.Abstract
{
    /// <summary>The listener's side of the bus: subscribes to a fact type without knowing who publishes it.</summary>
    public interface IEventSubscriber
    {
        /// <summary>Same as <see cref="IEvent{TData}.Subscribe"/> on the bus's channel of <typeparamref name="TData"/>.</summary>
        IDisposable Subscribe<TData>(EventListener<TData> listener) where TData : struct;
    }
}
