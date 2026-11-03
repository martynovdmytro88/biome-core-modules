using System;
using System.Collections.Generic;
using Biome.Configs.Abstract;

namespace Biome.Configs.Fakes
{
    /// <summary>
    /// The in-memory fake of <see cref="IConfigsEnvironment"/>: a source map built in code. Fail-fast: a second
    /// source for one config type is refused when it is added. Main thread only.
    /// </summary>
    public sealed class FakeConfigsEnvironment : IConfigsEnvironment
    {
        private readonly Dictionary<Type, ConfigSource> _sources = new Dictionary<Type, ConfigSource>();

        /// <summary>Creates an empty environment with the given id.</summary>
        public FakeConfigsEnvironment(string id)
        {
            Id = id;
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <summary>
        /// Adds the source of <paramref name="configType"/> and returns the environment. Throws
        /// <see cref="ArgumentNullException"/> for a null type and <see cref="InvalidOperationException"/> when the
        /// type already has a source.
        /// </summary>
        public FakeConfigsEnvironment Add(Type configType, ConfigSource source)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType));
            }

            if (_sources.ContainsKey(configType))
            {
                throw new InvalidOperationException($"Environment '{Id}' already names a source for config type '{configType.FullName}'.");
            }

            _sources.Add(configType, source);
            return this;
        }

        /// <summary>Adds a Resources source at <paramref name="path"/> for <typeparamref name="T"/> and returns the environment.</summary>
        public FakeConfigsEnvironment Add<T>(string path) where T : class
        {
            return Add(typeof(T), new ConfigSource(ConfigSourceKind.Resources, path));
        }

        /// <inheritdoc />
        public bool TryGetSource(Type configType, out ConfigSource source)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType));
            }

            return _sources.TryGetValue(configType, out source);
        }
    }
}
