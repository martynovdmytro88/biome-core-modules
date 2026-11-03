using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>The recording and filtering behaviour of <see cref="RecordingStateTransition"/>.</summary>
    [TestFixture]
    public sealed class RecordingStateTransitionTests
    {
        [Test]
        public void Applies_WithoutAFilter_IsTrueForEveryPair()
        {
            var transition = new RecordingStateTransition();

            Assert.That(transition.Applies(typeof(TestStateA), typeof(TestStateB)), Is.True);
            Assert.That(transition.Applies(null, typeof(TestStateA)), Is.True);
        }

        [Test]
        public void Applies_WithAFilter_FollowsIt()
        {
            var transition = new RecordingStateTransition(applies: (from, to) => to == typeof(TestStateB));

            Assert.That(transition.Applies(typeof(TestStateA), typeof(TestStateB)), Is.True);
            Assert.That(transition.Applies(typeof(TestStateB), typeof(TestStateC)), Is.False);
        }

        [UnityTest]
        public IEnumerator RunAsync_RecordsThePairAndWritesToTheJournal() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var transition = new RecordingStateTransition("Fade", journal);

            await transition.RunAsync(typeof(TestStateA), typeof(TestStateB), CancellationToken.None);

            Assert.That(transition.Runs, Is.EqualTo(new[] { (typeof(TestStateA), typeof(TestStateB)) }));
            Assert.That(journal, Is.EqualTo(new[] { "Fade.Run" }));
        });

        [Test]
        public void RunAsync_WithAFailure_ThrowsItAndStillRecords()
        {
            var transition = new RecordingStateTransition { Failure = new FormatException("run failed") };

            Assert.Throws<FormatException>(() => transition.RunAsync(typeof(TestStateA), typeof(TestStateB), CancellationToken.None));
            Assert.That(transition.Runs.Count, Is.EqualTo(1));
        }
    }
}
