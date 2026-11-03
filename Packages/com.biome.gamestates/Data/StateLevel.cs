namespace Biome.GameStates.Data
{
    /// <summary>The level a state lives on; the only thing that tells a base state from a sub-state.</summary>
    public enum StateLevel
    {
        /// <summary>A base state, switched by the state machine.</summary>
        Base,

        /// <summary>A sub-state, switched by the sub-state service of its base state.</summary>
        Sub,
    }
}
