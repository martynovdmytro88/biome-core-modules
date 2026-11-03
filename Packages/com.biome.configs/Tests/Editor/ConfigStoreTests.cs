using System;
using Biome.Configs.Data;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>The store: put and get by exact type, immutable once frozen.</summary>
    [TestFixture]
    public sealed class ConfigStoreTests
    {
        [Test]
        public void Put_ThenTryGet_ReturnsTheInstanceByType()
        {
            var store = new ConfigStore();
            var config = new TestConfigA(1);

            store.Put(typeof(TestConfigA), config);

            Assert.IsTrue(store.TryGet(typeof(TestConfigA), out var found));
            Assert.AreSame(config, found);
        }

        [Test]
        public void TryGet_TypeNeverPut_ReturnsFalse()
        {
            var store = new ConfigStore();
            store.Put(typeof(TestConfigA), new TestConfigA(1));

            Assert.IsFalse(store.TryGet(typeof(TestConfigB), out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void Put_TwoTypes_KeepsEachApart()
        {
            var store = new ConfigStore();
            var a = new TestConfigA(1);
            var b = new TestConfigB("b");

            store.Put(typeof(TestConfigA), a);
            store.Put(typeof(TestConfigB), b);

            Assert.IsTrue(store.TryGet(typeof(TestConfigA), out var foundA));
            Assert.IsTrue(store.TryGet(typeof(TestConfigB), out var foundB));
            Assert.AreSame(a, foundA);
            Assert.AreSame(b, foundB);
        }

        [Test]
        public void Freeze_SetsIsFrozen_AndKeepsTheContent()
        {
            var store = new ConfigStore();
            store.Put(typeof(TestConfigA), new TestConfigA(1));
            Assert.IsFalse(store.IsFrozen);

            store.Freeze();
            store.Freeze();

            Assert.IsTrue(store.IsFrozen);
            Assert.IsTrue(store.TryGet(typeof(TestConfigA), out _));
        }

        [Test]
        public void Put_AfterFreeze_Throws()
        {
            var store = new ConfigStore();
            store.Freeze();

            Assert.Throws<InvalidOperationException>(() => store.Put(typeof(TestConfigA), new TestConfigA(1)));
            Assert.IsFalse(store.TryGet(typeof(TestConfigA), out _));
        }

        [Test]
        public void Put_SecondInstanceOfOneType_Throws()
        {
            var store = new ConfigStore();
            var first = new TestConfigA(1);
            store.Put(typeof(TestConfigA), first);

            Assert.Throws<InvalidOperationException>(() => store.Put(typeof(TestConfigA), new TestConfigA(2)));
            store.TryGet(typeof(TestConfigA), out var found);
            Assert.AreSame(first, found);
        }

        [Test]
        public void Put_NullArguments_Throw()
        {
            var store = new ConfigStore();

            Assert.Throws<ArgumentNullException>(() => store.Put(null, new TestConfigA(1)));
            Assert.Throws<ArgumentNullException>(() => store.Put(typeof(TestConfigA), null));
        }

        [Test]
        public void Put_InstanceOfAnotherType_Throws()
        {
            var store = new ConfigStore();

            Assert.Throws<ArgumentException>(() => store.Put(typeof(TestConfigA), new TestConfigB("b")));
        }

        [Test]
        public void TryGet_NullType_Throws()
        {
            var store = new ConfigStore();

            Assert.Throws<ArgumentNullException>(() => store.TryGet(null, out _));
        }
    }
}
