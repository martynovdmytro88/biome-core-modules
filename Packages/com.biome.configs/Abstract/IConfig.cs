namespace Biome.Configs.Abstract
{
    /// <summary>
    /// The provider a consumer injects to read one config (B2). Reading <see cref="Value"/> before the config is
    /// available throws <see cref="ConfigNotInitializedException"/>, never returns null.
    /// </summary>
    public interface IConfig<out T> where T : class
    {
        /// <summary>The config instance. Throws <see cref="ConfigNotInitializedException"/> before it is available.</summary>
        T Value { get; }
    }
}
