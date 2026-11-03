using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;

namespace Biome.Utils.Commands.Tests
{
    /// <summary>Structured input for the parameter probe (advertised as a nested object schema).</summary>
    public sealed class ProbeArgs : IStructuredCommandInput
    {
        [CliArg("label", "A label")] public string Label { get; set; }
        [CliArg("count", "A count")] public int Count { get; set; }
        [CliArg("items", "Nested string array")] public string[] Items { get; set; }
    }

    /// <summary>
    /// Test-kind callables that prove the callable surface: arrays and structured parameters through
    /// <c>[CliCommand]</c> and through <c>invoke</c>. They stay in the Tests asmdef (compiled with the test
    /// framework only) and are the fixture of the scanner / invoke tests.
    /// </summary>
    public static class ProbeCallables
    {
        /// <summary>
        /// Echoes an array and a structured argument back, so the JSON→C# mapping of both can be read off the payload.
        /// </summary>
        /// <param name="tags">A string array.</param>
        /// <param name="args">A structured argument.</param>
        [BiomeCallable(CallableKind.Test)]
        [CliCommand("biome_probe_params", "Test probe: echo an array and a structured argument", Tags = new[] { "biome/test" })]
        public static BiomeEnvelope ProbeParams(
            [CliArg("tags", "String array")] string[] tags = null,
            [CliArg("args", "Structured argument")] ProbeArgs args = null)
        {
            return BiomeEnvelope.Ok("biome_probe_params", "echo", new
            {
                tags = tags ?? new string[0],
                tagCount = tags?.Length ?? 0,
                label = args?.Label,
                count = args?.Count ?? -1,
                items = args?.Items ?? new string[0],
            });
        }

        // no /// on purpose — the scanner must report it as no-doc
        [BiomeCallable(CallableKind.Test)]
        public static int ProbeUndocumented(int a, int b = 2) => a + b;

        /// <summary>Throws on purpose — the envelope must report <c>exception</c>.</summary>
        [BiomeCallable(CallableKind.Test)]
        public static string ProbeThrows(string why) => throw new System.InvalidOperationException(why);

        /// <summary>Sums a list — plain return value wrapped by invoke.</summary>
        [BiomeCallable(CallableKind.Test)]
        public static int ProbeSum(List<int> values) => values.Sum();
    }
}
