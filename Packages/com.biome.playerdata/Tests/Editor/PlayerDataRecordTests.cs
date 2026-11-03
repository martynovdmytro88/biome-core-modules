using Biome.PlayerData.Data;
using NUnit.Framework;

namespace Biome.PlayerData.Editor.Tests
{
    /// <summary>The record's default: a player with no record has not launched.</summary>
    [TestFixture]
    public sealed class PlayerDataRecordTests
    {
        [Test]
        public void HasLaunched_DefaultRecord_IsFalse()
        {
            Assert.IsFalse(default(PlayerDataRecord).HasLaunched);
        }
    }
}
