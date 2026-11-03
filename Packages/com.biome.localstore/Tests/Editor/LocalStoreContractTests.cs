using System;
using System.Collections.Generic;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using NUnit.Framework;

namespace Biome.LocalStore.Editor.Tests
{
    /// <summary>
    /// The contract of <see cref="ILocalStoreService"/>, written once over the backend seam: missing key · corrupt
    /// payload · version mismatch · limit violation (throws, previous state kept) · manifest integrity after external
    /// deletion · Clear() leaves foreign keys untouched · unregistered type fails loudly.
    /// <para>
    /// The package carries no implementation of the service, so nothing runs this suite here: a host derives from it in
    /// its own test assembly, returning its store from <see cref="CreateStore"/>.
    /// </para>
    /// </summary>
    public abstract class LocalStoreContractTests
    {
        private const string AudioKey = "audio";
        private const string TutorialKey = "tutorial";
        private const int AudioVersion = 2;

        // Test-chosen limits (small so a violation is easy to trigger), not the operator's figures.
        private static readonly LocalStoreLimits s_testLimits = new LocalStoreLimits(maxKeyLength: 32, maxValueBytes: 64, maxTotalBytes: 100);

        private TrackingLocalStoreBackend _backend;
        private ILocalStoreService _store;

        /// <summary>A key the store does not own (no prefix), planted to prove the store never touches it.</summary>
        protected virtual string ForeignKey => "biome.localstore.tests.foreign";

        [SetUp]
        public void SetUp()
        {
            _backend = new TrackingLocalStoreBackend(CreateBackend());
            _backend.Set(ForeignKey, "1920");
            _store = NewStore(_backend);
        }

        [TearDown]
        public void TearDown()
        {
            CleanUp(_backend);
        }

        // ---- round trip (the happy path the edges are measured against) ---------------------------------------

        [Test]
        public void Set_ThenTryGet_ReturnsOkAndTheValue()
        {
            _store.Set(new AudioSettings { Volume = 0.5f, Muted = true });

            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.True);
            Assert.That(status, Is.EqualTo(ReadStatus.Ok));
            Assert.That(value.Volume, Is.EqualTo(0.5f));
            Assert.That(value.Muted, Is.True);
            Assert.That(_backend.Read(Prefixed(AudioKey)), Is.EqualTo("{\"v\":2,\"d\":{\"Volume\":0.5,\"Muted\":true}}"),
                "record = {\"v\",\"d\"} envelope, version per record");
        }

        // ---- edge 1: missing key -------------------------------------------------------------------------------

