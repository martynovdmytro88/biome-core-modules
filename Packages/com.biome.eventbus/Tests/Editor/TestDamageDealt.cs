namespace Biome.EventBus.Editor.Tests
{
    /// <summary>A test-local fact; the module never names a payload type.</summary>
    public readonly struct TestDamageDealt
    {
        public readonly int Amount;

        public TestDamageDealt(int amount)
        {
            Amount = amount;
        }
    }
}
