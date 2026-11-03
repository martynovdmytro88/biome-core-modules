using System;
using System.Collections.Generic;

namespace Biome.Configs.Data
{
    /// <summary>
    /// Module state: config type to config instance, one instance per type. Filled while the configs load, then
    /// frozen; a frozen store accepts no further write. Main thread only.
    /// </summary>
    public sealed class ConfigStore
    {
        private readonly Dictionary<Type, object> _configs = new Dictionary<Type, object>();

        /// <summary>True once <see cref="Freeze"/> ran; the store is immutable from then on.</summary>
        public bool IsFrozen { get; private set; }

        /// <summary>
        /// Stores <paramref name="config"/> under <paramref name="configType"/>.
        /// Throws <see cref="ArgumentNullException"/> for a null argument, <see cref="ArgumentException"/> when the
        /// instance is not of that type, and <see cref="InvalidOperationException"/> when the store is frozen or the
        /// type is already stored: there is one source per type and no overlay.
        /// </summary>
        public void Put(Type configType, object config)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!configType.IsInstanceOfType(config))
            {
                throw new ArgumentException($"The instance is not of type '{configType.FullName}'.", nameof(config));
            }

            if (IsFrozen)
            {
                throw new InvalidOperationException($"The config store is frozen; '{configType.FullName}' cannot be put.");
            }

            if (_configs.ContainsKey(configType))
            {
                throw new InvalidOperationException($"The config store already holds '{configType.FullName}'.");
            }

            _configs.Add(configType, config);
        }

        /// <summary>
        /// The config stored under exactly <paramref name="configType"/>. False when none is stored.
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="configType"/> is null.
        /// </summary>
        public bool TryGet(Type configType, out object config)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType));
            }

            return _configs.TryGetValue(configType, out config);
        }

        /// <summary>Makes the store immutable. Calling it again changes nothing.</summary>
        public void Freeze()
        {
            IsFrozen = true;
        }
    }
}
