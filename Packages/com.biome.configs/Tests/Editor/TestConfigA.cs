namespace Biome.Configs.Editor.Tests
{
    /// <summary>Test-local config payload; the module never names a consumer's data.</summary>
    public sealed class TestConfigA
    {
        public TestConfigA(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
