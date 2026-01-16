using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem
{
    [Serializable]
    class BindingSettings
    {
        const string k_BindingSettingsPath = "ProjectSettings/Packages/com.postica.binding-system/settings.json";

        [OnBindSystemUpgrade]
        private static void Upgrade(string fromVersion)
        {
            if(string.Compare(fromVersion, "2.0.3", StringComparison.Ordinal) <= 0)
            {
                Current.autoRegisterConverters = true;
                Current.Save();
                Optimizer.RunAutoRegistrations(Current);
            } 
        }
        
        private static BindingSettings _instance;

        private static string SettingsLocalPath => BindingSystemIO.BuildPath(k_BindingSettingsPath);

        public static BindingSettings Current
        {
            get
            {
                if (_instance == null)
                {
                    try
                    {
                        var json = File.ReadAllText(SettingsLocalPath);
                        _instance = JsonUtility.FromJson<BindingSettings>(json);
                    }
                    catch (Exception)
                    {
                        _instance = new BindingSettings();
                        _instance.Save();
                    }
                }

                return _instance;
            }
        }

        [SerializeField] bool autoRegisterConverters = true;
        [SerializeField] bool autoRegisterModifiers = true;
        [SerializeField] bool autoRegisterProviders = true;
        [SerializeField] bool autoConvertObjects = false;
        [SerializeField] int maxBindPathDepth = 3;

        [SerializeField] bool showTargetsReplacement = true;
        [SerializeField] bool realtimeDebug;
        [SerializeField] bool showProxyBindings;

        [SerializeField] bool showLastUsedSources = true;
        [SerializeField] bool showLastUsedPins = true;
        
        [SerializeField] bool enableRefactoring = true;
        [SerializeField] bool preferRenamingAutoFix = true;
        [SerializeField] bool enableUnityClassesRefactoring = false;
        
        [SerializeField] bool addChildrenToDependenciesSearch = true;
        
        [SerializeField] bool performanceMode = false;
        
        [SerializeField] BindData defaultBindData = new(BindData.BitFlags.UpdateOnUpdate | BindData.BitFlags.OptimizeUpdate)
        {
            Mode = BindMode.Read
        };

        public bool AutoRegisterConverters
        {
            get => autoRegisterConverters;
            set => Set(ref autoConvertObjects, value);
        }

        public bool AutoRegisterModifiers
        {
            get => autoRegisterModifiers;
            set => Set(ref autoRegisterModifiers, value);
        }

        public bool AutoRegisterProviders
        {
            get => autoRegisterProviders;
            set => Set(ref autoRegisterProviders, value);
        }

        public bool ShowImplicitConverters => false;
        public bool ShowIncompatibleModifiers => false;

        [Tooltip("<b><color=#44ff44>Requires Minimal UI</color></b>\n" +
                 "Target group replacement is a feature that allows to replace a source object in bind fields with another one.\n" +
                 "This feature usually pops up in headers and group foldouts.\n" +
                 "This feature may add a small overhead when initializing the UI, so consider it carefully if there are slowdowns in UI initialization.\n" +
                 "Enable this option to show the replacement control.")]
        public bool ShowTargetGroupReplacement
        {
            get => showTargetsReplacement;
            set => Set(ref showTargetsReplacement, value);
        }
        
        [Tooltip("<b><color=#44ff44>Requires Minimal UI</color></b>\n" +
                 "If true, the debug information will be updated at 60Hz in the Inspector.\n" +
                 "If false, the debug information will be updated only when the Inspector is repainted.")]
        public bool RealtimeDebug
        {
            get => realtimeDebug;
            set => Set(ref realtimeDebug, value);
        }

        [Tooltip("<b><color=#44ff44>Requires UI Toolkit enabled in Inspector</color></b>\n" +
                 "If true, the GameObject proxy bindings and Scriptable Objects proxy bindings will be visible but not editable.")]
        public bool ShowProxyBindings
        {
            get => showProxyBindings;
            set => Set(ref showProxyBindings, value);
        }

        public bool AutoFixSerializationUpgrade
        {
            get => autoConvertObjects;
            set => Set(ref autoConvertObjects, value);
        }

        public bool UsePhasedBinding
        {
            get => BindSystem.Options.UsePhasedUpdates;
            set => BindSystem.Options.UsePhasedUpdates = value;
        }
        
        [Tooltip("The maximum depth of the bind path. If the path is longer than this value, the binding menu will not show it.")]
        public int MaxBindPathDepth
        {
            get => maxBindPathDepth;
            set => Set(ref maxBindPathDepth, value);
        }

        [Tooltip("If true, the last used pins will be shown when binding a field.\n<b>Disable</b> to unclutter the menu.")]
        public bool ShowLastUsedPins
        {
            get => showLastUsedPins;
            set => Set(ref showLastUsedPins, value);
        }

        [Tooltip("If true, the last used sources will be available in the dropdown menu.\n<b>Disable</b> to unclutter the menu.")]
        public bool ShowLastUsedSources
        {
            get => showLastUsedSources;
            set => Set(ref showLastUsedSources, value);
        }

        [Tooltip("If true, the refactoring will provide suggestions to refactor the invalid bindings. Otherwise, all invalid bindings <b>will be removed by default</b>.")]
        public bool EnableRefactoring
        {
            get => enableRefactoring;
            set => Set(ref enableRefactoring, value);
        }
        
        [Tooltip("If true, the refactoring will refactor the Unity classes as well.")]
        public bool EnableUnityClassesRefactoring
        {
            get => enableUnityClassesRefactoring;
            set => Set(ref enableUnityClassesRefactoring, value);
        }
        
        [Tooltip("If true, the refactoring will attempt to automatically find the renamed member and fix it.")]
        public bool PreferRenamingAutoFix
        {
            get => preferRenamingAutoFix;
            set => Set(ref preferRenamingAutoFix, value);
        }
        
        [Tooltip("If true, the children of the GameObjects will be added to the dependencies search.")]
        public bool AddChildrenToDependenciesSearch
        {
            get => addChildrenToDependenciesSearch;
            set => Set(ref addChildrenToDependenciesSearch, value);
        }

        [Tooltip("If true, the performance mode will be enabled. This mode will disable some features that are not needed for performance. " +
                 "This mode is recommended when there are significant slowdowns.")]
        public bool PerformanceMode  
        {
            get => performanceMode;
            set => Set(ref performanceMode, value);
        }

        [Tooltip("The default bind data that will be used when creating a new binding.")]
        public BindData DefaultBindData
        {
            get => defaultBindData;
            set => Set(ref defaultBindData, value);
        }

        private void Set<T>(ref T field, T value)
        {
            if (field.Equals(value)) return;
            field = value;
            Save();
        }

        private void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(SettingsLocalPath, json);
        }
    }
}