namespace Biome.LocalStore.Config
{
    /// <summary>
    /// Key-space configuration. Every key the store writes carries <see cref="Prefix"/>; the manifest of own keys
    /// lives under <see cref="ManifestKey"/>. Prefix + manifest are two independent guarantees that the module never
    /// touches foreign data.
    /// </summary>
    public static class LocalStoreKeys
    {
        public const string Prefix = "ls.";
        public const string ManifestKey = "ls.__manifest";
    }
}
