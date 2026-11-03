using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Assets.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biome.Assets.Tests
{
    /// <summary>
    /// The contract of the <see cref="IAssetBackend"/> seam. A subclass supplies the backend and two
    /// <see cref="TextAsset"/> keys under one label plus a scene key. The package binds it to its fake; a host binds its
    /// real backend the same way.
    /// </summary>
    public abstract class AssetBackendContractTests
    {
        private const int MaxWaitFrames = 600;

        protected IAssetBackend Backend { get; private set; }

        protected abstract AssetKey KeyA { get; }

        protected abstract AssetKey KeyB { get; }

        /// <summary>A label carried by exactly <see cref="KeyA"/> and <see cref="KeyB"/>.</summary>
        protected abstract AssetLabel Label { get; }

        /// <summary>A scene key that loads and unloads.</summary>
        protected abstract AssetKey SceneKey { get; }

        /// <summary>How many instances of the scene under <see cref="SceneKey"/> are open right now.</summary>
        protected abstract int LiveSceneCount { get; }

        [SetUp]
        public void SetUpBackend() => Backend = CreateBackend();

        [UnityTest]
        public IEnumerator LoadAsync_ValidKey_ReturnsAHandleOfTheAsset() => UniTask.ToCoroutine(async () =>
        {
            IAssetHandle handle = await Backend.LoadAsync(KeyA, typeof(TextAsset), CancellationToken.None);

            Assert.That(handle.Asset, Is.InstanceOf<TextAsset>());
            handle.Release();
        });

        [UnityTest]
        public IEnumerator LoadAsync_CancelledToken_Throws() => UniTask.ToCoroutine(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, IAssetHandle>(
                () => Backend.LoadAsync(KeyA, typeof(TextAsset), cts.Token)));
        });

        [UnityTest]
        public IEnumerator ResolveLabelAsync_UsedLabel_ReturnsItsKeys() => UniTask.ToCoroutine(async () =>
        {
            IReadOnlyList<AssetKey> keys = await Backend.ResolveLabelAsync(Label, CancellationToken.None);

            Assert.That(keys, Is.EquivalentTo(new[] { KeyA, KeyB }));
        });

        [UnityTest]
        public IEnumerator ResolveLabelAsync_UnusedLabel_ReturnsNoKeys() => UniTask.ToCoroutine(async () =>
        {
            IReadOnlyList<AssetKey> keys = await Backend.ResolveLabelAsync(new AssetLabel("biome-assets-tests-none"), CancellationToken.None);

            Assert.That(keys, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_ValidKey_OpensTheScene_AndUnloadClosesIt() => UniTask.ToCoroutine(async () =>
        {
            ISceneInstance scene = await Backend.LoadSceneAsync(SceneKey, CancellationToken.None);
            Assert.That(LiveSceneCount, Is.EqualTo(1));

            await scene.UnloadAsync(CancellationToken.None);
            await WaitForLiveScenesAsync(0);

            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_CancelledToken_Throws_AndLeavesNoScene() => UniTask.ToCoroutine(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, ISceneInstance>(
                () => Backend.LoadSceneAsync(SceneKey, cts.Token)));
            await WaitForLiveScenesAsync(0);

            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        /// <summary>A fresh backend under test; called once per test.</summary>
        protected abstract IAssetBackend CreateBackend();

        /// <summary>True when the awaited call throws <typeparamref name="TException"/>, synchronously or not.</summary>
        protected static async UniTask<bool> ThrowsAsync<TException, TResult>(Func<UniTask<TResult>> action) where TException : Exception
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

        private async UniTask WaitForLiveScenesAsync(int expected)
        {
            for (var frame = 0; frame < MaxWaitFrames && LiveSceneCount != expected; frame++)
            {
                await UniTask.Yield();
            }
        }
    }
}
