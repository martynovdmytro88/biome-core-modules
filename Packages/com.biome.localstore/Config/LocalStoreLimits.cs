namespace Biome.LocalStore.Config
{
    /// <summary>
    /// The store's own size budget — enforced by the module, not borrowed from a platform (backend ceilings differ
    /// per platform and across engine versions). Sizes are UTF-8 bytes of the serialized record, not characters.
    /// </summary>
    public readonly struct LocalStoreLimits
    {
        /// <summary>Maximum length (characters) of a prefixed backend key.</summary>
        public readonly int MaxKeyLength;
        /// <summary>Maximum UTF-8 byte size of one serialized record.</summary>
        public readonly int MaxValueBytes;
        /// <summary>Maximum summed UTF-8 byte size of all records the store owns (the manifest itself is not counted).</summary>
        public readonly long MaxTotalBytes;

        public LocalStoreLimits(int maxKeyLength, int maxValueBytes, long maxTotalBytes)
        {
            MaxKeyLength = maxKeyLength;
            MaxValueBytes = maxValueBytes;
            MaxTotalBytes = maxTotalBytes;
        }

        /// <summary>
        /// PLACEHOLDER — concrete figures are an operator decision (design §Open 1: verify against current backend
        /// documentation, then fix). These values only make the module compile and must not be read as verified.
        /// </summary>
        public static LocalStoreLimits Placeholder => new LocalStoreLimits(
            maxKeyLength: PlaceholderMaxKeyLength,
            maxValueBytes: PlaceholderMaxValueBytes,
            maxTotalBytes: PlaceholderMaxTotalBytes);

        // TODO(operator): fix the concrete limit values (seed Open 1). Placeholders, not decisions.
        public const int PlaceholderMaxKeyLength = 64;
        public const int PlaceholderMaxValueBytes = 8 * 1024;
        public const long PlaceholderMaxTotalBytes = 64 * 1024;
    }
}
