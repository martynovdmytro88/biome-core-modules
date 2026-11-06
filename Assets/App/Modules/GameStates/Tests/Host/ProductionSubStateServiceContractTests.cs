using System;
using System.Collections.Generic;
using Biome.EventBus.Fakes;
using Biome.GameStates.Abstract;
using Biome.GameStates.Editor.Tests;
using NUnit.Framework;

namespace Biome.GameStates.Tests.Host
{
    /// <summary>
    /// Binds the package's contract suite to the production sub-state service, bound to a base-state scope that holds
    /// <see cref="TestStateA"/> and <see cref="TestStateB"/> as its sub-states.
    /// </summary>
    [TestFixture]
    public sealed class ProductionSubStateServiceContractTests : SubStateServiceContractTests
    {
        protected override ISubStateService CreateService()
        {
            var bus = new RecordingEventBus();
            var logger = new StateSwitchLogger(new GameStateMachineTests.TestLogSink());
            var service = new SubStateService(
                typeof(TestStateC),
                Array.Empty<IStateTransition>(),
                new StateErrorRouter(bus, logger),
                logger,
                bus);
            var setup = new GameStateMachineTests.ScopeSetup("C", new List<string>(), bus);
            setup.States[typeof(TestStateA)] = new TestStateA();
            setup.States[typeof(TestStateB)] = new TestStateB();
            service.Bind(new GameStateMachineTests.TestScope(typeof(TestStateC), setup, false, null, service));
            return service;
        }
    }
}
