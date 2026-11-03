using System;
using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Fakes
{
    /// <summary>
    /// A recording <see cref="IGameState"/>. The machine keys states by type, so a test derives one class per state it
    /// needs. It counts calls, writes them to an optional shared journal, can hold the entry at a gate and can fail.
    /// </summary>
    public class FakeGameState : IGameState
    {
        private readonly string _name;
        private readonly IList<string> _journal;

        /// <summary>Creates the fake.</summary>
        /// <param name="name">The name written to the journal; the type name when null.</param>
        /// <param name="journal">The shared journal of calls ("Name.Enter" / "Name.Exit"), or null.</param>
        public FakeGameState(string name = null, IList<string> journal = null)
        {
            _name = name ?? GetType().Name;
            _journal = journal;
        }

        /// <summary>How many times <see cref="EnterAsync"/> was called.</summary>
        public int EnterCount { get; private set; }

        /// <summary>How many times <see cref="ExitAsync"/> was called.</summary>
        public int ExitCount { get; private set; }

        /// <summary>When set, the entry waits until the source completes.</summary>
        public UniTaskCompletionSource EnterGate { get; set; }

        /// <summary>When set, the entry throws it.</summary>
        public Exception EnterFailure { get; set; }

        /// <summary>When set, the exit throws it.</summary>
        public Exception ExitFailure { get; set; }

        /// <inheritdoc />
        public async UniTask EnterAsync(CancellationToken ct)
        {
            EnterCount++;
            _journal?.Add(_name + ".Enter");
            if (EnterGate != null)
            {
                await EnterGate.Task;
            }

            ct.ThrowIfCancellationRequested();
            if (EnterFailure != null)
            {
                throw EnterFailure;
            }
        }

        /// <inheritdoc />
        public UniTask ExitAsync(CancellationToken ct)
        {
            ExitCount++;
            _journal?.Add(_name + ".Exit");
            if (ExitFailure != null)
            {
                throw ExitFailure;
            }

            return UniTask.CompletedTask;
        }
    }
}
