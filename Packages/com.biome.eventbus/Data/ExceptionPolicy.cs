namespace Biome.EventBus.Data
{
    /// <summary>What the bus does when a listener throws.</summary>
    public enum ExceptionPolicy
    {
        /// <summary>Every listener call is guarded: the others still receive the event, the exception goes to the error sink.</summary>
        Isolate = 0,

        /// <summary>Fail-fast: the first listener exception propagates out of Publish.</summary>
        Rethrow = 1,
    }
}
