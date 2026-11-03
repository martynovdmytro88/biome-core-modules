#if UNITY_EDITOR
using System;
using Biome.GameStates.Config;
using NUnit.Framework;

namespace Biome.App.Tests
{
    /// <summary>
    /// The two shipped flow assets as data: each lists the expected states, scenes and transitions (asserted as literals,
    /// not read from another asset), validates, hands out copies, and sits outside any Resources folder. A state with no
    /// scene is a valid state.
    /// </summary>
    public sealed class AppFlowAssetsTests
    {
        private const string AppFlowPath = "Assets/App/Config/AppFlow.asset";
        private const string TestFlowPath = "Assets/App/Tests/TestFlow/TestFlow.asset";

        private const string Init = "Biome.App.InitState, Biome.App";
        private const string Emergency = "Biome.App.EmergencyState, Biome.App";
        private const string TestInit = "Biome.App.Tests.TestInitState, Biome.App.Tests";

        private static GameStatesFlowConfigSO Load(string path)
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameStatesFlowConfigSO>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        [Test]
        public void AppFlow_ToConfig_HasTheExpectedStatesAndTransition()
        {
            var flow = Load(AppFlowPath).ToConfig();

            Assert.That(flow.Name, Is.EqualTo("AppFlow"));
            Assert.That(flow.InitState, Is.EqualTo(Init));
            Assert.That(flow.States, Has.Length.EqualTo(2));
            AssertState(flow.States[0], Init, string.Empty);
            AssertState(flow.States[1], Emergency, string.Empty);
            Assert.That(flow.Transitions, Has.Length.EqualTo(1));
            AssertTransition(flow.Transitions[0], Init, Emergency);
        }

        [Test]
        public void TestFlow_ToConfig_IsTheOneTestInitStateWithNoTransitions()
        {
            var flow = Load(TestFlowPath).ToConfig();

            Assert.That(flow.Name, Is.EqualTo("TestFlow"));
            Assert.That(flow.InitState, Is.EqualTo(TestInit));
            Assert.That(flow.States, Has.Length.EqualTo(1));
            AssertState(flow.States[0], TestInit, string.Empty);
            Assert.That(flow.Transitions, Is.Empty);
        }

        [TestCase(AppFlowPath)]
        [TestCase(TestFlowPath)]
        public void Validate_BothFlows_Pass(string path)
        {
            var flow = Load(path).ToConfig();

            Assert.DoesNotThrow(flow.Validate);
            Assert.That(flow.ResolveInitState(), Is.Not.Null, "the first state resolves to a type");
        }

        [Test]
        public void Validate_NonInitStateWithNoScene_Passes_AndReportsNoScene()
        {
            var flow = Load(AppFlowPath).ToConfig();

            Assert.DoesNotThrow(flow.Validate);
            Assert.That(flow.TryGetScene(typeof(EmergencyState), out _), Is.False);
        }

        [TestCase(AppFlowPath)]
        [TestCase(TestFlowPath)]
        public void ToConfig_ChangingTheCopy_DoesNotChangeTheAsset(string path)
        {
            var asset = Load(path);
            var before = UnityEngine.JsonUtility.ToJson(asset.ToConfig());

            var copy = asset.ToConfig();
            copy.Name = "Changed";
            copy.InitState = "Changed";
            copy.States[0].Scene = "changed/scene";
            copy.States[0].StateType = "Changed";
            copy.States = Array.Empty<GameStatesFlowConfig.StateEntry>();
            copy.Transitions = new[] { new GameStatesFlowConfig.TransitionEntry { From = "x", To = "y" } };

            Assert.That(UnityEngine.JsonUtility.ToJson(asset.ToConfig()), Is.EqualTo(before));
            Assert.That(asset.ToConfig(), Is.Not.SameAs(asset.ToConfig()), "every call is a new copy");
        }

        [Test]
        public void ToConfig_ChangingAnEntryOfTheCopy_DoesNotChangeTheAsset()
        {
            var asset = Load(AppFlowPath);

            asset.ToConfig().States[1].Scene = "changed/scene";
            asset.ToConfig().Transitions[0].To = "Changed";

            Assert.That(asset.ToConfig().States[1].Scene, Is.EqualTo(string.Empty));
            Assert.That(asset.ToConfig().Transitions[0].To, Is.EqualTo(Emergency));
        }

        [TestCase(AppFlowPath)]
        [TestCase(TestFlowPath)]
        public void Assets_AreNotUnderAResourcesFolder(string path)
        {
            Assert.That(path, Does.Not.Contain("/Resources/"));
            Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<GameStatesFlowConfigSO>(path), Is.Not.Null);
        }

        private static void AssertState(GameStatesFlowConfig.StateEntry entry, string type, string scene)
        {
            Assert.That(entry.StateType, Is.EqualTo(type));
            Assert.That(entry.Scene, Is.EqualTo(scene));
        }

        private static void AssertTransition(GameStatesFlowConfig.TransitionEntry entry, string from, string to)
        {
            Assert.That(entry.From, Is.EqualTo(from));
            Assert.That(entry.To, Is.EqualTo(to));
        }
    }
}
#endif
