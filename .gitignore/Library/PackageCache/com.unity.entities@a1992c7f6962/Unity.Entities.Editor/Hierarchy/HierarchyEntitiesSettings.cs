using System;
using JetBrains.Annotations;
using Unity.Editor.Bridge;
using Unity.Entities.UI;
using Unity.Properties;
using UnityEditor;

namespace Unity.Entities.Editor
{
    [Flags]
    internal enum HierarchyWorldFilter
    {
        None       = 0,
        Live       = 1,
        Conversion = 1 << 4,
    }

    [DOTSEditorPreferencesSetting(Constants.Settings.Hierarchy), UsedImplicitly]
    internal class HierarchyEntitiesSettings : ISetting
    {
        public bool ShowHiddenEntities = false;
        [DisplayName("Type Of Worlds Shown")]
        public HierarchyWorldFilter WorldFilter = HierarchyWorldFilter.Live;

        public static bool GetShowHiddenEntities() => GetBoolValue(nameof(ShowHiddenEntities));
        public static void SetShowHiddenEntities(bool val) => SetBoolValue(nameof(ShowHiddenEntities), val);

        public static WorldFlags GetTypesOfWorldsShown()
        {
            var val = GetIntValue(nameof(WorldFilter));
            if (val != int.MinValue)
                return ToWorldFlags((HierarchyWorldFilter)val);
            return WorldFlags.Live;
        }

        public static void SetTypesOfWorldsShown(HierarchyWorldFilter val) => SetIntValue(nameof(WorldFilter), (int)val);

        static WorldFlags ToWorldFlags(HierarchyWorldFilter filter)
        {
            var flags = WorldFlags.None;
            if ((filter & HierarchyWorldFilter.Live) != 0)       flags |= WorldFlags.Live;
            if ((filter & HierarchyWorldFilter.Conversion) != 0) flags |= WorldFlags.Conversion;
            return flags;
        }

        static bool GetBoolValue(string key)
        {
            var setting = EditorUserSettings.GetConfigValue(key);
            return !string.IsNullOrEmpty(setting) && Convert.ToBoolean(setting);
        }

        static void SetBoolValue(string key, bool val)
        {
            EditorUserSettings.SetConfigValue(key, val ? "true" : "false");
        }
        
        static int GetIntValue(string key)
        {
            var setting = EditorUserSettings.GetConfigValue(key);
            return !string.IsNullOrEmpty(setting) ? Convert.ToInt32(setting) : int.MinValue;
        }
        
        static void SetIntValue(string key, int val)
        {
            EditorUserSettings.SetConfigValue(key, val.ToString());
        }        
        
        public string[] GetSearchKeywords() => ISetting.GetSearchKeywordsFromType(GetType());

        public void OnSettingChanged(PropertyPath path)
        {
            switch (path.ToString())
            {
                case nameof(ShowHiddenEntities):
                    ShowHiddenEntities = !GetShowHiddenEntities();
                    SetShowHiddenEntities(ShowHiddenEntities);
                    break;
                case nameof(WorldFilter):
                    SetTypesOfWorldsShown(WorldFilter);
                    break;                
            }

            var window = EditorWindow.GetWindow<Unity.Hierarchy.Editor.HierarchyWindow>();
            window.ReloadHostView();
        }
    }
}
