using Biome.LocalStore.Abstract;
using Biome.LocalStore.Fakes;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>Binds the seam contract to the package's in-memory fake, plus the fake's own surface.</summary>
    [TestFixture]
    public sealed class InMemoryLocalStoreBackendContractTests : LocalStoreBackendContractTests
    {
        [Test]
        public void Fake_RawExposesTheStoredRecords()
        {
            var backend = new InMemoryLocalStoreBackend();

            backend.Set("key", "value");
            backend.Raw["planted"] = "outside";

            Assert.That(backend.Raw["key"], Is.EqualTo("value"));
            Assert.That(backend.TryGet("planted", out string planted), Is.True);
            Assert.That(planted, Is.EqualTo("outside"));
        }

        [Test]
        public void Fake_SaveCount_CountsEverySave()
        {
            var backend = new InMemoryLocalStoreBackend();

            backend.Set("key", "value");
            Assert.That(backend.SaveCount, Is.EqualTo(0), "a write is not a save");

            backend.Save();
            backend.Save();

            Assert.That(backend.SaveCount, Is.EqualTo(2));
        }

        protected override ILocalStoreBackend CreateBackend() => new InMemoryLocalStoreBackend();
    }
}
