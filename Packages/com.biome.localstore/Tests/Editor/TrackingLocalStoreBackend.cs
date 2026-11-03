using System.Collections.Generic;
using System.Linq;
using Biome.LocalStore.Abstract;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>
    /// Wraps the backend under test so the contract suite reads the same on every backend: the test plants and inspects
    /// raw records through it, it counts <see cref="Save"/> calls, and it remembers every key ever written or deleted,
    /// so <see cref="Count"/> works on a backend that cannot enumerate and a fixture can clean exactly its own keys.
    /// </summary>
    public sealed class TrackingLocalStoreBackend : ILocalStoreBackend
    {
        private readonly ILocalStoreBackend _inner;
        private readonly HashSet<string> _touched = new HashSet<string>();

        public TrackingLocalStoreBackend(ILocalStoreBackend inner)
        {
            _inner = inner;
        }

        /// <summary>How many times <see cref="Save"/> was called.</summary>
        public int SaveCount { get; private set; }

        /// <summary>Keys written or deleted through this wrapper, present or not.</summary>
        public IEnumerable<string> TouchedKeys => _touched;

        /// <summary>Records currently on the backend among the touched keys.</summary>
        public int Count => _touched.Count(_inner.Has);

        /// <summary>The raw record; throws when absent (the test expects it there).</summary>
        public string Read(string key) => _inner.TryGet(key, out string value) ? value : throw new KeyNotFoundException(key);

        public bool TryGet(string key, out string value) => _inner.TryGet(key, out value);

        public void Set(string key, string value)
        {
            _touched.Add(key);
            _inner.Set(key, value);
        }

        public void Delete(string key)
        {
            _touched.Add(key);
            _inner.Delete(key);
        }

        public bool Has(string key) => _inner.Has(key);

        public void Save()
        {
            SaveCount++;
            _inner.Save();
        }
    }
}
