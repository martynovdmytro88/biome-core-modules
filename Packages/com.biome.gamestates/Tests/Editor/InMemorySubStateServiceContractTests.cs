using System;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>
    /// Runs the sub-state contract on a test-local double: the package ships no fake of the service, so this proves the
    /// suite itself. A real service is proven only when a host binds it.
    /// </summary>
    [TestFixture]
    public sealed class InMemorySubStateServiceContractTests : SubStateServiceContractTests
    {
        protected override ISubStateService CreateService() => new InMemorySubStateService();

        private sealed class InMemorySubStateService : ISubStateService
        {
            public Type Current { get; private set; }

            public UniTask SwitchAsync<TSubState>(CancellationToken ct)
                where TSubState : IGameState
            {
                Current = typeof(TSubState);
                return UniTask.CompletedTask;
            }
        }
    }
}
