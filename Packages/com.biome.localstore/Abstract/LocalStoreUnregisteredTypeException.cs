using System;

namespace Biome.LocalStore.Abstract
{
    /// <summary>A type with no entry in the <see cref="ILocalStoreRegistry"/> was used — deliberately loud.</summary>
    public sealed class LocalStoreUnregisteredTypeException : Exception
    {
        public Type UnregisteredType { get; }

        public LocalStoreUnregisteredTypeException(Type type)
            : base($"LocalStore: type '{type?.FullName}' is not registered — add it to the ILocalStoreRegistry supplied at initialization.")
        {
            UnregisteredType = type;
        }
    }
}
