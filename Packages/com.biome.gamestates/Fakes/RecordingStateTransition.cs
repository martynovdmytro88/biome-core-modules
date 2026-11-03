using System;
using System.Collections.Generic;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;

namespace Biome.GameStates.Fakes
{
    /// <summary>A recording <see cref="IStateTransition"/>: remembers every run and can be told which pairs it applies to or fail.</summary>
    public sealed class RecordingStateTransition : IStateTransition
    {
        private readonly string _name;
        private readonly IList<string> _journal;
        private readonly Func<Type, Type, bool> _applies;
        private readonly List<(Type From, Type To)> _runs = new List<(Type From, Type To)>();

        /// <summary>Creates the fake.</summary>
        /// <param name="name">The name written to the journal as "Name.Run".</param>
        /// <param name="journal">The shared journal of calls, or null.</param>
        /// <param name="applies">Tells which pairs it applies to; every pair when null.</param>
        public RecordingStateTransition(string name = "Transition", IList<string> journal = null, Func<Type, Type, bool> applies = null)
        {
            _name = name;
            _journal = journal;
            _applies = applies;
        }

        /// <summary>The pairs it ran for, in order.</summary>
        public IReadOnlyList<(Type From, Type To)> Runs => _runs;

        /// <summary>When set, the run throws it.</summary>
        public Exception Failure { get; set; }

        /// <inheritdoc />
        public bool Applies(Type from, Type to)
        {
            return _applies == null || _applies(from, to);
        }

        /// <inheritdoc />
        public UniTask RunAsync(Type from, Type to, CancellationToken ct)
        {
            _runs.Add((from, to));
            _journal?.Add(_name + ".Run");
            if (Failure != null)
            {
                throw Failure;
            }

            return UniTask.CompletedTask;
        }
    }
}
