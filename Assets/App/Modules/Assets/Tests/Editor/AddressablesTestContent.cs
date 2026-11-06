using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.TestTools;

namespace Biome.Assets.Tests.Editor
{
    /// <summary>
    /// The PlayMode prebuild and cleanup of the Addressables fixtures. The prebuild puts the smoke content of
    /// <c>Tests/Content/</c> in a temporary group: the two text assets at <c>assets-tests/a|b</c> under the label
    /// <c>assets-tests</c>, the empty scene at <c>assets-tests/scene</c>; and it selects the Use Asset Database play mode.
    /// The cleanup removes the group and the label and restores the play mode; settings the prebuild had to create are
    /// deleted again. The content is found by GUID, so the step does not depend on where the module sits.
    /// </summary>
    public sealed class AddressablesTestContent : IPrebuildSetup, IPostBuildCleanup
    {
        private const string GroupName = "Biome Assets Tests";
        private const string Label = "assets-tests";
        private const string SmokeAGuid = "30300dac15ee04656948a8392c95c8ac";
        private const string SmokeBGuid = "b02b9d1bb767e464188f307a7c781ca5";
        private const string SmokeSceneGuid = "a77722931bce40689e10c6958fd8f5c9";
        private const int UseAssetDatabaseIndex = 0;
        private const string PlayModeIndexKey = "Biome.Assets.Tests.Editor.PlayModeIndex";
        private const string CreatedSettingsKey = "Biome.Assets.Tests.Editor.CreatedSettings";

        public void Setup()
        {
            bool settingsExisted = AddressableAssetSettingsDefaultObject.SettingsExists;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group == null)
            {
                // The state to restore is taken only by the call that creates the group, so a repeated prebuild keeps it.
                SessionState.SetInt(PlayModeIndexKey, settings.ActivePlayModeDataBuilderIndex);
                SessionState.SetBool(CreatedSettingsKey, !settingsExisted);
                // No schema: Use Asset Database mode reads the entries as they are.
                group = settings.CreateGroup(GroupName, false, false, false, null);
            }

            Mark(settings, group, SmokeAGuid, "assets-tests/a", true);
            Mark(settings, group, SmokeBGuid, "assets-tests/b", true);
            Mark(settings, group, SmokeSceneGuid, "assets-tests/scene", false);
            settings.ActivePlayModeDataBuilderIndex = UseAssetDatabaseIndex;
            AssetDatabase.SaveAssets();
        }

        public void Cleanup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group != null)
            {
                settings.RemoveGroup(group);
            }

            settings.RemoveLabel(Label);
            settings.ActivePlayModeDataBuilderIndex = SessionState.GetInt(PlayModeIndexKey, settings.ActivePlayModeDataBuilderIndex);
            if (SessionState.GetBool(CreatedSettingsKey, false))
            {
                string folder = settings.ConfigFolder;
                EditorBuildSettings.RemoveConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName);
                AssetDatabase.DeleteAsset(folder);
            }

            AssetDatabase.SaveAssets();
            SessionState.EraseInt(PlayModeIndexKey);
            SessionState.EraseBool(CreatedSettingsKey);
        }

        private static void Mark(AddressableAssetSettings settings, AddressableAssetGroup group, string guid, string address, bool labelled)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
            {
                throw new InvalidOperationException($"The smoke asset {guid} of the Assets module tests is missing.");
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            if (labelled)
            {
                entry.SetLabel(Label, true, true);
            }
        }
    }
}
