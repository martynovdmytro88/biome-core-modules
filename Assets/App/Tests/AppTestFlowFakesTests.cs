#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Biome.Assets.Abstract;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Biome.LocalStore;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using Biome.PlayerData;
using Biome.PlayerData.Abstract;
using Biome.PlayerData.Fakes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.App.Tests
{
    /// <summary>
    /// The fakes of the test flow's Init state: which services the Init scope resolves to a fake, which stay real, that the
    /// fakes sit in the state scope and not in the root, and that a full <c>Test</c> boot writes no PlayerPrefs.
    /// </summary>
    public sealed class AppTestFlowFakesTests
    {
        private const string TestProfilePath = "Assets/App/Tests/TestFlow/Editor/Resources/Test.asset";
        private const string ProbeKey = "s7-probe";

        private static readonly string[] s_StoreKeys =
        {
            LocalStoreKeys.Prefix + PlayerDataStoreEntry.Key,
            LocalStoreKeys.ManifestKey,
            LocalStoreKeys.Prefix + ProbeKey,
        };

        private string[] _before;

        private static AppCompositionRoot Asset() => Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);

        private static string Snapshot(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        [SetUp]
        public void RememberPlayerPrefs()
        {
            _before = Array.ConvertAll(s_StoreKeys, Snapshot);
        }

        [UnityTearDown]
        public IEnumerator RestorePlayerPrefsAndBootstrapRoot()
        {
            for (var i = 0; i < s_StoreKeys.Length; i++)
            {
                if (_before[i] == null)
                {
                    PlayerPrefs.DeleteKey(s_StoreKeys[i]);
                }
                else
                {
                    PlayerPrefs.SetString(s_StoreKeys[i], _before[i]);
                }
            }

            AppCompositionRoot.Shutdown();
            yield return null; // Destroy lands at the end of the frame
            AppCompositionRoot.Raise(Asset().SelectProfile());
        }

        private static async UniTask<IObjectResolver> BootTestFlowAsync()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<AppProfile>(TestProfilePath);
            Assert.That(profile, Is.Not.Null);
            AppCompositionRoot.Shutdown();
            await UniTask.NextFrame();
            var container = AppCompositionRoot.Raise(profile).Container;
            await container.Resolve<StateFlowStarter>().RunAsync(default);
            await UniTask.NextFrame();
            return container;
        }

        // The machine keeps the Init scope private; the test reads it from the machine's fields.
        private static IObjectResolver InitScopeOf(IObjectResolver container)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var machine = container.Resolve<IGameStateMachine>();
            var initScope = machine.GetType().GetField("_initScope", flags).GetValue(machine);
            Assert.That(initScope, Is.Not.Null, "the machine opened its Init scope");
            var scope = initScope.GetType().GetField("_scope", flags).GetValue(initScope);
            return (IObjectResolver)scope.GetType().GetProperty("Resolver").GetValue(scope);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator InitScope_ThreeServices_ResolveToFakes() => UniTask.ToCoroutine(async () =>
        {
            var scope = InitScopeOf(await BootTestFlowAsync());

            Assert.That(scope.Resolve<IPlayerDataService>(), Is.InstanceOf<InMemoryPlayerDataService>());
            Assert.That(scope.Resolve<IConfigsService>(), Is.InstanceOf<FakeConfigsService>());
            var store = scope.Resolve<ILocalStoreService>();
            var backend = store.GetType().GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(store);
            Assert.That(backend.GetType().Name, Is.EqualTo("InMemoryLocalStoreBackend"), "the store runs over the in-memory backend");
        });

        [UnityTest, Timeout(60000)]
        public IEnumerator InitScope_BusAssetsMachine_StayReal() => UniTask.ToCoroutine(async () =>
        {
            var container = await BootTestFlowAsync();
            var scope = InitScopeOf(container);

            Assert.That(scope.Resolve<IEventBus>(), Is.SameAs(container.Resolve<IEventBus>()));
            Assert.That(scope.Resolve<IAssetsService>(), Is.SameAs(container.Resolve<IAssetsService>()));
            Assert.That(scope.Resolve<IGameStateMachine>(), Is.SameAs(container.Resolve<IGameStateMachine>()));
            Assert.That(container.Resolve<IEventBus>().GetType().Name, Does.Not.Contain("Fake"));
        });

        [UnityTest, Timeout(60000)]
        public IEnumerator Root_RegistersTheRealServices_NotTheFakes() => UniTask.ToCoroutine(async () =>
        {
            var container = await BootTestFlowAsync();

            // Looked up without resolving: a resolve would create the real service.
            Assert.That(container.TryGetRegistration(typeof(IPlayerDataService), out var player), Is.True);
            Assert.That(player.ImplementationType, Is.Not.EqualTo(typeof(InMemoryPlayerDataService)));
            Assert.That(container.TryGetRegistration(typeof(IConfigsService), out var configs), Is.True);
            Assert.That(configs.ImplementationType, Is.Not.EqualTo(typeof(FakeConfigsService)));
            Assert.That(container.TryGetRegistration(typeof(ILocalStoreService), out var store), Is.True);
            Assert.That(store.ImplementationType.Name, Does.Not.Contain("InMemory"));
        });

        [UnityTest, Timeout(60000)]
        public IEnumerator FullBoot_WithAWrite_LeavesPlayerPrefsUntouched() => UniTask.ToCoroutine(async () =>
        {
            var scope = InitScopeOf(await BootTestFlowAsync());

            scope.Resolve<IPlayerDataService>().MarkLaunched();
            scope.Resolve<ILocalStoreService>().Flush();

            for (var i = 0; i < s_StoreKeys.Length; i++)
            {
                Assert.That(Snapshot(s_StoreKeys[i]), Is.EqualTo(_before[i]), s_StoreKeys[i]);
            }
        });

        [Test]
        public void ProbeKeys_RealStoreWrite_IsSeen()
        {
            var registry = new DictionaryLocalStoreRegistry().Add<ProbeRecord>(ProbeKey, 1);
            var real = LocalStoreFactory.CreatePlayerPrefs(registry, LocalStoreLimits.Placeholder);

            real.Set(new ProbeRecord { Value = 1 });
            real.Flush();

            Assert.That(PlayerPrefs.HasKey(s_StoreKeys[2]), Is.True, "the probe can tell a real write; TearDown removes it");
        }

        private sealed class ProbeRecord
        {
            public int Value { get; set; }
        }
    }
}
#endif
