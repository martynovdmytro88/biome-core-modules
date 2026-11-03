using System;
using System.Collections.Generic;
using Biome.LocalStore.Abstract;

namespace Biome.LocalStore
{
    /// <summary>
    /// A registry built in code: a plain dictionary of <c>type → entry</c>. The composer builds the app's one; registry
    /// authoring (file format, editor tool, key-reuse validation) is a separate decision and not part of this class.
    /// </summary>
    public sealed class DictionaryLocalStoreRegistry : ILocalStoreRegistry
    {
        private readonly Dictionary<Type, LocalStoreEntry> _entries = new Dictionary<Type, LocalStoreEntry>();

        public DictionaryLocalStoreRegistry Add<T>(string key, int version)
        {
            _entries[typeof(T)] = new LocalStoreEntry(key, version);
            return this;
        }

        public bool TryResolve(Type type, out LocalStoreEntry entry) => _entries.TryGetValue(type, out entry);
    }
}
