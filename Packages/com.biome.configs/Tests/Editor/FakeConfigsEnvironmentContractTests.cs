using System;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>Binds the environment contract to <see cref="FakeConfigsEnvironment"/>, plus its fail-fast rules.</summary>
    [TestFixture]
    public sealed class FakeConfigsEnvironmentContractTests : ConfigsEnvironmentContractTests
    {
        [Test]
        public void Add_SecondSourceForOneType_Throws()
        {
            FakeConfigsEnvironment environment = new FakeConfigsEnvironment("test").Add<TestConfigA>("a1");

            Assert.Throws<InvalidOperationException>(() => environment.Add<TestConfigA>("a2"));
        }

        [Test]
        public void TryGetSource_NullType_Throws()
        {
            var environment = new FakeConfigsEnvironment("test");

            Assert.Throws<ArgumentNullException>(() => environment.TryGetSource(null, out _));
        }

        protected override IConfigsEnvironment CreateEnvironment(string id, params (Type Type, string Path)[] sources)
        {
            var environment = new FakeConfigsEnvironment(id);
            foreach ((Type type, string path) in sources)
            {
                environment.Add(type, new ConfigSource(ConfigSourceKind.Resources, path));
            }

            return environment;
        }
    }
}
