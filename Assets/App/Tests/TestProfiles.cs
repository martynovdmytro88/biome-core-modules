using System;
using VContainer;
using VContainer.Unity;

namespace Biome.App.Tests
{
    /// <summary>Counts its own disposals — registered as a type so the container that built it owns it.</summary>
    internal sealed class SpyDisposable : IDisposable
    {
        internal int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    /// <summary>An entry point that counts the frames it was ticked in.</summary>
    internal sealed class TickCounter : ITickable
    {
        internal int Ticks { get; private set; }

        public void Tick() => Ticks++;
    }

    /// <summary>Installs whatever the test hands it — the child-scope installer of the dispose-cascade tests.</summary>
    internal static class TestInstallers
    {
        internal static IInstaller SpyAndTicker() => new ActionInstaller(builder =>
        {
            builder.Register<SpyDisposable>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TickCounter>().AsSelf();
        });
    }
}
