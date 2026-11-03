namespace Biome.Configs.Abstract
{
    /// <summary>The read side of the configs. Main thread only.</summary>
    public interface IConfigsReader
    {
        /// <summary>
        /// The config of type <typeparamref name="T"/>. False is a blocker for the caller: there are no optional
        /// configs, so a missing config is never a normal outcome.
        /// </summary>
        bool TryGet<T>(out T config) where T : class;
    }
}
