using System.Linq;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using Biome.LocalStore.Editor.Tests;
using NUnit.Framework;

namespace Biome.LocalStore.Tests.Host
{
    /// <summary>
    /// The same contract on the real PlayerPrefs backend. The editor's PlayerPrefs are shared state: the fixture deletes
    /// exactly the keys it touched, and the suite's fixed keys before each test in case a run was cut short. It never calls
    /// <c>PlayerPrefs.DeleteAll()</c>.
    /// </summary>
    [TestFixture]
    public sealed class PlayerPrefsLocalStoreContractTests : LocalStoreContractTests
    {
        private static readonly string[] s_suiteKeys =
        {
            LocalStoreKeys.ManifestKey,
            LocalStoreKeys.Prefix + "audio",
            LocalStoreKeys.Prefix + "tutorial",
            LocalStoreKeys.Prefix + "big",
            LocalStoreKeys.Prefix + "orphan",
            "biome.localstore.tests.foreign",
        };

        protected override ILocalStoreBackend CreateBackend()
        {
            var backend = new PlayerPrefsLocalStoreBackend();
            foreach (string key in s_suiteKeys)
            {
                backend.Delete(key);
            }

            return backend;
        }

        protected override ILocalStoreService CreateStore(ILocalStoreBackend backend, ILocalStoreRegistry registry, LocalStoreLimits limits)
        {
            return new LocalStore(backend, registry, limits);
        }

        protected override void CleanUp(TrackingLocalStoreBackend backend)
        {
            foreach (string key in backend.TouchedKeys.ToArray())
            {
                backend.Delete(key);
            }

            backend.Save();
        }
    }
}
