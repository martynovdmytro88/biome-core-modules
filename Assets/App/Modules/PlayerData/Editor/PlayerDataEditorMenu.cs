using Biome.LocalStore;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Config;
using Biome.PlayerData.Data;
using UnityEditor;
using UnityEngine;

namespace Biome.PlayerData.Editor
{
    /// <summary>
    /// Editor menu for PlayerData. A running Play-mode session keeps its snapshot; the reset shows on the next launch.
    /// </summary>
    public static class PlayerDataEditorMenu
    {
        [MenuItem("Biome/PlayerData/ResetToDefaults")]
        public static void ResetToDefaults()
        {
            var registry = new DictionaryLocalStoreRegistry()
                .Add<PlayerDataRecord>(PlayerDataStoreEntry.Key, PlayerDataStoreEntry.Version);
            ILocalStoreService store = LocalStoreFactory.CreatePlayerPrefs(registry, LocalStoreLimits.Placeholder);

            store.Remove<PlayerDataRecord>();
            store.Flush();

            Debug.Log("PlayerData reset: the next launch reads as first launch.");
        }
    }
}
