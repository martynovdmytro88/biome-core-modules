using System;
using Biome.Configs.Abstract;

namespace Biome.Configs
{
    /// <summary>
    /// The B2 provider of one config: reads it from the configs reader at the moment <see cref="Value"/> is read.
    /// Main thread only.
    /// </summary>
    public sealed class ConfigProvider<T> : IConfig<T> where T : class
    {
        private readonly IConfigsReader _reader;

        /// <summary>Creates the provider. Throws <see cref="ArgumentNullException"/> when <paramref name="reader"/> is null.</summary>
        public ConfigProvider(IConfigsReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// The config instance, never null. Throws <see cref="ConfigNotInitializedException"/> while the reader
        /// has no such config yet.
        /// </summary>
        public T Value
        {
            get
            {
                if (_reader.TryGet<T>(out var config))
                {
                    return config;
                }

                throw new ConfigNotInitializedException(typeof(T));
            }
        }
    }
}
