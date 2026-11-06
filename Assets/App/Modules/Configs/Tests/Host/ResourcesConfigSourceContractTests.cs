using System;
using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using Biome.Configs.Editor.Tests;
using Biome.Configs.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.Configs.Tests.Host
{
    /// <summary>
    /// Binds the package's source-reader contract to the production <see cref="ResourcesConfigSource"/>, plus its own
    /// behaviour. The Resources asset is made for the fixture and removed after it; no committed asset is needed.
    /// </summary>
    [TestFixture]
    public sealed class ResourcesConfigSourceContractTests : ConfigSourceReaderContractTests<EnvironmentConfig>
    {
        private const string FixtureId = "resources-fixture";
        private const string AssetName = "ConfigsFixtureEnvironment";

        private IDisposable _fixture;

        protected override ConfigSource ExistingSource => new ConfigSource(ConfigSourceKind.Resources, AssetName);

        protected override ConfigSource MissingSource => new ConfigSource(ConfigSourceKind.Resources, "NoSuchConfigsFixture");

        [OneTimeSetUp]
        public void CreateResourcesAsset()
        {
            _fixture = ConfigsTestEnvironment.CreateResourcesAsset(AssetName, FixtureId);
        }

        [OneTimeTearDown]
        public void RemoveResourcesAsset()
        {
            _fixture?.Dispose();
        }

        [UnityTest]
        public IEnumerator ReadAsync_AssetOfAnotherType_Throws() => UniTask.ToCoroutine(async () =>
        {
            await HostTestAsync.CatchAsync<InvalidOperationException>(
                async () => await CreateReader().ReadAsync<TestConfigA>(ExistingSource, CancellationToken.None));
        });

        [UnityTest]
        public IEnumerator ReadAsync_SourceKindNotResources_Throws() => UniTask.ToCoroutine(async () =>
        {
            var foreign = new ConfigSource((ConfigSourceKind)99, AssetName);

            await HostTestAsync.CatchAsync<ArgumentException>(
                async () => await CreateReader().ReadAsync<EnvironmentConfig>(foreign, CancellationToken.None));
        });

        protected override IConfigSourceReader CreateReader()
        {
            return new ResourcesConfigSource();
        }

        protected override void AssertIsTheExistingAsset(EnvironmentConfig asset)
        {
            Assert.AreEqual(FixtureId, asset.Id);
        }
    }
}
