using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>Binds the provider contract to the fake's <see cref="FakeConfigsService.Provider{T}"/>.</summary>
    [TestFixture]
    public sealed class FakeConfigProviderContractTests : ConfigProviderContractTests
    {
        protected override IConfig<TestConfigA> CreateProvider(IConfigsReader reader)
        {
            return new FakeConfigsService.Provider<TestConfigA>(reader);
        }
    }
}
