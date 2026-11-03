namespace Biome.EventBus.Abstract
{
    /// <summary>
    /// A listener of one event type. <typeparamref name="TData"/> is the producer↔listener contract — a readonly,
    /// past-tense fact struct; it arrives by reference (<c>in</c>), never boxed or copied by the bus.
    /// </summary>
    public delegate void EventListener<TData>(in TData data) where TData : struct;
}
