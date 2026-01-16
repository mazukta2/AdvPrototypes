using System;
using System.Collections.Generic;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    [DefaultExecutionOrder(-32000)]
    internal class CustomBindTypesAsset : ScriptableObject
    {
        private static CustomBindTypesAsset _instance;
        internal static CustomBindTypesAsset Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                _instance = Resources.Load<CustomBindTypesAsset>("bind-types");

                return _instance;
            }
            set => _instance = value;
        }
        
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAllTypes()
        {
            Instance?.RegisterAll();
        }
        
        [SerializeField]
        // TODO: Add is safe flag for custom converters
        internal List<SerializedType> customConverters = new();
        
        [SerializeField]
        // TODO: Add templates and icons for custom modifiers
        internal List<SerializedType> customModifiers = new();
        
        [SerializeField]
        internal List<SerializedType> customAccessorProviders = new();

        [SerializeField]
        [HideInInspector]
        internal List<SerializedType> removedTypes = new();
        
        [NonSerialized]
        private bool _initialized;

        private void OnEnable()
        {
            RegisterAll();
        }

        internal void RegisterAll()
        {
            if (_initialized) return;
            
            _initialized = true;
            
            RegisterConverters();
            RegisterModifiers();
            RegisterProviders();
        }

        private void OnValidate()
        {
            _initialized = false;
        }

        internal void RegisterConverters()
        {
            foreach (var converterType in customConverters)
            {
                if (removedTypes.Contains(converterType))
                {
                    continue;
                }

                try
                {
                    ConvertersFactory.RegisterTemplate(converterType);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception(BindSystem.DebugPrefix + $"Error while registering converter {converterType.Name}: {ex.Message}", ex));
                }
            }
        }
        
        internal void RegisterModifiers()
        {
            foreach (var modifierType in customModifiers)
            {
                if (removedTypes.Contains(modifierType))
                {
                    continue;
                }

                try
                {
                    ModifiersFactory.RegisterTemplate(modifierType);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception(BindSystem.DebugPrefix + $"Error while registering modifier {modifierType.Name}: {ex.Message}", ex));
                }
            }
        }

        internal void RegisterProviders()
        {
            foreach (var providerType in customAccessorProviders)
            {
                if (removedTypes.Contains(providerType))
                {
                    continue;
                }

                try
                {
                    var provider = Activator.CreateInstance(providerType) as IAccessorProvider;
                    AccessorsFactory.RegisterAccessorProvider(provider);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception(BindSystem.DebugPrefix + $"Error while registering provider {providerType.Name}: {ex.Message}", ex));
                }
            }
        }
    }
}
