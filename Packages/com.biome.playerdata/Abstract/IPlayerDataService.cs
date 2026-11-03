namespace Biome.PlayerData.Abstract
{
    /// <summary>
    /// The player's persistent facts. Main thread only. v0 carries one fact: whether the player has opened the game
    /// before.
    /// </summary>
    public interface IPlayerDataService
    {
        /// <summary>
        /// Whether this session is the player's first launch. A session snapshot: read once when the service is
        /// created and unchanged for the whole session, even after <see cref="MarkLaunched"/>. A missing, corrupt or
        /// version-mismatched record reads as a first launch.
        /// </summary>
        bool IsFirstLaunch { get; }

        /// <summary>
        /// Records that the player has launched the game and flushes the store. A no-op when the record already says
        /// so. Does not change <see cref="IsFirstLaunch"/> in the current session.
        /// </summary>
        void MarkLaunched();
    }
}
