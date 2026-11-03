using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using Biome.LocalStore.Editor.Tests;
using Biome.LocalStore.Fakes;
using NUnit.Framework;

namespace Biome.LocalStore.Tests.Host
{
    /// <summary>Binds the package's service contract to the production core over the in-memory backend.</summary>
    [TestFixture]
    public sealed class InMemoryLocalStoreContractTests : LocalStoreContractTests
    {
        protected override ILocalStoreBackend CreateBackend() => new InMemoryLocalStoreBackend();

        protected override ILocalStoreService CreateStore(ILocalStoreBackend backend, ILocalStoreRegistry registry, LocalStoreLimits limits)
        {
            return new LocalStore(backend, registry, limits);
        }
    }
}
