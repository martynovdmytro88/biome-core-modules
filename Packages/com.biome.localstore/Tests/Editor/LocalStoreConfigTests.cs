using Biome.LocalStore.Config;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>The key-space and limit facts of the package's config; the figures themselves stay placeholders.</summary>
    [TestFixture]
    public sealed class LocalStoreConfigTests
    {
        [Test]
        public void ManifestKey_CarriesThePrefix()
        {
            Assert.That(LocalStoreKeys.ManifestKey, Does.StartWith(LocalStoreKeys.Prefix));
        }

        [Test]
        public void Limits_Constructor_KeepsEveryValue()
        {
            var limits = new LocalStoreLimits(1, 2, 3);

            Assert.That(limits.MaxKeyLength, Is.EqualTo(1));
            Assert.That(limits.MaxValueBytes, Is.EqualTo(2));
            Assert.That(limits.MaxTotalBytes, Is.EqualTo(3));
        }

        [Test]
        public void Limits_Placeholder_UsesTheNamedConstants()
        {
            LocalStoreLimits limits = LocalStoreLimits.Placeholder;

            Assert.That(limits.MaxKeyLength, Is.EqualTo(LocalStoreLimits.PlaceholderMaxKeyLength));
            Assert.That(limits.MaxValueBytes, Is.EqualTo(LocalStoreLimits.PlaceholderMaxValueBytes));
            Assert.That(limits.MaxTotalBytes, Is.EqualTo(LocalStoreLimits.PlaceholderMaxTotalBytes));
        }
    }
}
