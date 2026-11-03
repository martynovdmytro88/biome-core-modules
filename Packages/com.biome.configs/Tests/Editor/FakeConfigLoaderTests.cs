using System;
using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>The scripted behaviours of <see cref="FakeConfigLoader"/>.</summary>
    [TestFixture]
    public sealed class FakeConfigLoaderTests
    {
        private static readonly ConfigSource s_source = new ConfigSource(ConfigSourceKind.Resources, "a");

        [UnityTest]
        public IEnumerator LoadAsync_Succeeding_ReturnsTheConfigAndRecordsTheCall() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            FakeConfigLoader loader = FakeConfigLoader.Succeeding(config);

            object result = await loader.LoadAsync(s_source, null, CancellationToken.None);

            Assert.AreEqual(typeof(TestConfigA), loader.ConfigType);
            Assert.AreSame(config, result);
            Assert.AreEqual(1, loader.Attempts);
            Assert.AreEqual(s_source, loader.LastSource);
        });

        [UnityTest]
        public IEnumerator LoadAsync_FailingTwice_FailsTwiceThenReturnsTheConfig() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            FakeConfigLoader loader = FakeConfigLoader.FailingTimes(2, config);

            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(async () => await loader.LoadAsync(s_source, null, CancellationToken.None));
            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(async () => await loader.LoadAsync(s_source, null, CancellationToken.None));
            object result = await loader.LoadAsync(s_source, null, CancellationToken.None);

            Assert.AreSame(config, result);
            Assert.AreEqual(3, loader.Attempts);
        });

        [UnityTest]
        public IEnumerator LoadAsync_Delayed_StaysPendingThenReturnsTheConfig() => UniTask.ToCoroutine(async () =>
        {
            var config = new TestConfigA(1);
            FakeConfigLoader loader = FakeConfigLoader.Delayed(config, TimeSpan.FromMilliseconds(50));

            UniTask<object> run = loader.LoadAsync(s_source, null, CancellationToken.None);

            Assert.AreEqual(UniTaskStatus.Pending, run.Status);
            Assert.AreSame(config, await run);
        });

        [UnityTest]
        public IEnumerator LoadAsync_DelayedAndCancelled_Cancels() => UniTask.ToCoroutine(async () =>
        {
            FakeConfigLoader loader = FakeConfigLoader.Delayed(new TestConfigA(1), TimeSpan.FromMinutes(1));
            using (var cts = new CancellationTokenSource())
            {
                UniTask<object> run = loader.LoadAsync(s_source, null, cts.Token);
                cts.Cancel();

                await ConfigsTestAsync.CatchAsync<OperationCanceledException>(async () => await run);
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_Returning_GivesTheResultEvenOfAnotherType() => UniTask.ToCoroutine(async () =>
        {
            var other = new TestConfigB("b");
            FakeConfigLoader loader = FakeConfigLoader.Returning(typeof(TestConfigA), other);

            object result = await loader.LoadAsync(s_source, null, CancellationToken.None);

            Assert.AreEqual(typeof(TestConfigA), loader.ConfigType);
            Assert.AreSame(other, result);
        });

        [Test]
        public void Factories_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.Succeeding(null));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.FailingTimes(1, null));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.AlwaysFailing(null));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.Delayed(null, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.Gated(typeof(TestConfigA), null));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.UntilCancelled(null));
            Assert.Throws<ArgumentNullException>(() => FakeConfigLoader.Returning(null, null));
        }
    }
}
