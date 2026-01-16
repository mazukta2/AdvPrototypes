using Postica.BindingSystem.Accessors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Postica.Common;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;

namespace Postica.BindingSystem
{

    class Optimizer : AssetPostprocessor
    {
        private static bool _initialized;
        private static bool _autoRegistered;
        
        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            // RunAutoRegistrations(BindingSettings.Current);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if(_autoRegistered)
            {
                return;
            }
            _autoRegistered = true;
            RunAutoRegistrations(BindingSettings.Current);
        }

        public static void RunAutoRegistrations(BindingSettings settings)
        {
            if(settings.AutoRegisterModifiers || settings.AutoRegisterProviders || settings.AutoRegisterConverters)
            {
                GenerateUserDefinedClasses();
            }
        }
        
        #region Links File Generation
        
        public static void GenerateLinkFile(string filePath, Action<string, float> progressCallback)
        {
            StringBuilder sb = new StringBuilder();
            GenerateLinkFileContent(sb, progressCallback);

            var path = filePath ?? BindingSystemIO.GetBindingsPath("link.xml");

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
        }
        
        private static void GenerateLinkFileContent(StringBuilder sb, Action<string, float> progressCallback)
        {
            var processed = new HashSet<Object>();
            var types = new HashSet<Type>();
            
            ConvertersFactory.CreateInstanceOverride = (type, args) =>
            {
                var obj = Activator.CreateInstance(type, args);
                if(obj != null)
                {
                    types.Add(obj.GetType());
                }
                return obj;
            };

            var activeScene = SceneManager.GetActiveScene();
            var scenes = EditorBuildSettings.scenes;
            var scenesCount = scenes.Length;
            var allDisabled = scenes.All(s => !s.enabled);
            for (int i = 0; i < scenesCount; i++)
            {
                if (!scenes[i].enabled && allDisabled && scenes[i].path != activeScene.path)
                {
                    continue;
                }

                var path = scenes[i].path;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                var scene = SceneManager.GetSceneByBuildIndex(i);
                var isLoaded = scene.isLoaded;
                
                progressCallback?.Invoke("Processing Linker File", i / (float)scenesCount);
                
                try
                {
                    if (!isLoaded)
                    {
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }
                    foreach (var rootObj in scene.GetRootGameObjects())
                    {
                        var components = rootObj.GetComponentsInChildren<Component>(true);
                        foreach (var c in components)
                        {
                            if (c)
                            {
                                ScanForLinks(c, types, processed);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogError($"[BindingSystem]: Unable to process scene {scene.path} for AOT Generation. {ex}");
                }
                finally
                {
                    if (!isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            var resourcesObjs = Resources.FindObjectsOfTypeAll<Object>();
            foreach (var resourceObj in resourcesObjs)
            {
                if (resourceObj)
                {
                    ScanForLinks(resourceObj, types, processed);
                }
            }

            sb.AppendLine("<linker>");
            
            // Add base types
            foreach (var type in types.Distinct().ToList())
            {
                var baseType = type.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    types.Add(baseType);
                    baseType = baseType.BaseType;
                }
            }
            
            foreach(var typesGroup in types.Distinct().GroupBy(t => t.Assembly))
            {
                if (typesGroup.Key.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }
                if(typesGroup.Key.FullName.StartsWith("BindingSystem.Runtime"))
                {
                    continue;
                }
                sb.AppendLine($"  <assembly fullname=\"{typesGroup.Key.FullName}\">");
                foreach (var type in typesGroup)
                {
                    sb.AppendLine($"    <type fullname=\"{type.FullName}\" preserve=\"all\"/>");
                }
                sb.AppendLine("  </assembly>");
            }
            
            sb.AppendLine("</linker>");

            ConvertersFactory.CreateInstanceOverride = null;
        }

        private static void ScanForLinks(Object target, HashSet<Type> types, HashSet<Object> processed)
        {
            if (!processed.Add(target))
            {
                return;
            }

            var bindDataTypename = typeof(BindData<int>).Name;

            using (var serObj = new SerializedObject(target))
            {
                var iterator = serObj.FindProperty("m_Script");
                while (iterator != null 
                    && iterator.Next(iterator.propertyType == SerializedPropertyType.Generic 
                                  || iterator.propertyType == SerializedPropertyType.ManagedReference)
                    && iterator.depth < 20)
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var obj = iterator.objectReferenceValue;
                        if (obj)
                        {
                            ScanForLinks(obj, types, processed);
                        }
                    }
                    if(iterator.type == nameof(BindData) || iterator.type == bindDataTypename) 
                    {
                        using(var parentProperty = iterator.GetParent())
                        {
                            var isBoundProperty = parentProperty.FindPropertyRelative("_isBound");
                            if(isBoundProperty != null && !isBoundProperty.boolValue)
                            {
                                continue;
                            }
                            if(parentProperty.GetValue() is IBindAccessor bindAccessor)
                            {
                                if (iterator.GetValue() is IBindDataSimple data && data.Source != null)
                                {
                                    types.Add(data.Source.GetType());
                                }
                                AccessorsFactory.GetAccessorReturnTypes(bindAccessor.RawAccessor, types);
                                continue;
                            }
                        }
                        try
                        {
                            if (iterator.GetValue() is IBindDataSimple data && !string.IsNullOrEmpty(data.Path))
                            {
                                var accessor = data is IBindDataComplex complexData 
                                             ? AccessorsFactory.GetAccessor(data.Source, data.Path, complexData.Parameters, complexData.MainParameterIndex)
                                             : AccessorsFactory.GetAccessor(data.Source, data.Path);
                                AccessorsFactory.GetAccessorReturnTypes(accessor, types);
                                if(data.Source != null)
                                {
                                    types.Add(data.Source.GetType());
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            var targetName = target?.GetScenePathOrName() ?? target?.name ?? "<null>";
                            Debug.LogError($"[BindingSystem]: Unable to get value types for {targetName}->{iterator.propertyPath}. {ex}", target);
                        }
                    }
                }
                iterator?.Dispose();
            }
        }
        
        #endregion

        private static void GenerateUserDefinedClasses()
        {
            bool FilterType(Type type)
            {
                return !(type.IsAbstract
                    || type.IsGenericType
                    || type.IsInterface
                    || type.GetCustomAttribute<HideMemberAttribute>() != null);
            }

            var settings = BindingSettings.Current;
            var noTypes = Array.Empty<Type>() as IEnumerable<Type>;

            var converterTypes = settings.AutoRegisterConverters ? TypeCache.GetTypesDerivedFrom<IConverter>().Where(FilterType) : noTypes;
            var modifierTypes = settings.AutoRegisterModifiers ? TypeCache.GetTypesDerivedFrom<IModifier>().Where(FilterType) : noTypes;
            var providerTypes = settings.AutoRegisterProviders ? TypeCache.GetTypesDerivedFrom<IAccessorProvider>().Where(FilterType) : noTypes;

            var customTypesAsset = CustomBindTypesAsset.Instance;
            
            if(!converterTypes.Any() && !modifierTypes.Any() && !providerTypes.Any())
            {
                if (customTypesAsset)
                {
                    customTypesAsset.customConverters.Clear();
                    customTypesAsset.customModifiers.Clear();
                    customTypesAsset.customAccessorProviders.Clear();
                    customTypesAsset.removedTypes.Clear();
                }
                return;
            }
            
            if(!customTypesAsset)
            {
                customTypesAsset = ScriptableObject.CreateInstance<CustomBindTypesAsset>();
                customTypesAsset.name = "bind-types";
                var filePath = BindingSystemIO.GetResourcePath("bind-types.asset");
                AssetDatabase.CreateAsset(customTypesAsset, filePath);
                CustomBindTypesAsset.Instance = customTypesAsset;
            }

            if (settings.AutoRegisterConverters)
            {
                customTypesAsset.customConverters = converterTypes.Select(t => new SerializedType(t)).ToList();
            }
            if (settings.AutoRegisterModifiers)
            {
                customTypesAsset.customModifiers = modifierTypes.Select(t => new SerializedType(t)).ToList();
            }
            if (settings.AutoRegisterProviders)
            {
                customTypesAsset.customAccessorProviders = providerTypes.Select(t => new SerializedType(t)).ToList();
            }
            
            EditorUtility.SetDirty(customTypesAsset);
        }
    }
}