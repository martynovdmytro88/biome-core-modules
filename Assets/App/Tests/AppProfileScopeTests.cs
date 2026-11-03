#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Biome.Configs.Config;
using Biome.GameStates.Abstract;
using Biome.GameStates.Config;
using Biome.GameStates.Installers;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace Biome.App.Tests
{
    /// <summary>
    /// The profile scope as data and as a boot: the values on the two shipped prefabs, where each asset sits, what a
    /// scope with a missing or broken part says, the flow the container holds, the states each flow registers, and the
    /// boot of both profiles by id.
    /// </summary>
    public sealed class AppProfileScopeTests
    {
        private const string GamePrefabPath = "Assets/App/Composition/Profiles/Game.prefab";
        private const string GameAssetPath = "Assets/App/Composition/Profiles/Resources/Game.asset";
        private const string TestPrefabPath = "Assets/App/Tests/TestFlow/Test.prefab";
        private const string TestAssetPath = "Assets/App/Tests/TestFlow/Editor/Resources/Test.asset";
        private const string TestFlowPath = "Assets/App/Tests/TestFlow/TestFlow.asset";
        private const string AppFlowPath = "Assets/App/Config/AppFlow.asset";
        private const string ProductionPath = "Assets/App/Config/Configs/Resources/EnvironmentConfig.Production.asset";
        private const string TestEnvironmentPath = "Assets/App/Config/Configs/Resources/EnvironmentConfig.Test.asset";

        private readonly List<Object> _created = new List<Object>();

        private static AppCompositionRoot Asset() => Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);

        private static T Load<T>(string path)
            where T : Object
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        [UnityTearDown]
        public IEnumerator RestoreBootstrapRoot()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
            AppCompositionRoot.Shutdown();
            yield return null; // Destroy lands at the end of the frame
            AppCompositionRoot.Raise(Asset().SelectProfile());
        }

        [TestCase(GamePrefabPath, typeof(GameScope), ProductionPath, AppFlowPath)]
        [TestCase(TestPrefabPath, typeof(TestScope), TestEnvironmentPath, TestFlowPath)]
        public void Prefab_CarriesTheEnvironmentAndFlowOfItsVariant(string prefabPath, Type scopeType, string environmentPath, string flowPath)
        {
            var scope = Load<GameObject>(prefabPath).GetComponent<AppProfileScope>();

            Assert.That(scope, Is.InstanceOf(scopeType));
            var serialized = new UnityEditor.SerializedObject(scope);
            Assert.That(serialized.FindProperty("_environment").objectReferenceValue, Is.SameAs(Load<EnvironmentConfig>(environmentPath)));
            Assert.That(serialized.FindProperty("_flow").objectReferenceValue, Is.SameAs(Load<GameStatesFlowConfigSO>(flowPath)));
        }

        [TestCase(GamePrefabPath)]
        [TestCase(TestPrefabPath)]
        public void Prefab_EnvironmentAndFlowAreAdjacentFields(string prefabPath)
        {
            var scope = Load<GameObject>(prefabPath).GetComponent<AppProfileScope>();
            var names = new List<string>();
            var property = new UnityEditor.SerializedObject(scope).GetIterator();
            for (var enter = true; property.NextVisible(enter); enter = false)
            {
                names.Add(property.name);
            }

            var environment = names.IndexOf("_environment");
            Assert.That(environment, Is.GreaterThanOrEqualTo(0));
            Assert.That(names[environment + 1], Is.EqualTo("_flow"), "the environment and the flow sit side by side");
        }

        [TestCase(GamePrefabPath)]
        [TestCase(TestPrefabPath)]
        [TestCase(TestFlowPath)]
        public void Asset_IsOutsideAnyResourcesFolder(string path)
        {
            Assert.That(path, Does.Not.Contain("/Resources/"));
            Assert.That(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path), Is.Not.Null);
        }

        [Test]
        public void Profiles_ByFileName_LoadFromTheirResourcesFolders()
        {
            Assert.That(GameAssetPath, Does.Contain("/Resources/").And.Not.Contain("/Editor/"));
            Assert.That(TestAssetPath, Does.Contain("/Editor/Resources/"));
            Assert.That(Resources.Load<AppProfile>("Game"), Is.SameAs(Load<AppProfile>(GameAssetPath)));
            Assert.That(Resources.Load<AppProfile>("Test"), Is.SameAs(Load<AppProfile>(TestAssetPath)), "the Editor-only Resources folder answers in the Editor");
        }

        [Test]
        public void Profiles_EachProfile_PointsAtItsScopePrefab()
        {
            Assert.That(Load<AppProfile>(GameAssetPath).Scope, Is.SameAs(Load<GameObject>(GamePrefabPath).GetComponent<GameScope>()));
            Assert.That(Load<AppProfile>(TestAssetPath).Scope, Is.SameAs(Load<GameObject>(TestPrefabPath).GetComponent<TestScope>()));
        }

        [Test]
        public void ScopeAwake_NoEnvironment_ThrowsNamingThePrefab()
        {
            var ex = AwakeScope("BrokenScope", null, Load<GameStatesFlowConfigSO>(AppFlowPath));

            Assert.That(ex.Message, Does.Contain("BrokenScope").And.Contain("no environment"));
        }

        [Test]
        public void ScopeAwake_NoFlow_ThrowsNamingThePrefab()
        {
            var ex = AwakeScope("BrokenScope", Load<EnvironmentConfig>(ProductionPath), null);

            Assert.That(ex.Message, Does.Contain("BrokenScope").And.Contain("no state flow"));
        }

        [Test]
        public void ScopeAwake_FlowFailingValidate_FailsWithAReadableMessage()
        {
            var flow = ScriptableObject.CreateInstance<GameStatesFlowConfigSO>();
            _created.Add(flow);
            JsonUtility.FromJsonOverwrite("{\"_flow\":{\"Name\":\"BadFlow\",\"InitState\":\"NoSuchType\",\"States\":[],\"Transitions\":[]}}", flow);

            var ex = AwakeScope("BrokenScope", Load<EnvironmentConfig>(ProductionPath), flow);

            Assert.That(ex.Message, Does.Contain("Flow 'BadFlow'").And.Contain("'NoSuchType'").And.Contain("not a known type"));
        }

        [Test]
        public void Install_ListedStateWithNoCatalogEntry_ThrowsNamingFlowAndState()
        {
            var flow = Load<GameStatesFlowConfigSO>(TestFlowPath).ToConfig();

            // The app catalogue does not know the test state; only the test scope adds it.
            var ex = Assert.Throws<InvalidOperationException>(() => new AppStatesInstaller(flow).Install(new ContainerBuilder()));

            Assert.That(ex.Message, Does.Contain("TestFlow").And.Contain("TestInitState"));
        }

        [TestCase(typeof(GameScope), AppFlowPath)]
        [TestCase(typeof(TestScope), TestFlowPath)]
        public void Declare_EachFlow_RegistersExactlyTheStatesItLists(Type scopeType, string flowPath)
        {
            var flow = Load<GameStatesFlowConfigSO>(flowPath).ToConfig();
            var go = NewInactive("scope (template)");
            var scope = (AppProfileScope)go.AddComponent(scopeType);
            var states = new AppStatesInstaller(flow);
            typeof(AppProfileScope).GetMethod("DeclareStates", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scope, new object[] { states });
            var variants = (List<AppStateCatalog.Entry>)typeof(AppStatesInstaller)
                .GetField("_variantEntries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(states);
            var installer = NewMachineInstaller(flow);

            AppStateCatalog.Declare(flow, installer, variants);

            var listed = new List<Type>();
            foreach (var entry in flow.States)
            {
                listed.Add(GameStatesFlowConfig.ResolveState(entry.StateType));
            }

            Assert.That(Registered(installer), Is.EqualTo(listed));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Boot_GameById_RegistersACopyOfTheFlowAndNoScenario() => UniTask.ToCoroutine(async () =>
        {
            var container = await BootAsync("Game", typeof(GameScope));

            var flow = container.Resolve<GameStatesFlowConfig>();
            var asset = Load<GameStatesFlowConfigSO>(AppFlowPath);
            Assert.That(flow, Is.Not.SameAs(asset.ToConfig()));
            Assert.That(flow, Is.Not.SameAs(typeof(GameStatesFlowConfigSO).GetField("_flow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(asset)));
            flow.Name = "Changed";
            flow.States[0].Scene = "changed/scene";
            Assert.That(asset.ToConfig().Name, Is.EqualTo("AppFlow"), "the registered flow is a copy; the asset is not touched");
            Assert.That(asset.ToConfig().States[0].Scene, Is.EqualTo(string.Empty));
            Assert.That(container.TryResolve<IFlowScenario>(out _), Is.False, "the game registers no flow scenario");
        });

        [UnityTest, Timeout(60000)]
        public IEnumerator Boot_TestById_RegistersACopyOfTheTestFlow() => UniTask.ToCoroutine(async () =>
        {
            var container = await BootAsync("Test", typeof(TestScope));

            var flow = container.Resolve<GameStatesFlowConfig>();
            Assert.That(flow.Name, Is.EqualTo("TestFlow"));
            Assert.That(flow, Is.Not.SameAs(Load<GameStatesFlowConfigSO>(TestFlowPath).ToConfig()));
            Assert.That(container.Resolve<StateFlowStarter>(), Is.Not.Null);
        });

        private static async UniTask<IObjectResolver> BootAsync(string id, Type scopeType)
        {
            var profile = Resources.Load<AppProfile>(id);
            Assert.That(profile, Is.Not.Null, $"the profile '{id}' loads by id");
            AppCompositionRoot.Shutdown();
            await UniTask.NextFrame();
            var scope = AppCompositionRoot.Raise(profile);
            Assert.That(scope, Is.InstanceOf(scopeType));
            return scope.Container;
        }

        // Built by reflection: this assembly does not reference Biome.GameStates, where the descriptor interface lives.
        private static GameStatesInstaller NewMachineInstaller(GameStatesFlowConfig flow)
        {
            var descriptors = Activator.CreateInstance(typeof(ConfigFlowDescriptorProvider), flow);
            return (GameStatesInstaller)Activator.CreateInstance(typeof(GameStatesInstaller), descriptors, null);
        }

        private static List<Type> Registered(GameStatesInstaller installer)
        {
            var registrations = (IEnumerable)typeof(GameStatesInstaller).GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(installer);
            var result = new List<Type>();
            foreach (var registration in registrations)
            {
                result.Add((Type)registration.GetType().GetProperty("State").GetValue(registration));
            }

            return result;
        }

        private GameObject NewInactive(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            _created.Add(go);
            return go;
        }

        // The scope takes its profile in Awake from the hand-over. Awake is called by hand on an inactive object, so the
        // failure comes back as the exception itself instead of a console line.
        private InvalidOperationException AwakeScope(string name, EnvironmentConfig environment, GameStatesFlowConfigSO flow)
        {
            var profile = ScriptableObject.CreateInstance<AppProfile>();
            _created.Add(profile);
            var go = NewInactive(name);
            var scope = go.AddComponent<GameScope>();
            var serialized = new UnityEditor.SerializedObject(scope);
            serialized.FindProperty("_environment").objectReferenceValue = environment;
            serialized.FindProperty("_flow").objectReferenceValue = flow;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var awake = typeof(AppProfileScope).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            using (AppProfileScope.Handover(profile))
            {
                var wrapped = Assert.Throws<TargetInvocationException>(() => awake.Invoke(scope, null));
                Assert.That(wrapped.InnerException, Is.InstanceOf<InvalidOperationException>());
                return (InvalidOperationException)wrapped.InnerException;
            }
        }
    }
}
#endif
