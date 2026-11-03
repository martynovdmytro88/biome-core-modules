using System.Collections;
using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.GameStates.Abstract;
using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using Biome.LocalStore.Abstract;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Biome.App.Tests
{
    /// <summary>
    /// The composition root over the real start path: the bootstrap has raised the default profile before the test
    /// scene loaded. A test that boots again shuts the root down, waits the frame its Destroy takes, and raises the
    /// prod profile; TearDown puts the bootstrap's profile back the same way.
    /// </summary>
    public sealed class AppCompositionRootTests
    {
        private readonly List<Object> _created = new List<Object>();

        private static AppCompositionRoot Asset() => Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);

        private static IEnumerator Reraise(AppProfile profile)
        {
            AppCompositionRoot.Shutdown();
            yield return null; // Destroy lands at the end of the frame
            AppCompositionRoot.Raise(profile);
        }

        [UnityTearDown]
        public IEnumerator RestoreBootstrapRoot()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
            yield return Reraise(Asset().SelectProfile());
        }

        [Test]
        public void Bootstrap_RootIsUp_InDontDestroyOnLoad()
        {
            var root = AppCompositionRoot.Active;

            Assert.That(root != null, "the BeforeSceneLoad bootstrap raised a root");
            Assert.That(root.IsRoot, Is.True);
            Assert.That(root.Container, Is.Not.Null, "built eagerly");
            Assert.That(root.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(root, Is.InstanceOf<GameScope>(), "the root's id is Game");
        }

        [Test]
        public void Prod_EventBusAndLocalStore_Resolve()
        {
            var container = AppCompositionRoot.Active.Container;

            var bus = container.Resolve<IEventBus>();
            Assert.That(bus, Is.Not.Null.And.Not.InstanceOf<RecordingEventBus>(), "the production bus");
            Assert.That(container.Resolve<IEventPublisher>(), Is.SameAs(bus));
            Assert.That(container.Resolve<IEventSubscriber>(), Is.SameAs(bus));
            Assert.That(container.Resolve<ILocalStoreService>(), Is.Not.Null);
            Assert.That(container.Resolve<ILocalStoreService>(), Is.SameAs(container.Resolve<ILocalStoreService>()));
        }

        [Test]
        public void SceneScope_WithNoSetup_ParentsToTheRoot()
        {
            var go = new GameObject("Scene scope");
            _created.Add(go);
            var scope = go.AddComponent<LifetimeScope>(); // Awake builds it: no parent reference, no settings asset

            Assert.That(scope.Parent, Is.SameAs(AppCompositionRoot.Active));
            Assert.That(scope.Container.Resolve<IEventBus>(), Is.SameAs(AppCompositionRoot.Active.Container.Resolve<IEventBus>()));
        }

        [Test]
        public void Prod_Raised_RegistersTheProfilesEnvironmentInstance()
        {
            var container = AppCompositionRoot.Active.Container;

            Assert.That(container.Resolve<IConfigsEnvironment>(), Is.SameAs(Asset().SelectProfile().Scope.Environment));
        }

        [Test]
        public void Prod_Raised_RegistersTheFlowStarter()
        {
            Assert.That(AppCompositionRoot.Active.Container.Resolve<StateFlowStarter>(), Is.Not.Null);
        }

        [Test]
        public void Prod_InstallerOrder_EventBusIsTheOneConfigsAndStatesConsumersUse()
        {
            var container = AppCompositionRoot.Active.Container;
            var bus = container.Resolve<IEventBus>();

            Assert.That(container.Resolve<IGameStateMachine>(), Is.Not.Null, "GameStates built over the bus");
            Assert.That(container.Resolve<IConfigsService>(), Is.Not.Null);
            Assert.That(container.Resolve<IEventBus>(), Is.SameAs(bus), "one bus for the whole root");
        }

        [UnityTest]
        public IEnumerator Prod_RaisedAfterShutdown_YieldsTheProductionRoot()
        {
            yield return Reraise(Asset().SelectProfile());

            Assert.That(AppCompositionRoot.Active.IsRoot, Is.True);
            Assert.That(AppCompositionRoot.Active, Is.InstanceOf<GameScope>());
            Assert.That(AppCompositionRoot.Active.Container.Resolve<IEventBus>(), Is.Not.InstanceOf<RecordingEventBus>());
            Assert.That(AppCompositionRoot.Active.Container.Resolve<ILocalStoreService>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Shutdown_DisposesSingletonsAndChildScopes_AndTickingStops()
        {
            var root = AppCompositionRoot.Active;
            var bus = root.Container.Resolve<IEventBus>();
            var child = root.CreateChild<LifetimeScope>(TestInstallers.SpyAndTicker(), "Child scope");
            var spy = child.Container.Resolve<SpyDisposable>();
            var ticker = child.Container.Resolve<TickCounter>();

            yield return null;
            yield return null;
            Assert.That(ticker.Ticks, Is.GreaterThan(0), "the child's entry point ticks while its scope lives");

            AppCompositionRoot.Shutdown();
            yield return null; // the root's Destroy, and with it the child's, lands at the end of the frame
            var ticksAtDispose = ticker.Ticks;
            yield return null;
            yield return null;

            Assert.That(AppCompositionRoot.Active == null, Is.True);
            Assert.Throws<System.ObjectDisposedException>(() => bus.Publish(0), "the root's singleton bus is disposed");
            Assert.That(spy.DisposeCount, Is.EqualTo(1), "the child scope under the root's GameObject is disposed");
            Assert.That(child == null, Is.True, "the child's GameObject went with the root's");
            Assert.That(ticker.Ticks, Is.EqualTo(ticksAtDispose), "no tick after the scope is disposed");
        }

        [UnityTest]
        public IEnumerator ChildScopeDispose_StopsItsTickable_RootLives()
        {
            var child = AppCompositionRoot.Active.CreateChild<LifetimeScope>(TestInstallers.SpyAndTicker(), "Child scope");
            var ticker = child.Container.Resolve<TickCounter>();
            yield return null;

            child.Dispose();
            var ticksAtDispose = ticker.Ticks;
            yield return null;
            yield return null;

            Assert.That(ticker.Ticks, Is.EqualTo(ticksAtDispose), "no tick after the scope is disposed");
            Assert.That(AppCompositionRoot.Active.Container, Is.Not.Null, "the root outlives its child");
        }

        [UnityTest]
        public IEnumerator Settings_UnloadUnusedAssets_Survive()
        {
            var settings = VContainerSettings.Instance;
            Assert.That(settings != null, "the bootstrap's settings instance is live");

            yield return Resources.UnloadUnusedAssets();

            Assert.That(VContainerSettings.Instance != null, Is.True, "not unloaded");
            Assert.That(VContainerSettings.Instance, Is.SameAs(settings));
            var go = new GameObject("Scene scope after unload");
            _created.Add(go);
            Assert.That(go.AddComponent<LifetimeScope>().Parent, Is.SameAs(AppCompositionRoot.Active), "a later scene scope still finds the root");
        }

        [Test]
        public void SelectProfile_ShippedRoot_LoadsTheGameProfile()
        {
            Assert.That(Asset().SelectProfile().name, Is.EqualTo("Game"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("NoSuchProfile")]
        public void SelectProfile_BlankOrUnknownId_ThrowsNamingTheIdAndRaisesNothing(string id)
        {
            var root = ScriptableObject.CreateInstance<AppCompositionRoot>();
            _created.Add(root);
            typeof(AppCompositionRoot)
                .GetField("_profileId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(root, id);
            var active = AppCompositionRoot.Active;

            var ex = Assert.Throws<System.InvalidOperationException>(() => root.SelectProfile());

            Assert.That(ex.Message, Does.Contain($"'{id}'"));
            Assert.That(AppCompositionRoot.Active, Is.SameAs(active), "no scope was raised or shut down");
        }

#if UNITY_EDITOR
        [Test]
        public void SelectProfile_OverrideKeySet_LoadsThatProfileOnceThenTheAssetsOwn()
        {
            UnityEditor.SessionState.SetString(AppCompositionRoot.EditorProfileIdKey, "Test");
            try
            {
                Assert.That(Asset().SelectProfile().name, Is.EqualTo("Test"));
                Assert.That(UnityEditor.SessionState.GetString(AppCompositionRoot.EditorProfileIdKey, string.Empty), Is.Empty, "consumed");
                Assert.That(Asset().SelectProfile().name, Is.EqualTo("Game"));
            }
            finally
            {
                UnityEditor.SessionState.EraseString(AppCompositionRoot.EditorProfileIdKey);
            }
        }

        [TestCase("NoSuchProfile")]
        [TestCase("   ")]
        public void SelectProfile_OverrideKeyUnknown_ThrowsNamingTheIdAndIsConsumed(string id)
        {
            UnityEditor.SessionState.SetString(AppCompositionRoot.EditorProfileIdKey, id);
            try
            {
                var ex = Assert.Throws<System.InvalidOperationException>(() => Asset().SelectProfile());

                Assert.That(ex.Message, Does.Contain($"'{id}'"));
                Assert.That(UnityEditor.SessionState.GetString(AppCompositionRoot.EditorProfileIdKey, string.Empty), Is.Empty, "consumed");
                Assert.That(Asset().SelectProfile().name, Is.EqualTo("Game"), "the next launch is the asset's");
            }
            finally
            {
                UnityEditor.SessionState.EraseString(AppCompositionRoot.EditorProfileIdKey);
            }
        }
#endif

        [Test]
        public void Raise_WhileARootIsUp_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => AppCompositionRoot.Raise(Asset().SelectProfile()));
        }
    }
}
