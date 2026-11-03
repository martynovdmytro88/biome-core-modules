using System;

namespace Biome.Configs.Abstract
{
    /// <summary>Thrown by the loaders run when a config still fails to load after every attempt.</summary>
    public sealed class ConfigLoadException : Exception
    {
        /// <summary>Creates the exception for <paramref name="configType"/> after <paramref name="attempts"/> attempts.</summary>
        public ConfigLoadException(Type configType, int attempts, Exception inner)
            : base($"Config '{configType?.FullName}' failed to load after {attempts} attempt(s).", inner)
        {
            ConfigType = configType;
            Attempts = attempts;
        }

        /// <summary>The config type that failed to load.</summary>
        public Type ConfigType { get; }

        /// <summary>How many attempts were made.</summary>
        public int Attempts { get; }
    }
}
