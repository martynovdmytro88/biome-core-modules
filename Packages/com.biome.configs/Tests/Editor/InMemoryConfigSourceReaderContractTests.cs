using System;
using System.Threading;
using Biome.Configs.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Biome.Configs.Editor.Tests
{
    /// <summary>
    /// Runs the reader contract on a test-local double: the package ships no fake and no implementation of the seam, so
    /// this proves the suite itself. A real reader is proven only when a host binds it.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryConfigSourceReaderContractTests : ConfigSourceReaderContractTests<TestConfigA>
    {
        private static readonly TestConfigA s_asset = new TestConfigA(1);

        protected override ConfigSource ExistingSource => new ConfigSource(ConfigSourceKind.Resources, "existing");

        protected override ConfigSource MissingSource => new ConfigSource(ConfigSourceKind.Resources, "missing");

        protected override IConfigSourceReader CreateReader() => new InMemoryReader(ExistingSource);

        protected override void AssertIsTheExistingAsset(TestConfigA asset) => Assert.AreSame(s_asset, asset);

        private sealed class InMemoryReader : IConfigSourceReader
        {
            private readonly ConfigSource _existing;

            public InMemoryReader(ConfigSource existing)
            {
                _existing = existing;
            }

            public UniTask<T> ReadAsync<T>(ConfigSource source, CancellationToken ct) where T : class
            {
                if (ct.IsCancellationRequested)
                {
                    return UniTask.FromCanceled<T>(ct);
                }

                return source.Equals(_existing)
                    ? UniTask.FromResult((T)(object)s_asset)
                    : UniTask.FromException<T>(new InvalidOperationException($"Nothing at '{source.Path}'."));
            }
        }
    }
}
