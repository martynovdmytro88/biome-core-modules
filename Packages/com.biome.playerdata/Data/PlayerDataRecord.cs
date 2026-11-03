namespace Biome.PlayerData.Data
{
    /// <summary>
    /// The one growing record of the player's persistent facts, stored at version 1. A new field widens this record
    /// under a version bump; the previous shape stays frozen as <c>PlayerDataRecordV&lt;n&gt;</c> under the same key.
    /// Written only by the PlayerData service.
    /// </summary>
    public struct PlayerDataRecord
    {
        /// <summary>Whether the player has opened the game before.</summary>
        public bool HasLaunched { get; set; }
    }
}
