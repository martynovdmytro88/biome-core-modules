using System;

namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// What the registry knows about a stored type: its stable storage key (without the module prefix) and the
    /// version of its record shape. A retired key must never be reused for different data.
    /// </summary>
    public readonly struct LocalStoreEntry : IEquatable<LocalStoreEntry>
    {
        public readonly string Key;
        public readonly int Version;

        public LocalStoreEntry(string key, int version)
        {
            Key = key;
            Version = version;
        }

        /// <summary>Two entries are equal when their keys match ordinally and their versions match.</summary>
        public bool Equals(LocalStoreEntry other)
        {
            return string.Equals(Key, other.Key, StringComparison.Ordinal) && Version == other.Version;
        }

        /// <summary>Equal to another <see cref="LocalStoreEntry"/> by the same rule as <see cref="Equals(LocalStoreEntry)"/>.</summary>
        public override bool Equals(object obj)
        {
            return obj is LocalStoreEntry other && Equals(other);
        }

        /// <summary>Hash over the ordinal key and the version, consistent with <see cref="Equals(LocalStoreEntry)"/>.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key != null ? StringComparer.Ordinal.GetHashCode(Key) : 0) * 397) ^ Version;
            }
        }
    }
}
