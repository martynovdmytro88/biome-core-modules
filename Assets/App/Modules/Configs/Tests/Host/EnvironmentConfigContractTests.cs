using System;
using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using Biome.Configs.Editor.Tests;
using Biome.Configs.Fakes;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace Biome.Configs.Tests.Host
{
    /// <summary>Binds the package's environment contract to the production <see cref="EnvironmentConfig"/>.</summary>
    [TestFixture]
    public sealed class EnvironmentConfigContractTests : ConfigsEnvironmentContractTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyEnvironments()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        protected override IConfigsEnvironment CreateEnvironment(string id, params (Type Type, string Path)[] sources)
        {
            var raw = new (Type type, string path)[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                raw[i] = (sources[i].Type, sources[i].Path);
            }

            EnvironmentConfig environment = ConfigsTestEnvironment.CreateAsset(id, raw);
            _created.Add(environment);
            return environment;
        }
    }
}
