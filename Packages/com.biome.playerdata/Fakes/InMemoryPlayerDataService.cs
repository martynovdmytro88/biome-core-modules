using Biome.PlayerData.Abstract;

namespace Biome.PlayerData.Fakes
{
    /// <summary>
    /// In-memory <see cref="IPlayerDataService"/> for consumers' tests and the mock profile: no store, same snapshot
    /// semantics as production. The initial state is set at construction.
    /// </summary>
    public sealed class InMemoryPlayerDataService : IPlayerDataService
    {
        public InMemoryPlayerDataService(bool hasLaunched = false)
        {
            IsFirstLaunch = !hasLaunched;
        }

        /// <inheritdoc />
        public bool IsFirstLaunch { get; }

        /// <summary>True once <see cref="MarkLaunched"/> has been called on this instance.</summary>
        public bool IsLaunchMarked { get; private set; }

        /// <inheritdoc />
        public void MarkLaunched()
        {
            IsLaunchMarked = true;
        }
    }
}
