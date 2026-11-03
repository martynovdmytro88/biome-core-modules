using Biome.LocalStore.Abstract;
using Biome.LocalStore.Editor.Tests;
using NUnit.Framework;

namespace Biome.LocalStore.Tests.Host
{
    /// <summary>Binds the package's seam contract to the production PlayerPrefs backend.</summary>
    [TestFixture]
    public sealed class PlayerPrefsLocalStoreBackendContractTests : LocalStoreBackendContractTests
    {
        protected override ILocalStoreBackend CreateBackend() => new PlayerPrefsLocalStoreBackend();
    }
}
