using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Biome.Assets.Tests;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biome.Assets.Editor.Tests
{
    /// <summary>Binds the seam contract to the package's fake, plus the fake's own counters and gates.</summary>
    [TestFixture]
    public sealed class FakeAssetBackendContractTests : AssetBackendContractTests
    {
        private FakeAssetBackend _fake;

        protected override AssetKey KeyA { get; } = new AssetKey("fake/a");

        protected override AssetKey KeyB { get; } = new AssetKey("fake/b");

        protected override AssetLabel Label { get; } = new AssetLabel("fake-label");

        protected override AssetKey SceneKey { get; } = new AssetKey("fake/scene");

        protected override int LiveSceneCount => _fake.LiveSceneCount(SceneKey);

        [UnityTest]
        public IEnumerator Fake_LoadAndRelease_UpdateTheCounters() => UniTask.ToCoroutine(async () =>
        {
            IAssetHandle handle = await Backend.LoadAsync(KeyA, typeof(TextAsset), CancellationToken.None);
            Assert.That(_fake.StartedCount(KeyA), Is.EqualTo(1));
            Assert.That(_fake.LoadedCount(KeyA), Is.EqualTo(1));
            Assert.That(_fake.LiveCount(KeyA), Is.EqualTo(1));

            handle.Release();
            handle.Release();

            Assert.That(_fake.ReleasedCount(KeyA), Is.EqualTo(1), "a second release is not counted");
            Assert.That(_fake.LiveCount(KeyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Fake_Gate_HoldsTheLoadUntilOpen() => UniTask.ToCoroutine(async () =>
        {
            _fake.Gate(KeyA);

            UniTask<IAssetHandle> pending = Backend.LoadAsync(KeyA, typeof(TextAsset), CancellationToken.None);
            Assert.That(_fake.StartedCount(KeyA), Is.EqualTo(1));
            Assert.That(_fake.LoadedCount(KeyA), Is.EqualTo(0));

            _fake.Open(KeyA);
            await pending;

            Assert.That(_fake.LoadedCount(KeyA), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Fake_GatedLoad_CancelledToken_CountsAsCancelled() => UniTask.ToCoroutine(async () =>
        {
            _fake.Gate(KeyA);
            using var cts = new CancellationTokenSource();
            UniTask<IAssetHandle> pending = Backend.LoadAsync(KeyA, typeof(TextAsset), cts.Token);

            cts.Cancel();

            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, IAssetHandle>(() => pending));
            Assert.That(_fake.CancelledCount(KeyA), Is.EqualTo(1));
            Assert.That(_fake.LiveCount(KeyA), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Fake_Delay_CompletesAfterTheDelay() => UniTask.ToCoroutine(async () =>
        {
            _fake.Delay = TimeSpan.FromMilliseconds(30);

            UniTask<IAssetHandle> pending = Backend.LoadAsync(KeyA, typeof(TextAsset), CancellationToken.None);
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Pending));

            IAssetHandle handle = await pending;

            Assert.That(handle.Asset, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator Fake_UnregisteredKey_ThrowsKeyNotFound() => UniTask.ToCoroutine(async () =>
        {
            var missing = new AssetKey("fake/missing");

            Assert.IsTrue(await ThrowsAsync<KeyNotFoundException, IAssetHandle>(
                () => Backend.LoadAsync(missing, typeof(TextAsset), CancellationToken.None)));
            Assert.IsTrue(await ThrowsAsync<KeyNotFoundException, ISceneInstance>(
                () => Backend.LoadSceneAsync(missing, CancellationToken.None)));
        });

        [UnityTest]
        public IEnumerator Fake_Scene_CountsLoadsAndOneUnloadPerInstance() => UniTask.ToCoroutine(async () =>
        {
            ISceneInstance scene = await Backend.LoadSceneAsync(SceneKey, CancellationToken.None);
            Assert.That(_fake.SceneLoadedCount(SceneKey), Is.EqualTo(1));

            await scene.UnloadAsync(CancellationToken.None);
            await scene.UnloadAsync(CancellationToken.None);

            Assert.That(_fake.SceneUnloadedCount(SceneKey), Is.EqualTo(1));
        });

        protected override IAssetBackend CreateBackend()
        {
            _fake = new FakeAssetBackend()
                .Add(KeyA, new TextAsset("a"), Label)
                .Add(KeyB, new TextAsset("b"), Label)
                .AddScene(SceneKey);
            return _fake;
        }
    }
}
