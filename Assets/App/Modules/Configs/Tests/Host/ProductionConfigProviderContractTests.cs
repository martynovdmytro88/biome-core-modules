using Biome.Configs.Abstract;
using Biome.Configs.Editor.Tests;
using NUnit.Framework;

namespace Biome.Configs.Tests.Host
{
    /// <summary>Binds the package's provider contract to the production <see cref="ConfigProvider{T}"/>.</summary>
    [TestFixture]
    public sealed class ProductionConfigProviderContractTests : ConfigProviderContractTests
    {
        protected override IConfig<TestConfigA> CreateProvider(IConfigsReader reader)
        {
            return new ConfigProvider<TestConfigA>(reader);
        }
    }
}
