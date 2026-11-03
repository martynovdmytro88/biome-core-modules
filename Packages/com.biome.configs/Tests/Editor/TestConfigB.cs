namespace Biome.Configs.Editor.Tests
{
    /// <summary>A second test-local config payload.</summary>
    public sealed class TestConfigB
    {
        public TestConfigB(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
