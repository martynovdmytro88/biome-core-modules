namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// The <c>string → string</c> backend seam the core stores raw records behind. It exists so tests (in-memory) and
    /// any future alternative source never touch PlayerPrefs directly.
    /// Keys handed in are already prefixed; values are the raw serialized strings.
    /// </summary>
    public interface ILocalStoreBackend
    {
        bool TryGet(string key, out string value);
        void Set(string key, string value);
        void Delete(string key);
        bool Has(string key);
        /// <summary>Persist pending writes (PlayerPrefs.Save on the real backend).</summary>
        void Save();
    }
}
