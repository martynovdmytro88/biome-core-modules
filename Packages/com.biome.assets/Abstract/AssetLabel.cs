using System;

namespace Biome.Assets.Abstract
{
    /// <summary>An engine-free asset label (an Addressables label): a name a group of keys is tagged with.</summary>
    public readonly struct AssetLabel : IEquatable<AssetLabel>
    {
        /// <summary>Creates a label from an Addressables label name.</summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is null or empty.</exception>
        public AssetLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("An asset label must not be null or empty.", nameof(value));
            }

            Value = value;
        }

        /// <summary>The label name; null only for a default <see cref="AssetLabel"/>.</summary>
        public string Value { get; }

        /// <summary>True when both values are equal, ordinally.</summary>
        public bool Equals(AssetLabel other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <summary>True when <paramref name="obj"/> is an <see cref="AssetLabel"/> equal to this one.</summary>
        public override bool Equals(object obj) => obj is AssetLabel other && Equals(other);

        /// <summary>An ordinal hash of <see cref="Value"/>; 0 for a default label.</summary>
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        /// <summary><see cref="Value"/>, or "&lt;default AssetLabel&gt;" for a default label.</summary>
        public override string ToString() => Value ?? "<default AssetLabel>";

        /// <summary>Value equality.</summary>
        public static bool operator ==(AssetLabel left, AssetLabel right) => left.Equals(right);

        /// <summary>Value inequality.</summary>
        public static bool operator !=(AssetLabel left, AssetLabel right) => !left.Equals(right);
    }
}
