namespace Biome.EventBus.Abstract
{
    /// <summary>Dev-only observer of every Publish; called only while tracing is enabled on the bus.</summary>
    public interface IEventTrace
    {
        void OnPublished<TData>(in TData data, int listenerCount) where TData : struct;
    }
}
