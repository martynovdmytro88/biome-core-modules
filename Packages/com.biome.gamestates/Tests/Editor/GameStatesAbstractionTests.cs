using System;
using System.Linq;
using System.Reflection;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;
using NUnit.Framework;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>Shape and reference checks of the GameStates contracts, and the value semantics of <see cref="StateFailure"/>.</summary>
    [TestFixture]
    public sealed class GameStatesAbstractionTests
    {
        private static readonly string[] s_forbiddenReferences =
        {
            "UnityEngine",
            "VContainer",
            "Biome.GameStates.Config",
        };

        [Test]
        public void AbstractAssembly_References_OnlyDataAndUniTask()
        {
            string[] names = typeof(IGameState).Assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name.StartsWith("Biome.", StringComparison.Ordinal) || name.StartsWith("UnityEngine", StringComparison.Ordinal))
                .ToArray();

            Assert.That(names, Is.EquivalentTo(new[] { "Biome.GameStates.Data" }));
        }

        [Test]
        public void ContractAssemblies_Reference_NoForbiddenAssembly()
        {
            Assembly[] assemblies = { typeof(IGameState).Assembly, typeof(StateLevel).Assembly };

            foreach (Assembly assembly in assemblies)
            {
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    Assert.That(s_forbiddenReferences.Any(forbidden => reference.Name.StartsWith(forbidden, StringComparison.Ordinal)), Is.False,
                        assembly.GetName().Name + " references " + reference.Name);
                }
            }
        }

        [Test]
        public void StateFailure_Shape_IsAReadonlyStruct()
        {
            Assert.That(typeof(StateFailure).IsValueType, Is.True);
            Assert.That(typeof(StateFailure).GetCustomAttributes().Any(attribute => attribute.GetType().Name == "IsReadOnlyAttribute"), Is.True);
        }

        [Test]
        public void StateFailure_Create_CarriesEveryField()
        {
            var exception = new InvalidOperationException("boom");

            var failure = new StateFailure(typeof(string), StateLevel.Sub, StatePhase.Load, exception);

            Assert.That(failure.State, Is.EqualTo(typeof(string)));
            Assert.That(failure.Level, Is.EqualTo(StateLevel.Sub));
            Assert.That(failure.Phase, Is.EqualTo(StatePhase.Load));
            Assert.That(failure.Exception, Is.SameAs(exception));
        }

        [Test]
        public void StateFailure_Equality_ComparesAllFields()
        {
            var exception = new InvalidOperationException("boom");
            var a = new StateFailure(typeof(string), StateLevel.Base, StatePhase.Start, exception);
            var b = new StateFailure(typeof(string), StateLevel.Base, StatePhase.Start, exception);
            var c = new StateFailure(typeof(string), StateLevel.Sub, StatePhase.Start, exception);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(c));
        }

        [Test]
        public void Enums_Members_MatchTheDesign()
        {
            Assert.That(Enum.GetNames(typeof(StateLevel)), Is.EqualTo(new[] { "Base", "Sub" }));
            Assert.That(
                Enum.GetNames(typeof(StatePhase)),
                Is.EqualTo(new[] { "Init", "Load", "Start", "Stop", "Unload", "Dispose", "Transition" }));
        }

        [Test]
        public void SubStateService_SwitchAsync_MatchesMachineShape()
        {
            MethodInfo machine = typeof(IGameStateMachine).GetMethod(nameof(IGameStateMachine.SwitchAsync));
            MethodInfo service = typeof(ISubStateService).GetMethod(nameof(ISubStateService.SwitchAsync));

            Assert.That(service.ReturnType, Is.EqualTo(machine.ReturnType));
            Assert.That(service.GetParameters().Select(parameter => parameter.ParameterType), Is.EqualTo(machine.GetParameters().Select(parameter => parameter.ParameterType)));
            Assert.That(service.GetGenericArguments()[0].GetGenericParameterConstraints(), Is.EqualTo(new[] { typeof(IGameState) }));
        }
    }
}
