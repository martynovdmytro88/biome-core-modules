using Biome.GameStates.Abstract;
using Biome.GameStates.Editor.Tests;
using NUnit.Framework;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>
    /// Binds the package's contract suite to the production machine over fake scopes, scenes, bus and log. The machine
    /// needs an Init state first, so <see cref="TestStateA"/> is Init and the other two are base states.
    /// </summary>
    [TestFixture]
    public sealed class ProductionGameStateMachineContractTests : GameStateMachineContractTests
    {
        protected override IGameStateMachine CreateMachine()
        {
            var harness = new GameStateMachineTests.Harness();
            harness.AddInit(new TestStateA());
            harness.AddBase(new TestStateB());
            harness.AddBase(new TestStateC());
            return harness.Machine;
        }
    }
}
