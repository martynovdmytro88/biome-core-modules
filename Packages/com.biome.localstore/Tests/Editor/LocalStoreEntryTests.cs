using Biome.LocalStore.Abstract;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>The value semantics of <see cref="LocalStoreEntry"/>.</summary>
    [TestFixture]
    public sealed class LocalStoreEntryTests
    {
        [Test]
        public void Equals_SameKeyAndVersion_IsEqualWithTheSameHash()
        {
            var first = new LocalStoreEntry("audio", 2);
            var second = new LocalStoreEntry("audio", 2);

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [TestCase("audio", 3, TestName = "Equals_DifferentVersion_IsNotEqual")]
        [TestCase("Audio", 2, TestName = "Equals_KeyDiffersByCase_IsNotEqual")]
        public void Equals_Different_IsNotEqual(string key, int version)
        {
            Assert.That(new LocalStoreEntry("audio", 2).Equals(new LocalStoreEntry(key, version)), Is.False);
        }

        [Test]
        public void Equals_AnotherType_IsNotEqual()
        {
            Assert.That(new LocalStoreEntry("audio", 2).Equals("audio"), Is.False);
        }

        [Test]
        public void GetHashCode_DefaultEntry_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => default(LocalStoreEntry).GetHashCode());
        }
    }
}
