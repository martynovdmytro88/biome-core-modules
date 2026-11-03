using System;

namespace Biome.Configs.Abstract
{
    /// <summary>
    /// The environment: which source holds which config type. One per environment (production · test).
    /// </summary>
    public interface IConfigsEnvironment
    {
        /// <summary>The environment id, for example "production" or "test".</summary>
        string Id { get; }

        /// <summary>
        /// The source of <paramref name="configType"/>. False when the environment names none for that type.
        /// </summary>
        bool TryGetSource(Type configType, out ConfigSource source);
    }
}
