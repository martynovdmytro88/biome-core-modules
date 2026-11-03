using System;

namespace Biome.Assets.Abstract
{
    /// <summary>
    /// An engine-free asset key: wraps the Addressables runtime key (address string or GUID). Two keys are equal when
    /// their values are equal, ordinally.
    /// </summary>
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        /// <summary>Creates a key from an Addressables runtime key.</summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is null or empty.</exception>
        public AssetKey(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("An asset key must not be null or empty.", nameof(value));
            }

            Value = value;
        }

        /// <summary>The wrapped runtime key (address string or GUID); null only for a default <see cref="AssetKey"/>.</summary>
        public string Value { get; }

        /// <summary>True when both values are equal, ordinally.</summary>
        public bool Equals(AssetKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>True when <paramref name="obj"/> is an <see cref="AssetKey"/> equal to this one.</summary>
        public override bool Equals(object obj) => obj is AssetKey other && Equals(other);

        /// <summary>An ordinal hash of <see cref="Value"/>; 0 for a default key.</summary>
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        /// <summary><see cref="Value"/>, or "&lt;default AssetKey&gt;" for a default key.</summary>
        public override string ToString() => Value ?? "<default AssetKey>";

        /// <summary>Value equality.</summary>
        public static bool operator ==(AssetKey left, AssetKey right) => left.Equals(right);

        /// <summary>Value inequality.</summary>
        public static bool operator !=(AssetKey left, AssetKey right) => !left.Equals(right);
    }
}
