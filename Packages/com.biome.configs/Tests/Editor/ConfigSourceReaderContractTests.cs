using System;
using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>
    /// The source-reader contract written once over <see cref="IConfigSourceReader"/>: an asset that exists is returned
    /// typed, a missing one throws, a cancelled token cancels. <typeparamref name="TAsset"/> is the asset type the
    /// implementation under test can hold; a host binds its reader with its own asset.
    /// </summary>
    public abstract class ConfigSourceReaderContractTests<TAsset> where TAsset : class
    {
        /// <summary>A source that holds the asset checked by <see cref="AssertIsTheExistingAsset"/>.</summary>
        protected abstract ConfigSource ExistingSource { get; }

        /// <summary>A source that holds nothing.</summary>
        protected abstract ConfigSource MissingSource { get; }

        [UnityTest]
        public IEnumerator ReadAsync_ExistingSource_ReturnsTheTypedAsset() => UniTask.ToCoroutine(async () =>
        {
            TAsset asset = await CreateReader().ReadAsync<TAsset>(ExistingSource, CancellationToken.None);

            Assert.IsNotNull(asset);
            AssertIsTheExistingAsset(asset);
        });

        [UnityTest]
        public IEnumerator ReadAsync_MissingSource_Throws() => UniTask.ToCoroutine(async () =>
        {
            await ConfigsTestAsync.CatchAsync<InvalidOperationException>(
                async () => await CreateReader().ReadAsync<TAsset>(MissingSource, CancellationToken.None));
        });

        [UnityTest]
        public IEnumerator ReadAsync_CancelledToken_Cancels() => UniTask.ToCoroutine(async () =>
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await ConfigsTestAsync.CatchAsync<OperationCanceledException>(
                    async () => await CreateReader().ReadAsync<TAsset>(ExistingSource, cts.Token));
            }
        });

        /// <summary>The reader of the implementation under test.</summary>
        protected abstract IConfigSourceReader CreateReader();

        /// <summary>Asserts that <paramref name="asset"/> is the one behind <see cref="ExistingSource"/>.</summary>
        protected abstract void AssertIsTheExistingAsset(TAsset asset);
    }
}
