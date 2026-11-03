using System;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;

namespace Biome.LocalStore.Fakes
{
    /// <summary>
    /// The public entry to the fake: the real LocalStore core over the in-memory seam fake, so that a consumer's
    /// tests and the mock profile run the production logic with no PlayerPrefs. Limits are
    /// <see cref="LocalStoreLimits.Placeholder"/>.
    /// </summary>
    public static class InMemoryLocalStore
    {
        /// <summary>A fresh, empty store over a new in-memory backend.</summary>
        public static ILocalStoreService Create(ILocalStoreRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return new LocalStore(new InMemoryLocalStoreBackend(), registry, LocalStoreLimits.Placeholder);
        }
    }
}
