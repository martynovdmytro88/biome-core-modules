using System.Collections.Generic;
using Biome.LocalStore.Abstract;

namespace Biome.LocalStore.Fakes
{
    /// <summary>
    /// The fake: the in-memory implementation of the backend seam the contract tests run against. A plain dictionary
    /// of raw strings with no external state, exposed so a test can plant corrupt payloads, wrong versions or foreign
    /// keys and delete a record from outside to prove manifest self-healing.
    /// </summary>
    public sealed class InMemoryLocalStoreBackend : ILocalStoreBackend
    {
        /// <summary>The raw store — tests read and write it directly to simulate the outside world.</summary>
        public readonly Dictionary<string, string> Raw = new Dictionary<string, string>();

        /// <summary>How many times <see cref="Save"/> was called (Flush is caller-driven; nothing else may save).</summary>
        public int SaveCount { get; private set; }

        public bool TryGet(string key, out string value) => Raw.TryGetValue(key, out value);
        public void Set(string key, string value) => Raw[key] = value;
        public void Delete(string key) => Raw.Remove(key);
        public bool Has(string key) => Raw.ContainsKey(key);
        public void Save() => SaveCount++;
    }
}
