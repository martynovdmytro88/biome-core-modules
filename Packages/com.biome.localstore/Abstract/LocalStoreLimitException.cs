using System;

namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// A configured limit (key length, per-value UTF-8 bytes, total UTF-8 budget) was violated by a write.
    /// Nothing was written; the store is in its previous state. Oversize data is a programming error.
    /// </summary>
    public sealed class LocalStoreLimitException : Exception
    {
        /// <summary>The prefixed backend key of the offending write.</summary>
        public string Key { get; }
        /// <summary>The measured size (bytes or characters, per <see cref="Limit"/>).</summary>
        public long Actual { get; }
        /// <summary>The limit that was exceeded.</summary>
        public long Limit { get; }

        public LocalStoreLimitException(string key, long actual, long limit, string message)
            : base(message)
        {
            Key = key;
            Actual = actual;
            Limit = limit;
        }
    }
}
