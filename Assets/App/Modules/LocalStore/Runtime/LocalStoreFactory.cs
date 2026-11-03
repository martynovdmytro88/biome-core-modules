using System;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;

namespace Biome.LocalStore
{
    /// <summary>The public entry to the store: builds an <see cref="ILocalStoreService"/> over a real backend.</summary>
    public static class LocalStoreFactory
    {
        /// <summary>The store over <c>UnityEngine.PlayerPrefs</c>; reads its manifest from the backend on construction.</summary>
        public static ILocalStoreService CreatePlayerPrefs(ILocalStoreRegistry registry, LocalStoreLimits limits)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return new LocalStore(new PlayerPrefsLocalStoreBackend(), registry, limits);
        }
    }
}
