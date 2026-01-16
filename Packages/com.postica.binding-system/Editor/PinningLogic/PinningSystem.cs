using System;
using System.Collections.Generic;
using System.Linq;
using Postica.Common;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.PinningLogic
{
    internal static class PinningSystem
    {
        private const string PinText = "Pin to Bind Sources";
        private const string PinChildrenText = "Pin Children to Bind Sources";
        private const string UnpinText = "Unpin from Bind Sources";
        private const string PinCommand = "Binding/" + PinText;
        private const string UnpinCommand = "Binding/" + UnpinText;
        
        private static bool _initialized;
        private static PinnedStorageAsset _globalPins;
        private static readonly Dictionary<string, PinnedStorageComponent> _scenePins = new();
        private static Object[] _currentSelection;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            EditorApplication.contextualPropertyMenu -= OnContextualPropertyMenu;
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
            
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            _currentSelection = Selection.objects;
        }
        
        internal static void UseContext(Object context)
        {
            var storage = GetPinnedStorage(context);
            storage?.StorePinUsage(context);
            EnsureSaved(storage);
        }
        
        internal static IEnumerable<Object> GetLastUsedContexts(Object reference)
        {
            if (reference)
            {
                return GetPinnedStorage(reference)?.GetLastUsedPins().Concat(GetGlobalPins().GetLastUsedPins());
            }
            var storages = _scenePins
                .Where(p => SceneManager.GetSceneByName(p.Key).isLoaded)
                .Select(p => p.Value as IPinnedStorage)
                .Distinct().ToList();
            storages.Add(GetGlobalPins());
            return storages.SelectMany(s => s.GetLastUsedPins()).Distinct();
        }

        internal static IList<PinnedPath> GetAllPinnedPaths(params Object[] targets)
        {
            var paths = targets
                .Select(GetPinnedStorage).SelectMany(s => s.AllPaths)
                .Distinct().ToList();
            paths.AddRange(GetGlobalPins()?.AllPaths ?? ArraySegment<PinnedPath>.Empty);
            return paths;
        }
        
        internal static IEnumerable<IPinnedStorage> GetAllPinnedStorages()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneStorage = GetStorageInScene(currentScene, createIfMissing: false);
            if (_scenePins.ContainsKey(currentScene.path))
            {
                return _scenePins.Values.Concat(new[] {GetGlobalPins()});
            }
            return currentSceneStorage != null 
                ? _scenePins.Values.Concat(new[] { currentSceneStorage, GetGlobalPins()})
                : _scenePins.Values.Concat(new[] { GetGlobalPins()});
        }
        
        [MenuItem("CONTEXT/Component/" + PinCommand, false, 10)]
        private static void PinComponent(MenuCommand command)
        {
            if (TryGetObjectPath(command.context, out var pinnedStorage, out var pinnedPath))
            {
                pinnedStorage.AddPath(pinnedPath);
                UseContext(command.context);
            }
        }
        
        [MenuItem("CONTEXT/Component/" + UnpinCommand, false, 10)]
        private static void UnpinComponent(MenuCommand command)
        {
            if (TryGetObjectPath(command.context, out var pinnedStorage, out var pinnedPath))
            {
                pinnedStorage.RemovePath(pinnedPath);
                UseContext(command.context);
            }
        }
        
        [MenuItem("CONTEXT/Component/" + PinCommand, true)]
        private static bool CanPinComponent(MenuCommand command) => !IsPinned(command.context);
        
        [MenuItem("CONTEXT/Component/" + UnpinCommand, true)]
        private static bool CanUnpinAComponent(MenuCommand command) => IsPinned(command.context);
        
        [MenuItem("Assets/" + PinCommand, false, 10000)]
        private static void PinAsset(MenuCommand command)
        {
            if (TryGetObjectPath(Selection.activeObject, out var pinnedStorage, out var pinnedPath))
            {
                pinnedStorage.AddPath(pinnedPath);
                UseContext(Selection.activeObject);
                EditorApplication.delayCall += () => Selection.objects = _currentSelection;
            }
        }
        
        [MenuItem("Assets/" + UnpinCommand, false, 10000)]
        private static void UnpinAsset(MenuCommand command)
        {
            if (TryGetObjectPath(Selection.activeObject, out var pinnedStorage, out var pinnedPath))
            {
                pinnedStorage.RemovePath(pinnedPath);
                UseContext(Selection.activeObject);
                EditorApplication.delayCall += () => Selection.objects = _currentSelection;
            }
        }
        
        [MenuItem("Assets/" + PinCommand, true)]
        private static bool CanPinAsset() => !IsPinned(Selection.activeObject);
        
        [MenuItem("Assets/" + UnpinCommand, true)]
        private static bool CanUnpinAsset() => IsPinned(Selection.activeObject);
        
        [MenuItem("GameObject/" + PinCommand, false, 10000)]
        private static void PinGameObject(MenuCommand command) => PinAsset(command);

        [MenuItem("GameObject/" + UnpinCommand, false, 10000)]
        private static void UnpinGameObject(MenuCommand command) => UnpinAsset(command);

        [MenuItem("GameObject/" + PinCommand, true)]
        private static bool CanPinGameObject() => Selection.activeGameObject && !IsPinned(Selection.activeGameObject);
        
        [MenuItem("GameObject/" + UnpinCommand, true)]
        private static bool CanUnpinGameObject() => IsPinned(Selection.activeGameObject);
        
        public static void PinContextualMenuProperty(GenericMenu menu, SerializedProperty property)
        {
            OnContextualPropertyMenu(menu, property);
        }

        private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            var pinnedStorage = GetPinnedStorage(property.serializedObject.targetObject);
            var source = property.serializedObject.targetObject;
            var path = property.propertyPath;
            Type propertyType = null;
            try
            {
                propertyType = property.GetPropertyType(pathMayBeComplex: false);
            }
            catch
            {
                return;
            }

            if (propertyType == null)
            {
                return;
            }

            PinnedPath pinnedPath;
            try
            {
                pinnedPath = new PinnedPath(source, path, propertyType);
            }
