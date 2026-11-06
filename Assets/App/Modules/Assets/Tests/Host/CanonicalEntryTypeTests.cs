using System;
using System.Collections;
using System.Threading;
using Biome.Assets.Abstract;
using Biome.Assets.Fakes;
using Biome.Assets.Tests.Shared;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biome.Assets.Tests.Host
{
    /// <summary>
    /// A key loads once, by the asset's own type; each caller's T is a view taken by Cast&lt;T&gt; (MA-DEV-DEV-009).
    /// The result does not depend on which context asked first or on a label preload, and a failed view releases
    /// only the hold it took. Implementation-side tests beside the contract suite.
    /// </summary>
    [TestFixture]
    public sealed class CanonicalEntryTypeTests
    {
        private static readonly AssetKey s_prefab = new AssetKey("fake/prefab");
        private static readonly AssetKey s_bare = new AssetKey("fake/bare");
        private static readonly AssetLabel s_label = new AssetLabel("fake/canonical");

        private FakeAssetBackend _backend;
        private IAssetsService _service;
        private GameObject _prefab;
        private GameObject _bare;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("prefab", typeof(CanonicalProbe));
            _bare = new GameObject("bare");
            _backend = new FakeAssetBackend()
                .Add(s_prefab, _prefab, s_label)
                .Add(s_bare, _bare);
            _service = FakeAssets.CreateService(_backend);
        }

        [TearDown]
        public void TearDown()
        {
            (_service as IDisposable)?.Dispose();
            UnityEngine.Object.DestroyImmediate(_prefab);
            UnityEngine.Object.DestroyImmediate(_bare);
        }

        [UnityTest]
        public IEnumerator GameObjectFirst_ComponentView_ReturnsTheComponentOnOneLoad() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            GameObject asGo = await one.LoadAsync<GameObject>(s_prefab, CancellationToken.None);
            CanonicalProbe asComponent = await two.LoadAsync<CanonicalProbe>(s_prefab, CancellationToken.None);
            Assert.That(asGo, Is.SameAs(_prefab));
            Assert.That(asComponent, Is.SameAs(_prefab.GetComponent<CanonicalProbe>()));
            Assert.That(_backend.LoadedCount(s_prefab), Is.EqualTo(1));
            one.Release(s_prefab);
            Assert.That(_backend.LiveCount(s_prefab), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator ComponentFirst_ComponentView_ReturnsTheComponentOnOneLoad() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            CanonicalProbe asComponent = await one.LoadAsync<CanonicalProbe>(s_prefab, CancellationToken.None);
            GameObject asGo = await two.LoadAsync<GameObject>(s_prefab, CancellationToken.None);
            Assert.That(asComponent, Is.SameAs(_prefab.GetComponent<CanonicalProbe>()));
            Assert.That(asGo, Is.SameAs(_prefab));
            Assert.That(_backend.LoadedCount(s_prefab), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator InterfaceView_OnAComponent_ReturnsTheComponent() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext one = _service.CreateContext();
            IAssetsContext two = _service.CreateContext();
            await one.LoadAsync<GameObject>(s_prefab, CancellationToken.None);
            ICanonicalProbe asInterface = await two.LoadAsync<ICanonicalProbe>(s_prefab, CancellationToken.None);
            Assert.That(asInterface, Is.SameAs(_prefab.GetComponent<CanonicalProbe>()));
            Assert.That(_backend.LoadedCount(s_prefab), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator AfterPreloadByLabel_ComponentView_ReturnsTheComponent() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext context = _service.CreateContext();
            await context.PreloadByLabelAsync(s_label, null, CancellationToken.None);
            CanonicalProbe asComponent = await context.LoadAsync<CanonicalProbe>(s_prefab, CancellationToken.None);
            Assert.That(asComponent, Is.SameAs(_prefab.GetComponent<CanonicalProbe>()));
            Assert.That(_backend.LoadedCount(s_prefab), Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator MissingComponent_ThrowsInvalidCast_AndKeepsTheHoldOfTheOtherContext() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext holder = _service.CreateContext();
            IAssetsContext failing = _service.CreateContext();
            await holder.LoadAsync<GameObject>(s_bare, CancellationToken.None);

            await AssertInvalidCastAsync(failing, s_bare);

            // The failing context took no hold of its own: the holder's release frees the asset.
            holder.Release(s_bare);
            Assert.That(_backend.LiveCount(s_bare), Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator MissingComponent_WithNoPriorHolder_ReleasesItsOwnHold() => UniTask.ToCoroutine(async () =>
        {
            IAssetsContext failing = _service.CreateContext();
            await AssertInvalidCastAsync(failing, s_bare);

            Assert.That(_backend.LoadedCount(s_bare), Is.EqualTo(1));
            Assert.That(_backend.LiveCount(s_bare), Is.EqualTo(0));
        });

        private static async UniTask AssertInvalidCastAsync(IAssetsContext context, AssetKey key)
        {
            var threw = false;
            try
            {
                await context.LoadAsync<CanonicalProbe>(key, CancellationToken.None);
            }
            catch (InvalidCastException)
            {
                threw = true;
            }

            Assert.That(threw, Is.True, "Expected InvalidCastException for a missing component.");
        }
    }
}
