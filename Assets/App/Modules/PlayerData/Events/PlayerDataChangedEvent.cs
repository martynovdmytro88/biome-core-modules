namespace Biome.PlayerData.Events
{
    /// <summary>
    /// The stored player-data record has changed. Published once the new record is persisted; carries no payload,
    /// a reader asks <c>IPlayerDataService</c> for the state it needs.
    /// </summary>
    public readonly struct PlayerDataChangedEvent
    {
    }
}
