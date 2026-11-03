using Biome.LocalStore.Abstract;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>
    /// The contract of the <see cref="ILocalStoreBackend"/> seam. The package binds it to its in-memory fake; a host binds
    /// its real backend by deriving and overriding <see cref="CreateBackend"/>. The suite deletes its own keys after each
    /// test, so a backend with persistent state stays clean.
    /// </summary>
    public abstract class LocalStoreBackendContractTests
    {
        private const string KeyA = "biome.localstore.tests.backend.a";
        private const string KeyB = "biome.localstore.tests.backend.b";

        private ILocalStoreBackend _backend;

        [SetUp]
        public void SetUp()
        {
            _backend = CreateBackend();
            DeleteOwnKeys();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteOwnKeys();
        }

        [Test]
        public void TryGet_AbsentKey_ReturnsFalse()
        {
            Assert.That(_backend.TryGet(KeyA, out _), Is.False);
            Assert.That(_backend.Has(KeyA), Is.False);
        }

        [Test]
        public void Set_ThenTryGet_ReturnsTheValue()
        {
            _backend.Set(KeyA, "first");

            Assert.That(_backend.TryGet(KeyA, out string value), Is.True);
            Assert.That(value, Is.EqualTo("first"));
            Assert.That(_backend.Has(KeyA), Is.True);
        }

        [Test]
        public void Set_ExistingKey_ReplacesTheValue()
        {
            _backend.Set(KeyA, "first");
            _backend.Set(KeyA, "second");

            _backend.TryGet(KeyA, out string value);

            Assert.That(value, Is.EqualTo("second"));
        }

        [Test]
        public void Delete_StoredKey_RemovesOnlyThatKey()
        {
            _backend.Set(KeyA, "first");
            _backend.Set(KeyB, "kept");

            _backend.Delete(KeyA);

            Assert.That(_backend.Has(KeyA), Is.False);
            Assert.That(_backend.TryGet(KeyB, out string value), Is.True);
            Assert.That(value, Is.EqualTo("kept"));
        }

        [Test]
        public void Delete_AbsentKey_IsANoOp()
        {
            Assert.DoesNotThrow(() => _backend.Delete(KeyA));
        }

        [Test]
        public void Save_KeepsTheStoredValues()
        {
            _backend.Set(KeyA, "first");

            _backend.Save();

            Assert.That(_backend.TryGet(KeyA, out string value), Is.True);
            Assert.That(value, Is.EqualTo("first"));
        }

        /// <summary>A fresh backend under test; called once per test.</summary>
        protected abstract ILocalStoreBackend CreateBackend();

        private void DeleteOwnKeys()
        {
            _backend.Delete(KeyA);
            _backend.Delete(KeyB);
        }
    }
}
