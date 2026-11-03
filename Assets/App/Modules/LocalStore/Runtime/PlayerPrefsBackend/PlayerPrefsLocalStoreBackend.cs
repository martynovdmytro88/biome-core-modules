using Biome.LocalStore.Abstract;
using UnityEngine;

namespace Biome.LocalStore
{
    /// <summary>
    /// The production backend: the seam mapped one to one onto <see cref="PlayerPrefs"/>. The only engine-touching
    /// code of the store. Keys arrive already prefixed; main thread only (PlayerPrefs is).
    /// </summary>
    internal sealed class PlayerPrefsLocalStoreBackend : ILocalStoreBackend
    {
        public bool TryGet(string key, out string value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
                return true;
            }

            value = null;
            return false;
        }

        public void Set(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public bool Has(string key) => PlayerPrefs.HasKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}
