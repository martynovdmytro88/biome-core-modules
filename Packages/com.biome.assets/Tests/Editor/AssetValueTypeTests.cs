using System;
using Biome.Assets.Abstract;
using NUnit.Framework;

namespace Biome.Assets.Editor.Tests
{
    /// <summary>The value semantics of <see cref="AssetKey"/>, <see cref="AssetLabel"/> and <see cref="SceneHandle"/>.</summary>
    [TestFixture]
    public sealed class AssetValueTypeTests
    {
        [TestCase(null)]
        [TestCase("")]
        public void AssetKey_NullOrEmpty_Throws(string value)
        {
            Assert.Throws<ArgumentException>(() => new AssetKey(value));
        }

        [Test]
        public void AssetKey_Equality_IsOrdinal()
        {
            Assert.That(new AssetKey("key") == new AssetKey("key"), Is.True);
            Assert.That(new AssetKey("key").GetHashCode(), Is.EqualTo(new AssetKey("key").GetHashCode()));
            Assert.That(new AssetKey("key") != new AssetKey("KEY"), Is.True);
            Assert.That(new AssetKey("key").Equals((object)"key"), Is.False);
        }

        [Test]
        public void AssetKey_Default_HasNoValueAndAReadableName()
        {
            Assert.That(default(AssetKey).Value, Is.Null);
            Assert.That(default(AssetKey).GetHashCode(), Is.EqualTo(0));
            Assert.That(default(AssetKey).ToString(), Is.EqualTo("<default AssetKey>"));
            Assert.That(new AssetKey("key").ToString(), Is.EqualTo("key"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void AssetLabel_NullOrEmpty_Throws(string value)
        {
            Assert.Throws<ArgumentException>(() => new AssetLabel(value));
        }

        [Test]
        public void AssetLabel_Equality_IsOrdinal()
        {
            Assert.That(new AssetLabel("label") == new AssetLabel("label"), Is.True);
            Assert.That(new AssetLabel("label").GetHashCode(), Is.EqualTo(new AssetLabel("label").GetHashCode()));
            Assert.That(new AssetLabel("label") != new AssetLabel("LABEL"), Is.True);
            Assert.That(new AssetLabel("label").Equals((object)"label"), Is.False);
        }

        [Test]
        public void AssetLabel_Default_HasNoValueAndAReadableName()
        {
            Assert.That(default(AssetLabel).Value, Is.Null);
            Assert.That(default(AssetLabel).GetHashCode(), Is.EqualTo(0));
            Assert.That(default(AssetLabel).ToString(), Is.EqualTo("<default AssetLabel>"));
        }

        [Test]
        public void SceneHandle_Equality_NeedsBothKeyAndId()
        {
            var key = new AssetKey("scene");

            Assert.That(new SceneHandle(key, 1) == new SceneHandle(key, 1), Is.True);
            Assert.That(new SceneHandle(key, 1).GetHashCode(), Is.EqualTo(new SceneHandle(key, 1).GetHashCode()));
            Assert.That(new SceneHandle(key, 1) != new SceneHandle(key, 2), Is.True);
            Assert.That(new SceneHandle(key, 1) != new SceneHandle(new AssetKey("other"), 1), Is.True);
        }

        [Test]
        public void SceneHandle_ToStringAndDefault()
        {
            Assert.That(new SceneHandle(new AssetKey("scene"), 3).ToString(), Is.EqualTo("scene#3"));
            Assert.That(default(SceneHandle).Id, Is.EqualTo(0));
        }
    }
}
