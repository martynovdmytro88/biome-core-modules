using System;
using Biome.Configs.Abstract;
using UnityEngine;

namespace Biome.Configs.Config
{
    /// <summary>
    /// The configs module's own environment asset: which source holds which config type. One asset per environment
    /// (production · test); it holds data only and loads nothing. Main thread only.
    /// </summary>
    [CreateAssetMenu(menuName = "Biome/Configs/Environment Config")]
    public sealed class EnvironmentConfig : ScriptableObject, IConfigsEnvironment, IConfigsEnvironmentGuard
    {
        [SerializeField] private string _id;
        [SerializeField] private SourceEntry[] _sources = Array.Empty<SourceEntry>();
        [SerializeField] private ScriptableObject[] _containers = Array.Empty<ScriptableObject>();

        /// <inheritdoc />
        public string Id => _id;

        /// <summary>
        /// The source of <paramref name="configType"/>, matched by the type names in the source map. False when the
        /// map names none for that type, including when a stored name does not resolve to a type.
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="configType"/> is null.
        /// </summary>
        public bool TryGetSource(Type configType, out ConfigSource source)
        {
            if (configType == null)
            {
                throw new ArgumentNullException(nameof(configType));
            }

            foreach (var entry in _sources)
            {
                if (entry != null && entry.ResolveType() == configType)
                {
                    source = new ConfigSource(entry.Kind, entry.Path);
                    return true;
                }
            }

            source = default;
            return false;
        }

        /// <summary>
        /// The guard the service calls before it loads: true with the first config type name that two or more
        /// entries share. One source per type, so a duplicate makes the environment invalid.
        /// </summary>
        public bool TryFindDuplicateType(out string configType)
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                var name = _sources[i]?.ConfigTypeName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                for (var j = i + 1; j < _sources.Length; j++)
                {
                    if (string.Equals(name, _sources[j]?.ConfigTypeName, StringComparison.Ordinal))
                    {
                        configType = name;
                        return true;
                    }
                }
            }

            configType = null;
            return false;
        }

        private void OnValidate()
        {
            if (TryFindDuplicateType(out var duplicate))
            {
                Debug.LogError($"Environment '{name}' names more than one source for config type '{duplicate}'.", this);
            }
        }

        [Serializable]
        private sealed class SourceEntry
        {
            [SerializeField] private string _configType;
            [SerializeField] private ConfigSourceKind _kind;
            [SerializeField] private string _path;

            public string ConfigTypeName => _configType;

            public ConfigSourceKind Kind => _kind;

            public string Path => _path ?? string.Empty;

            public Type ResolveType()
            {
                return string.IsNullOrWhiteSpace(_configType) ? null : Type.GetType(_configType, false);
            }
        }
    }
}
