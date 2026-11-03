using System;
using Biome.EventBus.Abstract;
using Biome.LocalStore.Abstract;
using Biome.PlayerData.Abstract;
using Biome.PlayerData.Data;
using Biome.PlayerData.Events;

namespace Biome.PlayerData
{
    /// <summary>
    /// The production <see cref="IPlayerDataService"/> over LocalStore. Reads the record once at construction; a
    /// missing, corrupt or version-mismatched record reads as a first launch. Publishes
    /// <see cref="PlayerDataChangedEvent"/> after the record actually changes. Main thread only.
    /// </summary>
    public sealed class PlayerDataService : IPlayerDataService
    {
        private readonly ILocalStoreService _store;
        private readonly IEventPublisher _events;
        private bool _hasLaunched;

        /// <exception cref="ArgumentNullException"><paramref name="store"/> or <paramref name="events"/> is null.</exception>
        public PlayerDataService(ILocalStoreService store, IEventPublisher events)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _events = events ?? throw new ArgumentNullException(nameof(events));

            _hasLaunched = _store.TryGet(out PlayerDataRecord record, out _) && record.HasLaunched;
            IsFirstLaunch = !_hasLaunched;
            UnityEngine.Debug.Log($"IsFirstLaunch: {IsFirstLaunch}");
        }

        /// <inheritdoc />
        public bool IsFirstLaunch { get; }

        /// <inheritdoc />
        public void MarkLaunched()
        {
            if (_hasLaunched)
            {
                return;
            }

            _store.Set(new PlayerDataRecord { HasLaunched = true });
            _store.Flush();
            _hasLaunched = true;
            _events.Publish(new PlayerDataChangedEvent());
        }
    }
}
