using System;
using Biome.GameStates.Config;
using NUnit.Framework;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>The flow config: its validation, its transitions and its scenes.</summary>
    [TestFixture]
    public sealed class GameStatesFlowConfigTests
    {
        private static string Name<T>() => typeof(T).AssemblyQualifiedName;

        private static GameStatesFlowConfig Flow(string name, string first = null)
        {
            return new GameStatesFlowConfig
            {
                Name = name,
                InitState = first ?? Name<StubInit>(),
                States = new[]
                {
                    new GameStatesFlowConfig.StateEntry { StateType = Name<StubInit>(), Scene = string.Empty },
                    new GameStatesFlowConfig.StateEntry { StateType = Name<StubMenu>(), Scene = "scenes/menu" },
                    new GameStatesFlowConfig.StateEntry { StateType = Name<StubGameplay>(), Scene = "scenes/gameplay" },
                },
                Transitions = new[]
                {
                    new GameStatesFlowConfig.TransitionEntry { From = Name<StubInit>(), To = Name<StubMenu>() },
                },
            };
        }

        [Test]
        public void Validate_GoodFlow_Passes()
        {
            Assert.DoesNotThrow(() => Flow("Good").Validate());
        }

        [Test]
        public void Validate_UnknownStateType_ThrowsNamingFlowAndType()
        {
            GameStatesFlowConfig flow = Flow("Broken");
            flow.States[1].StateType = "No.Such.State, Nowhere";

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => flow.Validate());

            Assert.That(ex.Message, Does.Contain("Broken").And.Contain("No.Such.State"));
        }

        [Test]
        public void Validate_NonInitStateWithoutScene_Passes()
        {
            GameStatesFlowConfig flow = Flow("NoScene");
            flow.States[2].Scene = string.Empty;

            Assert.DoesNotThrow(() => flow.Validate());
            Assert.That(flow.TryGetScene(typeof(StubGameplay), out _), Is.False, "a state without a scene has none to give");
        }

        [Test]
        public void Validate_FirstStateNotInTheList_Throws()
        {
            GameStatesFlowConfig flow = Flow("Lost", Name<StubOutsider>());

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => flow.Validate());

            Assert.That(ex.Message, Does.Contain("Lost").And.Contain("first state"));
        }

        [Test]
        public void Validate_BlankFirstState_Throws()
        {
            GameStatesFlowConfig flow = Flow("Blank");
            flow.InitState = string.Empty;

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => flow.Validate());

            Assert.That(ex.Message, Does.Contain("Blank").And.Contain("first state"));
        }

        [Test]
        public void Validate_OneStateWithNoTransitions_Passes()
        {
            GameStatesFlowConfig flow = new GameStatesFlowConfig
            {
                Name = "Single",
                InitState = Name<StubInit>(),
                States = new[] { new GameStatesFlowConfig.StateEntry { StateType = Name<StubInit>(), Scene = string.Empty } },
            };

            Assert.DoesNotThrow(() => flow.Validate());
        }

        [Test]
        public void Allows_ListedTransition_TrueAndTheReverseFalse()
        {
            GameStatesFlowConfig flow = Flow("Seq");

            Assert.That(flow.Allows(typeof(StubInit), typeof(StubMenu)), Is.True);
            Assert.That(flow.Allows(typeof(StubMenu), typeof(StubInit)), Is.False);
        }

        [Test]
        public void TryGetScene_ListedAndUnlistedState_GivesTheSceneOrFalse()
        {
            GameStatesFlowConfig flow = Flow("Scenes");

            Assert.That(flow.TryGetScene(typeof(StubGameplay), out string scene), Is.True);
            Assert.That(scene, Is.EqualTo("scenes/gameplay"));
            Assert.That(flow.TryGetScene(typeof(StubInit), out _), Is.False, "Init has no scene");
            Assert.That(flow.TryGetScene(typeof(StubOutsider), out _), Is.False);
        }

        private sealed class StubInit
        {
        }

        private sealed class StubMenu
        {
        }

        private sealed class StubGameplay
        {
        }

        private sealed class StubOutsider
        {
        }
    }
}
