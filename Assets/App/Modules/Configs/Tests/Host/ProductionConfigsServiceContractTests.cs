using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.Configs.Data;
using Biome.Configs.Editor.Tests;
using NUnit.Framework;

namespace Biome.Configs.Tests.Host
{
    /// <summary>Binds the package's service contract to the production <see cref="ConfigsService"/>.</summary>
    [TestFixture]
    public sealed class ProductionConfigsServiceContractTests : ConfigsServiceContractTests
    {
        protected override IConfigsService CreateService(IReadOnlyList<IConfigLoader> loaders, IReadOnlyList<object> baseConfigs)
        {
            return new ConfigsService(new ConfigStore(), new ResourcesConfigSource(), loaders, baseConfigs);
        }
    }
}
