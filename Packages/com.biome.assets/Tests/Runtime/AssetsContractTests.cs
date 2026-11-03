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
    /// The contract of <see cref="IAssetsService"/> / <see cref="IAssetsContext"/>, written once. A subclass supplies
    /// the implementation (<see cref="CreateService"/>) and two <see cref="TextAsset"/> keys under one label. Only
    /// behaviour visible through the contract is asserted here.
    /// <para>
    /// The package carries no implementation of the service, so nothing runs this suite here: a host derives from it in
    /// its own test assembly. The assembly is not Editor-only, so a production backend can run it in PlayMode.
    /// </para>
    /// </summary>
    public abstract class AssetsContractTests
    {
        private const int MaxWaitFrames = 600;

        protected IAssetsService Service { get; private set; }

        protected abstract AssetKey KeyA { get; }

        protected abstract AssetKey KeyB { get; }

        /// <summary>A label carried by exactly <see cref="KeyA"/> and <see cref="KeyB"/>.</summary>
        protected abstract AssetLabel Label { get; }

        /// <summary>A scene key that loads and unloads.</summary>
        protected abstract AssetKey SceneKey { get; }

        /// <summary>How many instances of the scene under <see cref="SceneKey"/> are open right now, whoever loaded them.</summary>
        protected abstract int LiveSceneCount { get; }

        [SetUp]
        public void SetUpService() => Service = CreateService();

        [TearDown]
        public void TearDownService() => (Service as IDisposable)?.Dispose();

        [UnityTest]
        public IEnumerator Load_ValidKey_ReturnsTheAsset() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            TextAsset asset = await context.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            Assert.That(asset, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator Load_HeldKey_CompletesSynchronouslyWithTheSameInstance() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            TextAsset first = await context.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            UniTask<TextAsset> again = context.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            Assert.That(again.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(await again, Is.SameAs(first));
        });

        [UnityTest]
        public IEnumerator Load_SameKeyInTwoContexts_IsOneInstance() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = Service.CreateContext();
            IAssetsContext two = Service.CreateContext();
            TextAsset a = await one.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            TextAsset b = await two.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            Assert.That(b, Is.SameAs(a));
        });

        [UnityTest]
        public IEnumerator Release_IsIdempotent_AndTheKeyCanBeLoadedAgain() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            await context.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            context.Release(KeyA);
            Assert.DoesNotThrow(() => context.Release(KeyA));
            Assert.DoesNotThrow(() => context.Release(KeyB));
            Assert.That(await context.LoadAsync<TextAsset>(KeyA, CancellationToken.None), Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator Release_InOneContext_DoesNotUnloadForTheOther() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = Service.CreateContext();
            IAssetsContext two = Service.CreateContext();
            await one.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            TextAsset held = await two.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            one.Release(KeyA);
            UniTask<TextAsset> still = two.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            Assert.That(still.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(await still, Is.SameAs(held));
        });

        [Test]
        public void Unload_ClosesTheContext_AndIsIdempotent()
        {
            IAssetsContext context = Service.CreateContext();
            Assert.That(context.IsClosed, Is.False);
            context.Unload();
            Assert.That(context.IsClosed, Is.True);
            Assert.DoesNotThrow(context.Unload);
            Assert.DoesNotThrow(context.Dispose);
            Assert.DoesNotThrow(() => context.Release(KeyA));
        }

        [Test]
        public void LoadOrCreateChild_ClosedContext_Throws()
        {
            IAssetsContext context = Service.CreateContext();
            context.Dispose();
            Assert.Throws<ObjectDisposedException>(() => context.LoadAsync<TextAsset>(KeyA, CancellationToken.None));
            Assert.Throws<ObjectDisposedException>(() => context.CreateChild());
        }

        [Test]
        public void Unload_Parent_ClosesChildrenFirst()
        {
            IAssetsContext parent = Service.CreateContext();
            IAssetsContext child = parent.CreateChild();
            IAssetsContext grandchild = child.CreateChild();
            parent.Unload();
            Assert.That(child.IsClosed, Is.True);
            Assert.That(grandchild.IsClosed, Is.True);
        }

        [Test]
        public void Contexts_HaveDistinctIds_AndTheGlobalOneIsOpen()
        {
            IAssetsContext one = Service.CreateContext();
            IAssetsContext two = Service.CreateContext();
            Assert.That(one.Id, Is.Not.EqualTo(two.Id));
            Assert.That(one.CreateChild().Id, Is.Not.EqualTo(one.Id));
            Assert.That(Service.GlobalContext.IsClosed, Is.False);
        }

        [UnityTest]
        public IEnumerator Unload_Child_DoesNotTouchTheParentsAssets() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext parent = Service.CreateContext();
            IAssetsContext child = parent.CreateChild();
            TextAsset asset = await parent.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            await child.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            child.Unload();
            UniTask<TextAsset> again = parent.LoadAsync<TextAsset>(KeyA, CancellationToken.None);
            Assert.That(again.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(await again, Is.SameAs(asset));
        });

        [UnityTest]
        public IEnumerator Load_WithACancelledToken_Throws() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, TextAsset>(
                () => context.LoadAsync<TextAsset>(KeyB, cts.Token)));
        });

        [UnityTest]
        public IEnumerator Load_WrongType_ThrowsInvalidCast() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            Assert.IsTrue(await ThrowsAsync<InvalidCastException, Texture2D>(
                () => context.LoadAsync<Texture2D>(KeyA, CancellationToken.None)));
        });

        [UnityTest]
        public IEnumerator BatchLoad_ReturnsAssetsInKeyOrder_AndReportsProgressToOne() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            var reports = new List<float>();
            var progress = new ListProgress(reports);
            IReadOnlyList<TextAsset> result = await context.LoadAsync<TextAsset>(new[] { KeyB, KeyA }, progress, CancellationToken.None);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.SameAs(await context.LoadAsync<TextAsset>(KeyB, CancellationToken.None)));
            Assert.That(result[1], Is.SameAs(await context.LoadAsync<TextAsset>(KeyA, CancellationToken.None)));
            Assert.That(reports, Is.Not.Empty);
            Assert.That(reports[reports.Count - 1], Is.EqualTo(1f).Within(1e-4f));
        });

        [UnityTest]
        public IEnumerator BatchLoad_OfNoKeys_ReportsCompleteAndReturnsEmpty() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            var reports = new List<float>();
            IReadOnlyList<TextAsset> result = await context.LoadAsync<TextAsset>(Array.Empty<AssetKey>(), new ListProgress(reports), CancellationToken.None);
            Assert.That(result, Is.Empty);
            Assert.That(reports, Is.EqualTo(new[] { 1f }));
        });

        [UnityTest]
        public IEnumerator PreloadByLabel_ReturnsTheKeys_AndHoldsThem() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            var reports = new List<float>();
            IReadOnlyList<AssetKey> keys = await context.PreloadByLabelAsync(Label, new ListProgress(reports), CancellationToken.None);
            Assert.That(keys, Is.EquivalentTo(new[] { KeyA, KeyB }));
            Assert.That(reports[reports.Count - 1], Is.EqualTo(1f).Within(1e-4f));
            Assert.That(context.LoadAsync<TextAsset>(KeyA, CancellationToken.None).Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(context.LoadAsync<TextAsset>(KeyB, CancellationToken.None).Status, Is.EqualTo(UniTaskStatus.Succeeded));
        });

        [UnityTest]
        public IEnumerator PreloadByLabel_OfAnUnusedLabel_ReturnsNoKeys() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            IReadOnlyList<AssetKey> keys = await context.PreloadByLabelAsync(new AssetLabel("biome-assets-tests-none"), null, CancellationToken.None);
            Assert.That(keys, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LoadScene_ValidKey_OpensTheScene_UnderAHandleOfThatKey() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            SceneHandle handle = await context.LoadSceneAsync(SceneKey, CancellationToken.None);
            Assert.That(handle.Key, Is.EqualTo(SceneKey));
            Assert.That(LiveSceneCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator LoadScene_Twice_OpensTwoInstancesWithDistinctHandles() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            SceneHandle first = await context.LoadSceneAsync(SceneKey, CancellationToken.None);
            SceneHandle second = await context.LoadSceneAsync(SceneKey, CancellationToken.None);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(LiveSceneCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator UnloadScene_LoadedScene_ClosesIt_AndAgainIsANoOp() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            SceneHandle handle = await context.LoadSceneAsync(SceneKey, CancellationToken.None);
            await context.UnloadSceneAsync(handle, CancellationToken.None);
            await WaitForLiveScenesAsync(0);
            Assert.That(LiveSceneCount, Is.EqualTo(0));
            await context.UnloadSceneAsync(handle, CancellationToken.None);
            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator UnloadScene_HandleOfAnotherContext_LeavesTheSceneOpen() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext owner = Service.CreateContext();
            IAssetsContext other = Service.CreateContext();
            SceneHandle handle = await owner.LoadSceneAsync(SceneKey, CancellationToken.None);
            await other.UnloadSceneAsync(handle, CancellationToken.None);
            await other.UnloadSceneAsync(default, CancellationToken.None);
            Assert.That(LiveSceneCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Dispose_Context_UnloadsEveryScene_ItAndItsChildrenLoaded() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            IAssetsContext child = context.CreateChild();
            await context.LoadSceneAsync(SceneKey, CancellationToken.None);
            await child.LoadSceneAsync(SceneKey, CancellationToken.None);
            Assert.That(LiveSceneCount, Is.EqualTo(2));
            context.Dispose();
            await WaitForLiveScenesAsync(0);
            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator Dispose_Context_DoesNotUnloadTheScenesOfAnotherContext() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = Service.CreateContext();
            IAssetsContext two = Service.CreateContext();
            await one.LoadSceneAsync(SceneKey, CancellationToken.None);
            await two.LoadSceneAsync(SceneKey, CancellationToken.None);
            one.Dispose();
            await WaitForLiveScenesAsync(1);
            Assert.That(LiveSceneCount, Is.EqualTo(1));
        });

        [Test]
        public void LoadScene_ClosedContext_Throws()
        {
            IAssetsContext context = Service.CreateContext();
            context.Dispose();
            Assert.Throws<ObjectDisposedException>(() => context.LoadSceneAsync(SceneKey, CancellationToken.None));
        }

        [UnityTest]
        public IEnumerator LoadScene_WithACancelledToken_Throws_AndOpensNothing() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, SceneHandle>(
                () => context.LoadSceneAsync(SceneKey, cts.Token)));
            await WaitForLiveScenesAsync(0);
            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator LoadScene_ContextClosedWhileLoading_Throws_AndLeavesNoScene() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = Service.CreateContext();
            HoldSceneLoads();
            UniTask<SceneHandle> pending = context.LoadSceneAsync(SceneKey, CancellationToken.None);
            context.Dispose();
            ReleaseSceneLoads();
            Assert.IsTrue(await ThrowsAsync<OperationCanceledException, SceneHandle>(() => pending));
            await WaitForLiveScenesAsync(0);
            Assert.That(LiveSceneCount, Is.EqualTo(0));
        });

        protected abstract IAssetsService CreateService();

        /// <summary>Makes scene loads started from now on stay in flight until <see cref="ReleaseSceneLoads"/>; a backend that always loads asynchronously leaves this empty.</summary>
        protected virtual void HoldSceneLoads()
        {
        }

        /// <summary>Lets the scene loads held by <see cref="HoldSceneLoads"/> finish.</summary>
        protected virtual void ReleaseSceneLoads()
        {
        }

        /// <summary>Waits (up to a few seconds of frames) until <see cref="LiveSceneCount"/> reaches <paramref name="expected"/>.</summary>
        private async UniTask WaitForLiveScenesAsync(int expected)
        {
            for (var frame = 0; frame < MaxWaitFrames && LiveSceneCount != expected; frame++)
            {
                await UniTask.Yield();
            }
        }

        private static async UniTask<bool> ThrowsAsync<TException, TResult>(Func<UniTask<TResult>> action) where TException : Exception
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

        /// <summary>A synchronous <see cref="IProgress{T}"/> (the BCL <c>Progress</c> posts to the sync context).</summary>
        private sealed class ListProgress : IProgress<float>
        {
            private readonly List<float> _reports;

            public ListProgress(List<float> reports)
            {
                _reports = reports;
            }

            public void Report(float value) => _reports.Add(value);
        }
    }
}
