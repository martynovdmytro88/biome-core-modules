using Biome.LocalStore.Abstract;

namespace Biome.PlayerData
{
    /// <summary>
    /// PlayerData's contribution to the LocalStore registry: the one key and version under which
    /// <c>PlayerDataRecord</c> is stored. The composer and the Editor menu take it from here; no key literal lives
    /// anywhere else.
    /// </summary>
    public static class PlayerDataStoreEntry
    {
        /// <summary>The storage key of the player-data record, without the module prefix.</summary>
        public const string Key = "player-data";

        /// <summary>The version of the record shape stored under <see cref="Key"/>.</summary>
        public const int Version = 1;

        /// <summary>The registry entry of the player-data record.</summary>
        public static LocalStoreEntry Entry => new LocalStoreEntry(Key, Version);
    }
}
