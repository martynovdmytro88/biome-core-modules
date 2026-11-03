namespace Biome.GameStates
{
    /// <summary>The seam to the log: the machine writes tagged lines, the host decides where they go.</summary>
    public interface IStateLogSink
    {
        /// <summary>Writes an informational line (a switch).</summary>
        /// <param name="message">The tagged line.</param>
        void Info(string message);

        /// <summary>Writes an error line (a failure).</summary>
        /// <param name="message">The tagged line.</param>
        void Error(string message);
    }
}
