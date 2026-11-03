using System;

namespace Biome.Assets.Abstract
{
    /// <summary>
    /// An opaque handle to one scene instance loaded through an <see cref="IAssetsContext"/>: the key it was loaded
    /// from plus an id unique to the instance. Two handles are equal when key and id are equal; a default handle
    /// belongs to no context.
    /// </summary>
    public readonly struct SceneHandle : IEquatable<SceneHandle>
    {
        /// <summary>Creates a handle; contexts mint them, callers only pass them back.</summary>
        public SceneHandle(AssetKey key, int id)
        {
            Key = key;
            Id = id;
        }

        /// <summary>The key the scene was loaded from.</summary>
        public AssetKey Key { get; }

        /// <summary>The id of this instance; 0 only for a default handle.</summary>
        public int Id { get; }

        /// <summary>True when both key and id are equal.</summary>
        public bool Equals(SceneHandle other) => Id == other.Id && Key.Equals(other.Key);

        /// <summary>True when <paramref name="obj"/> is a <see cref="SceneHandle"/> equal to this one.</summary>
        public override bool Equals(object obj) => obj is SceneHandle other && Equals(other);

        /// <summary>A hash of key and id.</summary>
        public override int GetHashCode() => (Key.GetHashCode() * 397) ^ Id;

        /// <summary>The key and the id, for logs.</summary>
        public override string ToString() => $"{Key}#{Id}";

        /// <summary>Value equality.</summary>
        public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);

        /// <summary>Value inequality.</summary>
        public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);
    }
}
