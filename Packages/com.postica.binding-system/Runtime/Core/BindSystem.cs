
using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// The main class of the Binding System to access important information.
    /// </summary>
    public static partial class BindSystem
    {
        private static BindMetaValues _metaValues;
        
        /// <summary>
        /// Represents the current version of the Binding System with the format "Major.Minor.Patch".
        /// </summary>
        public const string Version = "2.5.2";
        
        /// <summary>
        /// Represents the name of the Binding System.
        /// </summary>
        public const string ProductName = "Binding System";
        
        /// <summary>
        /// Represents the full product text with the version.
        /// </summary>
        public const string FullProductText = ProductName + " v" + Version;
        
        /// <summary>
        /// Used to prefix debug messages.
        /// </summary>
        internal const string DebugPrefix = "[" + FullProductText + "] - ";
        
        /// <summary>
        /// True if the current architecture is ARM64, false otherwise.
        /// </summary>
        public static bool IsARM64Architecture => SystemInfo.processorType.Contains("ARM", StringComparison.OrdinalIgnoreCase) ||
                                                  SystemInfo.processorType.Contains("Apple", StringComparison.OrdinalIgnoreCase);
        
        internal static BindMetaValues MetaValues
        {
            get
            {
                if (_metaValues) return _metaValues;
                
                _metaValues = Resources.Load<BindMetaValues>("bind-meta-values");
                
                if (!_metaValues)
                {
                    _metaValues = ScriptableObject.CreateInstance<BindMetaValues>();
                }
                else
                {
                    _metaValues.Sanitize();
                }

                return _metaValues;
            }
            set => _metaValues = value;
        }

        public static bool RerouteBoundField(Type type, string fieldName, string newFieldOrProperty,
            bool overwrite = false)
        {
            return ReflectionExtensions.RerouteFieldPath(type, fieldName, newFieldOrProperty, overwrite);
        }
        
        public static bool RerouteBoundFieldOf<T>(string fieldName, string newFieldOrProperty, bool overwrite = false)
        {
            return ReflectionExtensions.RerouteFieldPath(typeof(T), fieldName, newFieldOrProperty, overwrite);
        }
        
        public static bool UnrouteBoundField(Type type, string fieldName)
        {
            return ReflectionExtensions.UnRerouteFieldPath(type, fieldName);
        }

        /// <summary>
        /// The options used by the Binding System. These are stored in the MetaValues asset.
        /// </summary>
        internal static class Options
        {
            private const string OptionsKey = "options.";
            
            public static event Action OnOptionsChanged;

            public static bool UsePhasedUpdates
            {
                get => GetOption(nameof(UsePhasedUpdates), true);
                set => SetOption(nameof(UsePhasedUpdates), value);
            }
            
            private static T GetOption<T>(string name, T defaultValue)
            {
                return MetaValues.GetValue(OptionsKey + name, defaultValue);
            }
            
            private static void SetOption<T>(string name, T value)
            {
                if(MetaValues.TrySetValue(OptionsKey + name, value))
                {
                    OnOptionsChanged?.Invoke();
                }
            }
        }
    }
}
