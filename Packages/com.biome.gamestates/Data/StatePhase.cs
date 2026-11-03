namespace Biome.GameStates.Data
{
    /// <summary>The phase of a state's life, or of a switch, in which a failure can happen.</summary>
    public enum StatePhase
    {
        /// <summary>The state is being initialised.</summary>
        Init,

        /// <summary>The state is loading its content.</summary>
        Load,

        /// <summary>The state is starting.</summary>
        Start,

        /// <summary>The state is stopping.</summary>
        Stop,

        /// <summary>The state is unloading its content.</summary>
        Unload,

        /// <summary>The state is being disposed.</summary>
        Dispose,

        /// <summary>A transition step between two states is running.</summary>
        Transition,
    }
}
