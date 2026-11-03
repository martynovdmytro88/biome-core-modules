using System;
using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Fakes
{
    /// <summary>
    /// A test machine with one state, <see cref="MockGameState"/>. A switch only records the requested state type; no
    /// scope, scene or queue is involved. Hand it to <c>GameStatesInstallerMock</c> to give consumers a machine.
    /// </summary>
    public sealed class MockGameStateMachine : IGameStateMachine
    {
        private readonly List<Type> _switches = new List<Type>();

        /// <summary>The only state of this machine.</summary>
        public MockGameState State { get; } = new MockGameState();

        /// <summary>The state types requested so far, in order.</summary>
        public IReadOnlyList<Type> Switches => _switches;

        /// <inheritdoc />
        public Type Current { get; private set; }

        /// <inheritdoc />
        public UniTask SwitchAsync<TState>(CancellationToken ct)
            where TState : IGameState
        {
            Current = typeof(TState);
            _switches.Add(Current);
            return UniTask.CompletedTask;
        }
    }
}
