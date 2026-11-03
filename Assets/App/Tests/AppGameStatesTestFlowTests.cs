#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Biome.EventBus.Abstract;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using Biome.GameStates.Events;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.App.Tests
{
    /// <summary>
    /// The test flow over the test profile: the profile selects <see cref="TestScope"/>, whose flow lists the one state
    /// <see cref="TestInitState"/>. The flow starts there and goes nowhere.
    /// </summary>
    public sealed class AppGameStatesTestFlowTests
    {
        private const string TestProfilePath = "Assets/App/Tests/TestFlow/Editor/Resources/Test.asset";

        private static AppCompositionRoot Asset() => Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);

        [UnityTearDown]
        public IEnumerator RestoreBootstrapRoot()
        {
            AppCompositionRoot.Shutdown();
            yield return null; // Destroy lands at the end of the frame
            AppCompositionRoot.Raise(Asset().SelectProfile());
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Flow_Test_StartsInTestInitAndStaysThere() => UniTask.ToCoroutine(async () =>
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<AppProfile>(TestProfilePath);
            Assert.That(profile, Is.Not.Null);
            AppCompositionRoot.Shutdown();
            await UniTask.NextFrame();
            var container = AppCompositionRoot.Raise(profile).Container;
            var switches = new List<StateSwitchedEvent>();
            using (container.Resolve<IEventBus>().Subscribe<StateSwitchedEvent>((in StateSwitchedEvent e) => switches.Add(e)))
            {
                await container.Resolve<StateFlowStarter>().RunAsync(default);
                await UniTask.NextFrame();

                Assert.That(AppCompositionRoot.Active, Is.InstanceOf<TestScope>());
                Assert.That(container.Resolve<IGameStateMachine>().Current, Is.EqualTo(typeof(TestInitState)));
                var bases = switches.FindAll(e => e.Level == StateLevel.Base).ConvertAll(e => e.To);
                Assert.That(bases, Is.EqualTo(new[] { typeof(TestInitState) }), "the test flow enters its one state and goes nowhere");
            }
        });
    }
}
#endif
