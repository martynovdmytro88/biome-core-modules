using System;

namespace Biome.Configs.Abstract
{
    /// <summary>Thrown when a config is read before it is available.</summary>
    public sealed class ConfigNotInitializedException : InvalidOperationException
    {
        /// <summary>Creates the exception for <paramref name="configType"/>.</summary>
        public ConfigNotInitializedException(Type configType)
            : base($"Config '{configType?.FullName}' is not available yet.")
        {
            ConfigType = configType;
        }

        /// <summary>The config type that was read too early.</summary>
        public Type ConfigType { get; }
    }
}
