using System;
using Biome.Configs.Config;
using Biome.Configs.Editor.Tests;
using Biome.Configs.Fakes;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace Biome.Configs.Tests.Host
{
    /// <summary>The <see cref="EnvironmentConfig"/>-only behaviour: the unresolvable-name rule and the duplicate guard.</summary>
    [TestFixture]
    public sealed class EnvironmentConfigTests
    {
        private EnvironmentConfig _environment;

        [TearDown]
        public void DestroyEnvironment()
        {
            if (_environment != null)
            {
                Object.DestroyImmediate(_environment);
            }
        }

        [Test]
        public void TryGetSource_UnresolvableTypeName_NeverMatches()
        {
            _environment = ConfigsTestEnvironment.CreateRawAsset("test", ("No.Such.Type, NoSuchAssembly", "x"), (string.Empty, "y"));

            Assert.IsFalse(_environment.TryGetSource(typeof(TestConfigA), out _));
        }

        [Test]
        public void TryGetSource_NullType_Throws()
        {
            _environment = ConfigsTestEnvironment.CreateAsset("test");

            Assert.Throws<ArgumentNullException>(() => _environment.TryGetSource(null, out _));
        }

        [Test]
        public void TryFindDuplicateType_OneSourcePerType_ReturnsFalse()
        {
            _environment = ConfigsTestEnvironment.CreateAsset("test", (typeof(TestConfigA), "a"), (typeof(TestConfigB), "b"));

            Assert.IsFalse(_environment.TryFindDuplicateType(out var duplicate));
            Assert.IsNull(duplicate);
        }

        [Test]
        public void TryFindDuplicateType_TwoSourcesForOneType_ReturnsThatTypeName()
        {
            _environment = ConfigsTestEnvironment.CreateAsset("test", (typeof(TestConfigA), "a1"), (typeof(TestConfigB), "b"), (typeof(TestConfigA), "a2"));

            Assert.IsTrue(_environment.TryFindDuplicateType(out var duplicate));
            Assert.AreEqual(typeof(TestConfigA).AssemblyQualifiedName, duplicate);
        }

        [Test]
        public void TryFindDuplicateType_BlankNames_AreIgnored()
        {
            _environment = ConfigsTestEnvironment.CreateRawAsset("test", (string.Empty, "x"), (string.Empty, "y"));

            Assert.IsFalse(_environment.TryFindDuplicateType(out _));
        }
    }
}