        [Test]
        public void TryGet_MissingKey_ReturnsFalseWithMissing()
        {
            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.False);
            Assert.That(status, Is.EqualTo(ReadStatus.Missing));
            Assert.That(value, Is.Null);
            Assert.That(_store.Has<AudioSettings>(), Is.False);
        }

        // ---- edge 2: corrupt payload ---------------------------------------------------------------------------

        [TestCase("not json at all")]
        [TestCase("{\"v\":2}", TestName = "TryGet_CorruptPayload_EnvelopeWithoutPayloadField")]
        [TestCase("{\"d\":{\"Volume\":1}}", TestName = "TryGet_CorruptPayload_EnvelopeWithoutVersionField")]
        [TestCase("{\"v\":\"2\",\"d\":{}}", TestName = "TryGet_CorruptPayload_VersionNotAnInteger")]
        [TestCase("[1,2,3]", TestName = "TryGet_CorruptPayload_NotAnObject")]
        [TestCase("{\"v\":2,\"d\":{\"Volume\":\"loud\"}}", TestName = "TryGet_CorruptPayload_PayloadDoesNotFitTheType")]
        [TestCase("{\"v\":2,\"d\":{}} trailing", TestName = "TryGet_CorruptPayload_TrailingGarbage")]
        public void TryGet_CorruptPayload_ReturnsFalseWithCorrupt(string planted)
        {
            _backend.Set(Prefixed(AudioKey), planted);

            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.False);
            Assert.That(status, Is.EqualTo(ReadStatus.Corrupt));
            Assert.That(value, Is.Null);
        }

        // ---- edge 3: version mismatch --------------------------------------------------------------------------

        [Test]
        public void TryGet_VersionMismatch_ReturnsFalseWithVersionMismatch()
        {
            _backend.Set(Prefixed(AudioKey), "{\"v\":1,\"d\":{\"Volume\":0.25,\"Muted\":false}}");

            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.False);
            Assert.That(status, Is.EqualTo(ReadStatus.VersionMismatch));
            Assert.That(value, Is.Null, "no migration framework: the module reports, the consumer decides");
        }

        // ---- edge 4: limit violation throws, previous state kept -----------------------------------------------

        [Test]
        public void Set_OversizeValue_ThrowsAndKeepsPreviousState()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            string before = _backend.Read(Prefixed(AudioKey));
            string manifestBefore = _backend.Read(LocalStoreKeys.ManifestKey);
            ILocalStoreService bigStore = CreateStore(_backend, new TestRegistry().Add<string>("big", 1), s_testLimits);

            // A 64-byte limit; 1000 two-byte characters measure 2000 UTF-8 bytes, so bytes decide, not characters.
            LocalStoreLimitException exception = Assert.Throws<LocalStoreLimitException>(() => bigStore.Set(new string('ї', 1000)));

            Assert.That(exception.Actual, Is.GreaterThan(1000), "size is measured in UTF-8 bytes, not characters");
            Assert.That(_backend.Has(Prefixed("big")), Is.False, "nothing written");
            Assert.That(_backend.Read(Prefixed(AudioKey)), Is.EqualTo(before));
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Is.EqualTo(manifestBefore));
            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.True);
            Assert.That(status, Is.EqualTo(ReadStatus.Ok));
            Assert.That(value.Volume, Is.EqualTo(0.5f));
        }

        [Test]
        public void Set_TotalBudgetExceeded_ThrowsAndKeepsPreviousState()
        {
            // The audio record alone fits a 50-byte budget; the tutorial record on top of it does not.
            ILocalStoreService tight = CreateStore(_backend, Registry(), new LocalStoreLimits(32, 64, 50));
            tight.Set(new AudioSettings { Volume = 0.5f });
            string manifestBefore = _backend.Read(LocalStoreKeys.ManifestKey);

            LocalStoreLimitException exception = Assert.Throws<LocalStoreLimitException>(() => tight.Set(new TutorialFlag { Completed = true }));

            Assert.That(exception.Limit, Is.EqualTo(50));
            Assert.That(_backend.Has(Prefixed(TutorialKey)), Is.False);
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Is.EqualTo(manifestBefore));
            Assert.That(tight.Has<TutorialFlag>(), Is.False);
        }

        [Test]
        public void Set_KeyTooLong_Throws()
        {
            ILocalStoreService store = CreateStore(_backend, new TestRegistry().Add<TutorialFlag>(new string('k', 40), 1), s_testLimits);

            Assert.Throws<LocalStoreLimitException>(() => store.Set(new TutorialFlag()));
            Assert.That(_backend.Count, Is.EqualTo(1), "only the foreign key is on the backend");
        }

        // ---- edge 5: manifest integrity after external deletion ------------------------------------------------

        [Test]
        public void TryGet_AfterExternalDeletion_ReportsMissingAndPrunesManifest()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.Set(new TutorialFlag { Completed = true });
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Does.Contain(Prefixed(AudioKey)));

            // The outside world wipes one record behind any cache.
            _backend.Delete(Prefixed(AudioKey));

            Assert.That(_store.TryGet<AudioSettings>(out _, out var status), Is.False);
            Assert.That(status, Is.EqualTo(ReadStatus.Missing), "a dangling manifest entry is not an error");
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Does.Not.Contain(Prefixed(AudioKey)), "pruned");
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Does.Contain(Prefixed(TutorialKey)), "the other entry survives");
            Assert.That(_store.TryGet<TutorialFlag>(out var flag, out _), Is.True);
            Assert.That(flag.Completed, Is.True);
        }

        [Test]
        public void FreshInstance_ExternalDeletion_SelfHealsManifestOnLoad()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.Set(new TutorialFlag { Completed = true });
            _backend.Delete(Prefixed(TutorialKey));

            // A new process: no cache, the manifest loads from the backend.
            ILocalStoreService reopened = NewStore(_backend);

            Assert.That(reopened.Has<TutorialFlag>(), Is.False);
            Assert.That(reopened.Has<AudioSettings>(), Is.True);
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Is.EqualTo("[\"" + Prefixed(AudioKey) + "\"]"));

            reopened.Clear();

            Assert.That(_backend.Has(Prefixed(AudioKey)), Is.False);
            Assert.That(_backend.Has(LocalStoreKeys.ManifestKey), Is.False);
        }

        // ---- edge 6: Clear() leaves foreign keys untouched -----------------------------------------------------

        [Test]
        public void Clear_WithForeignAndOrphanKeys_RemovesOwnKeysOnly()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.Set(new TutorialFlag { Completed = true });

            // Prefixed but not in the manifest: still not the store's to delete.
            _backend.Set(Prefixed("orphan"), "{\"v\":1,\"d\":1}");

            _store.Clear();

            Assert.That(_backend.Has(Prefixed(AudioKey)), Is.False);
            Assert.That(_backend.Has(Prefixed(TutorialKey)), Is.False);
            Assert.That(_backend.Has(LocalStoreKeys.ManifestKey), Is.False);
            Assert.That(_backend.Read(ForeignKey), Is.EqualTo("1920"), "Clear() walks the manifest, never a bulk delete");
            Assert.That(_backend.Read(Prefixed("orphan")), Is.EqualTo("{\"v\":1,\"d\":1}"));
            Assert.That(_store.TryGet<AudioSettings>(out _, out var status), Is.False);
            Assert.That(status, Is.EqualTo(ReadStatus.Missing));
        }

        [Test]
        public void Remove_StoredRecord_DeletesRecordAndManifestEntry()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.Set(new TutorialFlag { Completed = true });

            _store.Remove<AudioSettings>();

            Assert.That(_store.Has<AudioSettings>(), Is.False);
            Assert.That(_backend.Read(LocalStoreKeys.ManifestKey), Is.EqualTo("[\"" + Prefixed(TutorialKey) + "\"]"));
            Assert.DoesNotThrow(() => _store.Remove<AudioSettings>(), "removing an absent record is a no-op");
        }

        // ---- edge 7: unregistered type fails loudly ------------------------------------------------------------

        [Test]
        public void EveryMember_UnregisteredType_Throws()
        {
            Assert.Throws<LocalStoreUnregisteredTypeException>(() => _store.Set(new Unregistered()));
            Assert.Throws<LocalStoreUnregisteredTypeException>(() => _store.TryGet<Unregistered>(out _, out _));
            Assert.Throws<LocalStoreUnregisteredTypeException>(() => _store.Has<Unregistered>());
            Assert.Throws<LocalStoreUnregisteredTypeException>(() => _store.Remove<Unregistered>());
            Assert.That(_backend.Count, Is.EqualTo(1), "nothing was written");
        }

        // ---- design guarantees around the edges ----------------------------------------------------------------

        [Test]
        public void TryGet_ReturnsAFreshObjectPerCall_CacheHoldsRawStringsOnly()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.TryGet<AudioSettings>(out var first, out _);

            // A consumer mutates its copy.
            first.Volume = 0.9f;
            _store.TryGet<AudioSettings>(out var second, out _);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Volume, Is.EqualTo(0.5f), "no mutation leaks through a shared reference");
        }

        [Test]
        public void Flush_AfterSetRemoveClear_IsTheOnlySave()
        {
            _store.Set(new AudioSettings { Volume = 0.5f });
            _store.Remove<AudioSettings>();
            _store.Clear();
            Assert.That(_backend.SaveCount, Is.EqualTo(0), "the caller decides when to flush");

            _store.Flush();

            Assert.That(_backend.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void Envelope_WithTypeMetadata_IsNotHonoured()
        {
            // Type metadata is ignored: "$type" is just an unknown property, never a type hint.
            _backend.Set(Prefixed(AudioKey),
                "{\"v\":2,\"d\":{\"$type\":\"System.IO.FileInfo, System.IO.FileSystem\",\"Volume\":0.1,\"Muted\":false}}");

            Assert.That(_store.TryGet<AudioSettings>(out var value, out var status), Is.True);
            Assert.That(status, Is.EqualTo(ReadStatus.Ok));
            Assert.That(value, Is.TypeOf<AudioSettings>());
            Assert.That(value.Volume, Is.EqualTo(0.1f));
        }

        /// <summary>A fresh backend under test; called once per test.</summary>
        protected abstract ILocalStoreBackend CreateBackend();

        /// <summary>The store under test over <paramref name="backend"/>, with the given registry and limits.</summary>
        protected abstract ILocalStoreService CreateStore(ILocalStoreBackend backend, ILocalStoreRegistry registry, LocalStoreLimits limits);

        /// <summary>Called after each test; a backend with state outside the process deletes the touched keys here.</summary>
        protected virtual void CleanUp(TrackingLocalStoreBackend backend)
        {
        }

        private static ILocalStoreRegistry Registry() => new TestRegistry()
            .Add<AudioSettings>(AudioKey, AudioVersion)
            .Add<TutorialFlag>(TutorialKey, 1);

        private static string Prefixed(string key) => LocalStoreKeys.Prefix + key;

        private ILocalStoreService NewStore(TrackingLocalStoreBackend backend) => CreateStore(backend, Registry(), s_testLimits);

        // ---- test-local payloads and registry ------------------------------------------------------------------

        private sealed class AudioSettings
        {
            public float Volume { get; set; }

            public bool Muted { get; set; }
        }

        private sealed class TutorialFlag
        {
            public bool Completed { get; set; }
        }

        private sealed class Unregistered
        {
        }

        private sealed class TestRegistry : ILocalStoreRegistry
        {
            private readonly Dictionary<Type, LocalStoreEntry> _entries = new Dictionary<Type, LocalStoreEntry>();

            public TestRegistry Add<T>(string key, int version)
            {
                _entries[typeof(T)] = new LocalStoreEntry(key, version);
                return this;
            }

            public bool TryResolve(Type type, out LocalStoreEntry entry) => _entries.TryGetValue(type, out entry);
        }
    }
}
