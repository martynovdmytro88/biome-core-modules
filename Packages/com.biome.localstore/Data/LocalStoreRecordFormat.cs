namespace Biome.LocalStore.Data
{
    /// <summary>
    /// The on-backend shape of the module's data — format facts only, no serializer type.
    /// <list type="bullet">
    /// <item>Record: one JSON object <c>{"v":&lt;int&gt;,"d":&lt;payload&gt;}</c>. The version is per record, not per store.</item>
    /// <item>Manifest: one JSON array of the store's own (prefixed) backend keys, stored under the manifest key.</item>
    /// </list>
    /// </summary>
    public static class LocalStoreRecordFormat
    {
        /// <summary>Envelope field holding the per-record version (<c>int</c>).</summary>
        public const string VersionField = "v";

        /// <summary>Envelope field holding the serialized payload.</summary>
        public const string PayloadField = "d";
    }
}
