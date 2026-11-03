using Biome.PlayerData.Abstract;
using Biome.PlayerData.Fakes;
using NUnit.Framework;

namespace Biome.PlayerData.Editor.Tests.Consumer
{
    /// <summary>
    /// Stands in for a consumer module's test assembly: it references only Abstract and Fakes, and drives its first-launch
    /// branch through the in-memory fake.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryPlayerDataConsumerTests
    {
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void Consumer_ChoosesItsBranchThroughTheFake_WithOnlyTheAbstractReference(bool hasLaunched, bool expectsTutorial)
        {
            var service = new InMemoryPlayerDataService(hasLaunched);

            bool showsTutorial = new TutorialGate(service).ShouldShow();

            Assert.That(showsTutorial, Is.EqualTo(expectsTutorial));
            Assert.That(service.IsLaunchMarked, Is.True);
        }

        private sealed class TutorialGate
        {
            private readonly IPlayerDataService _playerData;

            public TutorialGate(IPlayerDataService playerData)
            {
                _playerData = playerData;
            }

            public bool ShouldShow()
            {
                _playerData.MarkLaunched();
                return _playerData.IsFirstLaunch;
            }
        }
    }
}
