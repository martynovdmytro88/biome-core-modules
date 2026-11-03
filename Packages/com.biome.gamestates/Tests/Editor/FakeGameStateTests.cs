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
    /// <summary>The recording, gating and failing behaviour of <see cref="FakeGameState"/>.</summary>
    [TestFixture]
    public sealed class FakeGameStateTests
    {
        [UnityTest]
        public IEnumerator EnterAndExit_CountAndWriteToTheJournal() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var state = new FakeGameState("State", journal);

            await state.EnterAsync(CancellationToken.None);
            await state.ExitAsync(CancellationToken.None);

            Assert.That(state.EnterCount, Is.EqualTo(1));
            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(journal, Is.EqualTo(new[] { "State.Enter", "State.Exit" }));
        });

        [UnityTest]
        public IEnumerator Journal_WithoutAName_UsesTheTypeName() => UniTask.ToCoroutine(async () =>
        {
            var journal = new List<string>();
            var state = new FakeGameState(journal: journal);

            await state.EnterAsync(CancellationToken.None);

            Assert.That(journal, Is.EqualTo(new[] { nameof(FakeGameState) + ".Enter" }));
        });

        [UnityTest]
        public IEnumerator EnterAsync_WithAGate_WaitsUntilItCompletes() => UniTask.ToCoroutine(async () =>
        {
            var state = new FakeGameState { EnterGate = new UniTaskCompletionSource() };

            UniTask run = state.EnterAsync(CancellationToken.None);
            Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending));

            state.EnterGate.TrySetResult();
            await run;

            Assert.That(state.EnterCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator EnterAsync_CancelledToken_Cancels() => UniTask.ToCoroutine(async () =>
        {
            var state = new FakeGameState();
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                Assert.That(await ThrowsAsync<OperationCanceledException>(() => state.EnterAsync(cts.Token)), Is.True);
            }
        });

        [UnityTest]
        public IEnumerator EnterAsync_WithAFailure_ThrowsIt() => UniTask.ToCoroutine(async () =>
        {
            var state = new FakeGameState { EnterFailure = new FormatException("enter failed") };

            Assert.That(await ThrowsAsync<FormatException>(() => state.EnterAsync(CancellationToken.None)), Is.True);
        });

        [UnityTest]
        public IEnumerator ExitAsync_WithAFailure_ThrowsIt() => UniTask.ToCoroutine(async () =>
        {
            var state = new FakeGameState { ExitFailure = new FormatException("exit failed") };

            Assert.That(await ThrowsAsync<FormatException>(() => state.ExitAsync(CancellationToken.None)), Is.True);
            Assert.That(state.ExitCount, Is.EqualTo(1));
        });

        private static async UniTask<bool> ThrowsAsync<TException>(Func<UniTask> action) where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException)
            {
                return true;
            }

            return false;
        }
    }
}
