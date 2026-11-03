using System;
using Biome.GameStates.Abstract;
using Biome.GameStates.Data;

namespace Biome.GameStates
{
    /// <summary>Writes the switch and failure lines; base states and sub-states go under distinct tags.</summary>
    internal sealed class StateSwitchLogger
    {
        internal const string BaseTag = "[State]";
        internal const string SubTag = "[SubState]";

        private readonly IStateLogSink _sink;

        public StateSwitchLogger(IStateLogSink sink)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void BaseSwitched(Type from, Type to)
        {
            _sink.Info($"{BaseTag} {NameOf(from)} -> {NameOf(to)}");
        }

        public void SubSwitched(Type owner, Type from, Type to)
        {
            _sink.Info($"{SubTag} {NameOf(owner)}: {NameOf(from)} -> {NameOf(to)}");
        }

        public void Failed(StateFailure failure)
        {
            _sink.Error($"{TagOf(failure.Level)} {NameOf(failure.State)} failed in {failure.Phase}: {failure.Exception}");
        }

        public void HandlerFailed(StateFailure failure, Exception handlerException)
        {
            _sink.Error($"{TagOf(failure.Level)} the scenario handler failed on {NameOf(failure.State)} / {failure.Phase}: {handlerException}");
        }

        public void CleanupFailed(Type state, Exception exception)
        {
            _sink.Error($"{BaseTag} cleanup of {NameOf(state)} failed: {exception}");
        }

        private static string TagOf(StateLevel level)
        {
            return level == StateLevel.Base ? BaseTag : SubTag;
        }

        private static string NameOf(Type state)
        {
            return state == null ? "none" : state.Name;
        }
    }
}
