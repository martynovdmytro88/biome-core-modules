using System;
using Biome.Configs.Abstract;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>The source value (equality by kind and path, a null path refused) and what the two exceptions carry.</summary>
    [TestFixture]
    public sealed class ConfigValueTypeTests
    {
        [Test]
        public void ConfigSource_SameKindAndPath_AreEqualWithOneHash()
        {
            var left = new ConfigSource(ConfigSourceKind.Resources, "a/b");
            var right = new ConfigSource(ConfigSourceKind.Resources, "a/b");

            Assert.AreEqual(left, right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void ConfigSource_DifferentPath_AreNotEqual()
        {
            var left = new ConfigSource(ConfigSourceKind.Resources, "a");
            var right = new ConfigSource(ConfigSourceKind.Resources, "b");

            Assert.AreNotEqual(left, right);
        }

        [Test]
        public void ConfigSource_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigSource(ConfigSourceKind.Resources, null));
        }

        [Test]
        public void ConfigLoadException_Constructed_CarriesTypeAttemptsAndInner()
        {
            var inner = new InvalidOperationException("inner");

            var exception = new ConfigLoadException(typeof(TestConfigA), 3, inner);

            Assert.AreEqual(typeof(TestConfigA), exception.ConfigType);
            Assert.AreEqual(3, exception.Attempts);
            Assert.AreSame(inner, exception.InnerException);
        }

        [Test]
        public void ConfigNotInitializedException_Constructed_CarriesTheType()
        {
            var exception = new ConfigNotInitializedException(typeof(TestConfigA));

            Assert.AreEqual(typeof(TestConfigA), exception.ConfigType);
            Assert.IsInstanceOf<InvalidOperationException>(exception);
        }
    }
}
