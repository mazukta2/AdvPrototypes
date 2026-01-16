using System;
using Postica.Common;
using Postica.BindingSystem.PinningLogic;
using Postica.BindingSystem.ProxyBinding;
using Postica.BindingSystem.Refactoring;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Postica.BindingSystem
{
    [InitializeOnLoad]
    class BindingSystemStartup : AssetPostprocessor
    {
        private static bool _initialized;
        private static bool _fullyInitialized;
        
        static BindingSystemStartup()
        {
            Startup();
        }

        [InitializeOnLoadMethod]
        [DidReloadScripts]
        [InitializeOnEnterPlayMode]
        static void Startup()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            
            BindingSystemBridge.Initialize();
            DrawerSystem.Initialize();
            Optimizer.Initialize();
            UnityThread.Initialize();
            InspectorProxy.Initialize();
            
            ProxyBindingSystem.Initialize();
            ProxyBindingFixer.Initialize();
            ProxyBindingEventInjector.Initialize();
            
            PinningSystem.Initialize();
            RefactorSystem.Initialize();
            FieldRoutes.Initialize(EditorUtility.SetDirty);

            RegisterCustomAccessors();
            
            BindingSystemRuntimeInit.Init();

            BindColors.IsDarkTheme = EditorGUIUtility.isProSkin;

            EditorApplication.delayCall += IconsRegistrar.RegisterBasicTypes;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorApplication.update += UnityThread.SpinOnce;
            
            BindingSystemValidator.AutoValidate(true);
            
            BindDataDrawer.InitializeGlobally();

            // ShowProxyBindingsInHierarchy();
        }

        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if(_fullyInitialized)
            {
                return;
            }
            _fullyInitialized = true;
            
            EnsureMetaValuesExist();
            EnsureAssetExists<FieldRoutes>(FieldRoutes.AssetName, _ => FieldRoutes.Initialize());
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }
            
            IconsRegistrar.Invalidate();
            EditorApplication.delayCall += IconsRegistrar.RegisterBasicTypes;
        }

        private static void EnsureMetaValuesExist()
        {
            if (AssetDatabase.Contains(BindSystem.MetaValues)) return;

            try
            {
                var path = BindingSystemIO.GetResourcePath("bind-meta-values.asset");
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BindMetaValues>(), path);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{BindSystem.DebugPrefix} Failed to create BindMetaValues asset. Exception: {e}");
            }
        }
        
        private static void EnsureAssetExists<T>(string assetName, Action<T> onCreated = null) where T : ScriptableObject
        {

            try
            {
                if (!assetName.EndsWith(".asset"))
                {
                    assetName += ".asset";
                }
                var path = BindingSystemIO.GetResourcePath(assetName);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset)
                {
                    return;
                }

                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                onCreated?.Invoke(asset);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{BindSystem.DebugPrefix} Failed to create {assetName} asset. Exception: {e}");
            }
        }

        private static void RegisterCustomAccessors()
        {
            foreach (var method in TypeCache.GetMethodsWithAttribute<RegistersCustomAccessorsAttribute>())
            {
                try
                {
                    method.Invoke(null, null);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"{BindSystem.DebugPrefix} Failed to register custom accessors for method {method?.ReflectedType?.FullName}.{method?.Name}(). The method should be static void and without parameters.\n" +
                                   $"Exception: {e}");
                }
            }
        }
        
        private static void ShowProxyBindingsInHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += (id, rect) =>
            {
#if UNITY_6000_3_OR_NEWER
                var go = EditorUtility.EntityIdToObject(id) as GameObject;
#else
                var go = EditorUtility.InstanceIDToObject(id) as GameObject;
#endif
                if (go == null)
                {
                    return;
                }
                var rect2 = new Rect(rect.xMax - 16, rect.y, 16, 16);
                if (go.GetComponent<ProxyBindings>() != null)
                {
                    EditorGUI.DrawRect(rect2, Color.red);
                }
            };
        }

    }
}