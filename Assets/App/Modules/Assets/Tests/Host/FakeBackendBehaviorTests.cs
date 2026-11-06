using System;
using System.Collections;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biome.Assets.Tests.Host
{
    /// <summary>
    /// What only the fake can observe — its counters: reference counting across contexts, the shared in-flight load
    /// and its cancellation rule (Q4 / Q11). Implementation-side tests beside the contract suite, never editing it.
    /// </summary>
    [TestFixture]
    public sealed class FakeBackendBehaviorTests
    {
        private static readonly AssetKey s_keyA = new AssetKey("fake/a");
        private static readonly AssetKey s_keyB = new AssetKey("fake/b");

        private FakeAssetBackend _backend;
        private IAssetsService _service;

        [SetUp]
        public void SetUp()
        {
            _backend = new FakeAssetBackend()
                .Add(s_keyA, new TextAsset("a"))
                .Add(s_keyB, new TextAsset("b"));
            _service = FakeAssets.CreateService(_backend);
        }

        [TearDown]
        public void TearDown() => (_service as IDisposable)?.Dispose();

        [UnityTest]
        public IEnumerator SameKeyTwiceInOneContext_HoldsOneReference_OneReleaseFreesIt() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = _service.CreateContext();
            await context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            await context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            Assert.That(_backend.LoadedCount(s_keyA), Is.EqualTo(1));
            context.Release(s_keyA);
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator SameKeyInTwoContexts_LoadsOnce_LeavesMemoryWithTheLastHolder() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            await one.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            await two.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            Assert.That(_backend.LoadedCount(s_keyA), Is.EqualTo(1));
            one.Release(s_keyA);
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(1));
            two.Unload();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Unload_ReleasesEveryHeldKey_ChildrenIncluded() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext parent = _service.CreateContext();
            IAssetsContext child = parent.CreateChild();
            await parent.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            await child.LoadAsync<TextAsset>(s_keyB, CancellationToken.None);
            parent.Unload();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
            Assert.That(_backend.LiveCount(s_keyB), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Dispose_Service_UnloadsGlobalAndTopLevelContexts() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = _service.CreateContext();
            await context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            await _service.GlobalContext.LoadAsync<TextAsset>(s_keyB, CancellationToken.None);
            ((IDisposable)_service).Dispose();
            Assert.That(context.IsClosed, Is.True);
            Assert.That(_service.GlobalContext.IsClosed, Is.True);
            Assert.That(_backend.LiveCount(s_keyA) + _backend.LiveCount(s_keyB), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator InFlightLoad_IsShared_ByTwoContexts() => UniTask.ToCoroutine(async () =>
        {
            _backend.Gate(s_keyA);
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            UniTask<TextAsset> first = one.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            UniTask<TextAsset> second = two.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            Assert.That(_backend.StartedCount(s_keyA), Is.EqualTo(1));
            _backend.Open(s_keyA);
            Assert.That(await second, Is.SameAs(await first));
            Assert.That(_backend.LoadedCount(s_keyA), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Load_OneWaiterCancels_DoesNotCancelTheSharedLoad() => UniTask.ToCoroutine(async () =>
        {
            _backend.Gate(s_keyA);
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            using var cts = new CancellationTokenSource();
            UniTask<TextAsset> cancelled = one.LoadAsync<TextAsset>(s_keyA, cts.Token);
            UniTask<TextAsset> staying = two.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            cts.Cancel();
            Assert.That(cancelled.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_backend.CancelledCount(s_keyA), Is.EqualTo(0));
            _backend.Open(s_keyA);
            Assert.That(await staying, Is.Not.Null);
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LastWaiterCancelling_CancelsTheLoad_AndNothingStaysLoaded() => UniTask.ToCoroutine(async () =>
        {
            _backend.Gate(s_keyA);
            IAssetsContext context = _service.CreateContext();
            using var cts = new CancellationTokenSource();
            UniTask<TextAsset> pending = context.LoadAsync<TextAsset>(s_keyA, cts.Token);
            cts.Cancel();
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_backend.CancelledCount(s_keyA), Is.EqualTo(1));
            await UniTask.Yield();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Unload_CancelsPendingWaiters_AndTheLoadWithThem() => UniTask.ToCoroutine(async () =>
        {
            _backend.Gate(s_keyA);
            IAssetsContext context = _service.CreateContext();
            UniTask<TextAsset> pending = context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            context.Unload();
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_backend.CancelledCount(s_keyA), Is.EqualTo(1));
            await UniTask.Yield();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Release_OfAPendingKey_CancelsThatWaiter() => UniTask.ToCoroutine(async () =>
        {
            _backend.Gate(s_keyA);
            IAssetsContext context = _service.CreateContext();
            UniTask<TextAsset> pending = context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            context.Release(s_keyA);
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
            await UniTask.Yield();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Load_WaiterCancelledAfterLoadFinished_DoesNotDropTheAsset() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = _service.CreateContext();
            using var cts = new CancellationTokenSource();
            await context.LoadAsync<TextAsset>(s_keyA, cts.Token);
            cts.Cancel();
            Assert.That(_backend.LiveCount(s_keyA), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Load_WithDelay_CompletesAfterTheDelay() => UniTask.ToCoroutine(async () =>
        {
            _backend.Delay = TimeSpan.FromMilliseconds(30);
            IAssetsContext context = _service.CreateContext();
            UniTask<TextAsset> pending = context.LoadAsync<TextAsset>(s_keyA, CancellationToken.None);
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(await pending, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator FailedLoad_PropagatesToTheWaiter_AndLeavesNothingHeld() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = _service.CreateContext();
            var caught = false;
            try
            {
                await context.LoadAsync<TextAsset>(new AssetKey("fake/missing"), CancellationToken.None);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                caught = true;
            }

            Assert.That(caught, Is.True);
            Assert.That(context.IsClosed, Is.False);
        });
    }
}
