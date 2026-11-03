using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Biome.Configs.Abstract
{
    /// <summary>
    /// Loads one config type. Implemented in the OWNER's Config/ and registered by the owner's installer; one
    /// loader per config type. It owns the DTO to config conversion.
    /// </summary>
    public interface IConfigLoader
    {
        /// <summary>The config type this loader produces.</summary>
        Type ConfigType { get; }

        /// <summary>Reads the raw asset from <paramref name="source"/> through <paramref name="reader"/> and returns the config.</summary>
        UniTask<object> LoadAsync(ConfigSource source, IConfigSourceReader reader, CancellationToken ct);
    }
}
