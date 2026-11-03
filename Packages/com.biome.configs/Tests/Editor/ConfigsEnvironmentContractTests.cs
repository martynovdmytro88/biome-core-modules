using System;
using Biome.Configs.Abstract;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>
    /// The environment contract written once over <see cref="IConfigsEnvironment"/>: a source is found by config type, and
    /// a type the environment does not name has none. A host binds its environment by overriding
    /// <see cref="CreateEnvironment"/> and destroys what it created in its own teardown.
    /// </summary>
    public abstract class ConfigsEnvironmentContractTests
    {
        [Test]
        public void Id_Created_IsTheEnvironmentId()
        {
            IConfigsEnvironment environment = CreateEnvironment("test", (typeof(TestConfigA), "a"));

            Assert.AreEqual("test", environment.Id);
        }

        [Test]
        public void TryGetSource_ByType_ReturnsThatTypesSource()
        {
            IConfigsEnvironment environment = CreateEnvironment("test", (typeof(TestConfigA), "path/a"), (typeof(TestConfigB), "path/b"));

            Assert.IsTrue(environment.TryGetSource(typeof(TestConfigB), out var source));
            Assert.AreEqual(new ConfigSource(ConfigSourceKind.Resources, "path/b"), source);
        }

        [Test]
        public void TryGetSource_TypeNotNamed_ReturnsFalse()
        {
            IConfigsEnvironment environment = CreateEnvironment("test", (typeof(TestConfigA), "path/a"));

            Assert.IsFalse(environment.TryGetSource(typeof(TestConfigB), out _));
        }

        /// <summary>An environment of the implementation under test with one Resources source per (type, path) pair.</summary>
        protected abstract IConfigsEnvironment CreateEnvironment(string id, params (Type Type, string Path)[] sources);
    }
}
