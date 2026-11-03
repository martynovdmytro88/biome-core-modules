namespace Biome.EventBus.Editor.Tests
{
    /// <summary>A second test-local fact, for the per-type delivery cases.</summary>
    public readonly struct TestUnitSpawned
    {
        public readonly int UnitId;

        public TestUnitSpawned(int unitId)
        {
            UnitId = unitId;
        }
    }
}
