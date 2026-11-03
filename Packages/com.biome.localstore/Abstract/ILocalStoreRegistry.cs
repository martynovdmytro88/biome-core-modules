using System;

namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// The caller-supplied <c>type → { key, version }</c> resolution the store consumes. The mapping is never
    /// carried by the type (no attribute, marker interface or base class) and never derived from the type name.
    /// Where the registry is authored is the caller's concern.
    /// </summary>
    public interface ILocalStoreRegistry
    {
        /// <summary>Resolves <paramref name="type"/>; <c>false</c> when the type is not registered.</summary>
        bool TryResolve(Type type, out LocalStoreEntry entry);
    }
}
