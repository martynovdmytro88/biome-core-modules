using System;

namespace Biome.Configs.Abstract
{
    /// <summary>Where one config asset lives: a source kind and a path inside it.</summary>
    public readonly struct ConfigSource : IEquatable<ConfigSource>
    {
        /// <summary>Creates a source. Throws <see cref="ArgumentNullException"/> when <paramref name="path"/> is null.</summary>
        public ConfigSource(ConfigSourceKind kind, string path)
        {
            Kind = kind;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>The kind of source the path is read from.</summary>
        public ConfigSourceKind Kind { get; }

        /// <summary>The path inside the source; its form depends on <see cref="Kind"/>.</summary>
        public string Path { get; }

        /// <inheritdoc />
        public bool Equals(ConfigSource other)
        {
            return Kind == other.Kind && string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ConfigSource other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine((int)Kind, Path);
        }
    }
}
