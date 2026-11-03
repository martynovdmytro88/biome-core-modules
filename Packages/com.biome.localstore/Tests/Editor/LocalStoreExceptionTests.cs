using Biome.LocalStore.Abstract;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>What the contract's two exceptions carry for the caller.</summary>
    [TestFixture]
    public sealed class LocalStoreExceptionTests
    {
        [Test]
        public void LimitException_CarriesKeySizesAndMessage()
        {
            var exception = new LocalStoreLimitException("ls.audio", 70, 64, "too big");

            Assert.That(exception.Key, Is.EqualTo("ls.audio"));
            Assert.That(exception.Actual, Is.EqualTo(70));
            Assert.That(exception.Limit, Is.EqualTo(64));
            Assert.That(exception.Message, Is.EqualTo("too big"));
        }

        [Test]
        public void UnregisteredTypeException_NamesTheType()
        {
            var exception = new LocalStoreUnregisteredTypeException(typeof(string));

            Assert.That(exception.UnregisteredType, Is.EqualTo(typeof(string)));
            Assert.That(exception.Message, Does.Contain("System.String"));
        }
    }
}