#if BS_DEBUG
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }
#else
            catch
            {
                return;
            }
#endif
            if (pinnedStorage.ContainsPath(pinnedPath))
            {
                menu.AddItem(new GUIContent(UnpinText), false,
                    () =>
                    {
                        pinnedStorage.RemovePath(pinnedPath);
                        RemoveStorageIfEmpty();
                    });
            }
            else
            {
                menu.AddItem(new GUIContent(PinText), false, () => PinProperty(pinnedPath));
                if (property.hasVisibleChildren || property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    menu.AddItem(new GUIContent(PinChildrenText), false,
                        () => PinProperty(pinnedPath.WithFlags(PinnedPath.BitFlags.PinChildren)));
                }
            }

            // RemoveStorageIfEmpty();
            return;

            void PinProperty(PinnedPath newPath)
            {
                pinnedStorage.AddPath(newPath);
                UseContext(source);
            }

            void RemoveStorageIfEmpty()
            {
                if (!pinnedStorage.AllPaths.Any() && pinnedStorage is PinnedStorageComponent component)
                {
                    _scenePins.Remove(component.gameObject.scene.path);
                    Object.DestroyImmediate(component.gameObject);
                }
            }
        }

        private static bool TryGetObjectPath(Object source, out IPinnedStorage storage, out PinnedPath path)
        {
            if(!source)
            {
                path = default;
                storage = default;
                return false;
            }
            storage = GetPinnedStorage(source);
            path = new PinnedPath(source, "/", source.GetType());
            return true;
        }
        
        private static void EnsureSaved(IPinnedStorage pinnedStorage)
        {
            if (pinnedStorage is Object pinnedStorageObject && pinnedStorageObject)
            {
                EditorUtility.SetDirty(pinnedStorageObject);
                AssetDatabase.SaveAssetIfDirty(pinnedStorageObject);
            }
        }

        private static bool IsPinned(Object source, string path = "/")
        {
            if(!source)
            {
                return false;
            }
            var storage = GetPinnedStorage(source);
            var pinnedPath = new PinnedPath(source, path, source.GetType());
            return storage.ContainsPath(pinnedPath);
        }
        
        private static IPinnedStorage GetPinnedStorage(Object target)
        {
            if (target is GameObject go)
            {
                return GetFromGameObject(go);
            }

            if (target is Component c)
            {
                return GetFromGameObject(c.gameObject);
            }

            return GetGlobalPins();
        }

        private static IPinnedStorage GetGlobalPins()
        {
            if (_globalPins)
            {
                return _globalPins;
            }

            // This file prevents the build of player, need to move it out of Resources folder
            var path = BindingSystemIO.GetBindingsPath("global-pins.asset");
            
            _globalPins = AssetDatabase.LoadAssetAtPath<PinnedStorageAsset>(path);
            if (_globalPins) return _globalPins;
            
            _globalPins = ScriptableObject.CreateInstance<PinnedStorageAsset>();
            _globalPins.hideFlags = HideFlags.DontSaveInBuild;
            AssetDatabase.CreateAsset(_globalPins, path);
            AssetDatabase.SaveAssetIfDirty(_globalPins);

            return _globalPins;
        }

        private static IPinnedStorage GetFromGameObject(GameObject go)
        {
            var scene = go.scene;
            return !PrefabUtility.IsPartOfPrefabAsset(go) ? GetStorageInScene(scene) : GetGlobalPins();
        }

        private static IPinnedStorage GetStorageInScene(Scene scene, bool createIfMissing = true)
        {
            // if(string.IsNullOrEmpty(scene.path))
            // {
            //     return GetGlobalPins();
            // }
            
            if (!string.IsNullOrEmpty(scene.path) && _scenePins.TryGetValue(scene.path, out var storage))
            {
                return storage;
            }

            PinnedStorageComponent storageComponent = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out storageComponent))
                {
                    break;
                }
            }
            
            if (!storageComponent && createIfMissing)
            {
                storageComponent = new GameObject("PinnedStorage").AddComponent<PinnedStorageComponent>();
                storageComponent.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
            }

            if (storageComponent && !string.IsNullOrEmpty(scene.path))
            {
                _scenePins[scene.path] = storageComponent;
            }

            return storageComponent;
        }
    }
}