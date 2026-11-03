namespace Biome.EventBus.Abstract
{
    /// <summary>The producer's side of the bus: publishes a fact without knowing who listens.</summary>
    public interface IEventPublisher
    {
        /// <summary>Same delivery as <see cref="IEvent{TData}.Publish"/> on the bus's channel of <typeparamref name="TData"/>.</summary>
        void Publish<TData>(in TData data) where TData : struct;
    }
}
