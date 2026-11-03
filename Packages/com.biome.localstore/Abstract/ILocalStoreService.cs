namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// The public contract of the LocalStore module: small typed key-value persistence for short-lived,
    /// low-volume, non-critical data (settings, flags, UI preferences). Not a save-game system.
    /// The storage key and record version of <typeparamref name="T"/> come from the caller-supplied
    /// <see cref="ILocalStoreRegistry"/>; an unregistered type fails loudly with
    /// <see cref="LocalStoreUnregisteredTypeException"/> on every member.
    /// <para><b>Main thread only.</b> No async, no encryption, no sync, no migrations.</para>
    /// </summary>
    public interface ILocalStoreService
    {
        /// <summary>
        /// Reads the record of <typeparamref name="T"/>. Returns <c>true</c> only for <see cref="ReadStatus.Ok"/>;
        /// never throws on data (bad data surfaces as <paramref name="status"/>). On any non-Ok status
        /// <paramref name="value"/> is <c>default</c>.
        /// </summary>
        bool TryGet<T>(out T value, out ReadStatus status);

        /// <summary>
        /// Serializes and writes <paramref name="value"/> under the registered key with the registered version.
        /// Throws <see cref="LocalStoreLimitException"/> when a configured limit is violated — the value is not
        /// written and the store keeps its previous state (oversize data is a programming error, not a runtime condition).
        /// </summary>
        void Set<T>(T value);

        /// <summary>Whether a record of <typeparamref name="T"/> exists on the backend (no content check).</summary>
        bool Has<T>();

        /// <summary>Removes the record of <typeparamref name="T"/>; a no-op when absent.</summary>
        void Remove<T>();

        /// <summary>Removes every record this store owns — walks its own manifest, never the backend's bulk delete.</summary>
        void Clear();

        /// <summary>Persists pending backend writes. The caller decides when; the module has no lifecycle hooks.</summary>
        void Flush();
    }
}
