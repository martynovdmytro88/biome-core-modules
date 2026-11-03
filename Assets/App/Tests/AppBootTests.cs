#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Biome.GameStates.Config;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace Biome.App.Tests
{
    /// <summary>
    /// The boot of the game profile with no project state: the shipped profile enters Init with the configs loaded and
    /// stays there; the same scope and flow over an environment that fails the init take the route to Emergency, which
    /// has no scene.
    /// </summary>
    public sealed class AppBootTests
    {
        private const string GameAssetPath = "Assets/App/Composition/Profiles/Resources/Game.asset";
        private const string AppFlowPath = "Assets/App/Config/AppFlow.asset";
        private const string DuplicateSources =
            "{\"_id\":\"Broken\",\"_sources\":[{\"_configType\":\"Duplicate\"},{\"_configType\":\"Duplicate\"}]}";

        private readonly List<Object> _created = new List<Object>();

        private static AppCompositionRoot Asset() => Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);

        [UnityTearDown]
        public IEnumerator RestoreBootstrapRoot()
        {
            LogAssert.ignoreFailingMessages = false;
            AppCompositionRoot.Shutdown();
            yield return null; // Destroy lands at the end of the frame
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
            AppCompositionRoot.Raise(Asset().SelectProfile());
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Boot_Game_EntersInitWithConfigsLoadedAndStaysThere() => UniTask.ToCoroutine(async () =>
        {
            var game = UnityEditor.AssetDatabase.LoadAssetAtPath<AppProfile>(GameAssetPath);
            Assert.That(Asset().SelectProfile(), Is.SameAs(game), "the composition root selects the shipped profile");

            AppCompositionRoot.Shutdown();
            await UniTask.NextFrame();
            var container = AppCompositionRoot.Raise(game).Container;
            var bases = new List<Type>();
            using (container.Resolve<IEventBus>().Subscribe<StateSwitchedEvent>((in StateSwitchedEvent e) => AddBase(bases, e)))
            {
                await container.Resolve<StateFlowStarter>().RunAsync(default);
                await UniTask.NextFrame();

                var configs = container.Resolve<IConfigsService>();
                Assert.That(container.Resolve<IGameStateMachine>().Current, Is.EqualTo(typeof(InitState)));
                Assert.That(bases, Is.EqualTo(new[] { typeof(InitState) }));
                Assert.That(configs.IsInitialized, Is.True, "Init initialised the configs");
                Assert.That(configs.IsLoaded, Is.True, "and loaded them");
            }
        });

        [UnityTest, Timeout(60000)]
        public IEnumerator Boot_InitFails_TakesTheRouteToEmergencyWithNoScene() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true; // the machine logs the failure it routes
            var profile = ProfileOverBrokenEnvironment();
            var scenes = SceneManager.sceneCount;

            AppCompositionRoot.Shutdown();
            await UniTask.NextFrame();
            var container = AppCompositionRoot.Raise(profile).Container;
            var bases = new List<Type>();
            var errors = new List<Type>();
            var bus = container.Resolve<IEventBus>();
            using (bus.Subscribe<StateSwitchedEvent>((in StateSwitchedEvent e) => AddBase(bases, e)))
            using (bus.Subscribe<StateErrorEvent>((in StateErrorEvent e) => errors.Add(e.State)))
            {
                await container.Resolve<StateFlowStarter>().RunAsync(default);
                var machine = container.Resolve<IGameStateMachine>();
                await UniTask.WaitUntil(() => machine.Current == typeof(EmergencyState));

                Assert.That(errors, Is.EqualTo(new[] { typeof(InitState) }), "the failure of Init took the error route");
                Assert.That(bases, Is.EqualTo(new[] { typeof(EmergencyState) }), "Init never entered; Emergency did");
                Assert.That(container.Resolve<IConfigsService>().IsInitialized, Is.False);
                Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes), "Emergency loads no scene");
            }
        });

        private static void AddBase(List<Type> bases, in StateSwitchedEvent e)
        {
            if (e.Level == StateLevel.Base)
            {
                bases.Add(e.To);
            }
        }

        // The shipped flow on the game scope; only the environment differs, and it fails the service's own guard.
        private AppProfile ProfileOverBrokenEnvironment()
        {
            var environment = ScriptableObject.CreateInstance<EnvironmentConfig>();
            _created.Add(environment);
            JsonUtility.FromJsonOverwrite(DuplicateSources, environment);

            var go = new GameObject("Game (broken environment)");
            go.SetActive(false);
            _created.Add(go);
            var scope = new UnityEditor.SerializedObject(go.AddComponent<GameScope>());
            scope.FindProperty("_environment").objectReferenceValue = environment;
            scope.FindProperty("_flow").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameStatesFlowConfigSO>(AppFlowPath);
            scope.ApplyModifiedPropertiesWithoutUndo();

            var profile = ScriptableObject.CreateInstance<AppProfile>();
            _created.Add(profile);
            var serialized = new UnityEditor.SerializedObject(profile);
            serialized.FindProperty("_scope").objectReferenceValue = go.GetComponent<GameScope>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }
    }
}
#endif
