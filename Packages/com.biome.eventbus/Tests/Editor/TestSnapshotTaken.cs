namespace Biome.EventBus.Editor.Tests
{
    /// <summary>A ~64-byte payload: the <c>in</c> path must carry it intact through the bus.</summary>
    public readonly struct TestSnapshotTaken
    {
        public readonly long A;
        public readonly long B;
        public readonly long C;
        public readonly long D;
        public readonly long E;
        public readonly long F;
        public readonly long G;
        public readonly long H;

        public TestSnapshotTaken(long seed)
        {
            A = seed;
            B = seed + 1;
            C = seed + 2;
            D = seed + 3;
            E = seed + 4;
            F = seed + 5;
            G = seed + 6;
            H = seed + 7;
        }
    }
}
