using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MonoHook;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using Postica.Common.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.ProxyBinding
{
    internal static class ProxyBindingSystem
    {
        private const string EnableBindingText = "Enable Binding";
        private const string DisableBindingText = "Disable Binding";
        private const string CreateBindingsAssetText = "Create Bindings Asset";

        private static Dictionary<string, Type> _rootTypeReplacements = new()
        {
            { "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData", typeof(Camera) },
            { "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData", typeof(Camera) },
            { "UnityEngine.Rendering.Universal.UniversalAdditionalLightData", typeof(Light) },
            { "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData", typeof(Light) },
        };

        private static readonly UndoManager _undo = new();
        private static bool _initialized;
        private static Object _lastTarget;
        private static MethodHook _matPropertyMenuHook;
        private static ProxyBindingsAsset _globalBindings;
        private static List<InspectorBindings> _bindings = new();
        private static Dictionary<Object, ProxyBindingsAsset> _activeAssetBindings = new();
        
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            EditorApplication.contextualPropertyMenu -= OnContextualPropertyMenu;
            EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;

            InspectorProxy.OnInspectorClosed += OnInspectorClosed;
            InspectorProxy.OnInspectorOpened += OnInspectorOpened;

            EditorApplication.delayCall += () =>
            {
                DrawerSystem.MaterialEditor.contextualPropertyMenu += (_, _, _) =>
                {
#if BS_DEBUG
                    Debug.Log(BindSystem.DebugPrefix + "MaterialEditor.contextualPropertyMenu");
#endif
                };
                
                EditorApplication.delayCall += HookToMaterialPropertyMenu;
            };
            Undo.undoRedoEvent += UndoRedoPerformed;
        }

        private static void HookToMaterialPropertyMenu()
        {
            if (_matPropertyMenuHook != null)
            {
                return;
            }
            
            var propertyDataTypename = typeof(MaterialProperty).FullName + "+PropertyData";
            var propertyData = typeof(MaterialProperty).Assembly.GetType(propertyDataTypename);
            if (propertyData == null)
            {
                Debug.LogError(BindSystem.DebugPrefix + "Unable to activate Material Property Binding System");
                return;
            }

            var originalMethod = propertyData.GetVoidMethod<GenericMenu, bool, Object[]>("DoRegularMenu");
            var localMethod = typeof(ProxyBindingSystem).GetVoidMethod<GenericMenu, bool, Object[]>(nameof(OnMaterialPropertyContextMenu));
            var proxyMethod = typeof(ProxyBindingSystem).GetVoidMethod<GenericMenu, bool, Object[]>(nameof(DefaultRegularMenu));
            
            _matPropertyMenuHook = new MethodHook(originalMethod, localMethod, proxyMethod);
            _matPropertyMenuHook.Install();
            
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                _matPropertyMenuHook.Uninstall();
                _matPropertyMenuHook = null;
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnMaterialPropertyContextMenu(GenericMenu menu, bool isOverriden, Object[] materials)
        {
            if (materials.Length != 1)
            {
                return;
            }
            
            var target = _lastTarget switch
            {
                Renderer r => r,
                GameObject go => go.GetComponent<Renderer>(),
                Component c => c.GetComponent<Renderer>(),
                _ => _lastTarget
            };

            var prefix = GetPathPrefix(target, materials[0] as Material);
            
            menu.AddDisabledItem(new GUIContent("/"));

            foreach (var materialProperty in DrawerSystem.MaterialProperty.PropertyData.capturedProperties)
            {
                HandlePropertyMenu(target, prefix, menu, materialProperty);
            }
            DefaultRegularMenu(menu, isOverriden, materials);
        }
        
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void DefaultRegularMenu(GenericMenu menu, bool isOverriden, Object[] targets)
        {
            Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
        }
        
        private static string GetMaterialPropertyName(MaterialProperty property, string displayName)
        {
            var isTiling = displayName == DrawerSystem.MaterialEditor.TilingText.text;
            var isOffset = displayName == DrawerSystem.MaterialEditor.OffsetText.text;
            
            return GetMaterialPropertyName(property, isTiling, isOffset);
        }

        private static string GetMaterialPropertyName(MaterialProperty property, bool isTiling, bool isOffset)
        {
#if UNITY_6000_2_OR_NEWER
            var suffix = property.propertyType switch
            {
                ShaderPropertyType.Texture when isTiling => "tiling",
                ShaderPropertyType.Texture when isOffset => "offset",
                _ => property.propertyType.ToString().ToLower(),
            };
#else
            var suffix = property.type switch
            {
                MaterialProperty.PropType.Texture when isTiling => "tiling",
                MaterialProperty.PropType.Texture when isOffset => "offset",
                _ => property.type.ToString().ToLower(),
            };
#endif
            return $"{property.name}-{suffix}";
        }

        private static void HandlePropertyMenu(Object context, string prefix, GenericMenu menu, MaterialProperty property)
        {
            var prefixLabelText = DrawerSystem.EditorGUI.PrefixLabel?.text;
#if UNITY_6000_2_OR_NEWER
            var isTiling = property.propertyType == ShaderPropertyType.Texture && prefixLabelText == DrawerSystem.MaterialEditor.TilingText.text;
            var isOffset = property.propertyType == ShaderPropertyType.Texture &&  prefixLabelText == DrawerSystem.MaterialEditor.OffsetText.text;
#else
            var isTiling = property.type == MaterialProperty.PropType.Texture && prefixLabelText == DrawerSystem.MaterialEditor.TilingText.text;
            var isOffset = property.type == MaterialProperty.PropType.Texture &&  prefixLabelText == DrawerSystem.MaterialEditor.OffsetText.text;
#endif
            var isOffsetOrTiling = isOffset || isTiling;

            var path = prefix + GetMaterialPropertyName(property, isOffsetOrTiling, false);
            var otherPath = isOffsetOrTiling ? prefix + GetMaterialPropertyName(property, false, true) : null;
            var bindings = GetProxyBindings(context, createIfMissing: false);

#if UNITY_6000_2_OR_NEWER
            var propertyTypeName = property.propertyType switch
            {
                ShaderPropertyType.Texture when isOffsetOrTiling => nameof(Vector2),
                _ => property.propertyType.ToString()
            };
#else
            var propertyTypeName = property.type switch
            {
                MaterialProperty.PropType.Texture when isOffsetOrTiling => nameof(Vector2),
                _ => property.type.ToString()
            };
#endif
            
            HandleProperty(path, isOffsetOrTiling ? "Tiling" : propertyTypeName);
            if (otherPath != null)
            {
                HandleProperty(otherPath, isOffsetOrTiling ? "Offset" : propertyTypeName);
            }

            void HandleProperty(string propPath, string suffix)
            {
                if (bindings?.TryGetProxy(context, propPath, out _, out _) == true)
                {
                    var bindText = $"{DisableBindingText} for {suffix}";
                    menu.AddItem(new GUIContent(bindText), false,
                        () =>
                        {
                            if (bindings is Object bindingsObj)
                            {
                                _undo.RegisterCompleteObjectUndo(bindingsObj, bindText);
                            }

                            if (bindings?.RemoveProxy(context, propPath) != true) return;
                    
                            foreach (var inspector in GetInspectorsBindings(context))
                            {
                                inspector.RemoveBinding(context, propPath);
                            }
                            
                            if (bindings.IsEmpty)
                            {
                                // Remove the bindings object if it is empty
                                foreach (var inspector in GetInspectorsBindings(context))
                                {
                                    inspector.DestroyBindingsObject();
                                }
                            }
                        });
                }
                else
                {
                    var bindText = $"{EnableBindingText} for {suffix}";
                    menu.AddItem(new GUIContent(bindText), false,
                        () =>
                        {
                            bindings ??= GetProxyBindings(context, createIfMissing: true);
#if UNITY_6000_2_OR_NEWER
                            var type = property.propertyType switch
                            {
                                ShaderPropertyType.Color => typeof(Color),
                                ShaderPropertyType.Float => typeof(float),
                                ShaderPropertyType.Range => typeof(float),
                                ShaderPropertyType.Vector => typeof(Vector4),
                                ShaderPropertyType.Texture when isOffsetOrTiling => typeof(Vector2),
                                ShaderPropertyType.Texture => typeof(Texture),
                                ShaderPropertyType.Int => typeof(int),
                                _ => null,
                            };
#else
                            var type = property.type switch
                            {
                                MaterialProperty.PropType.Color => typeof(Color),
                                MaterialProperty.PropType.Float => typeof(float),
                                MaterialProperty.PropType.Range => typeof(float),
                                MaterialProperty.PropType.Vector => typeof(Vector4),
                                MaterialProperty.PropType.Texture when isOffsetOrTiling => typeof(Vector2),
                                MaterialProperty.PropType.Texture => typeof(Texture),
                                MaterialProperty.PropType.Int => typeof(int),
                                _ => null,
                            };
#endif
                    
                            var bindProxy = new BindProxy()
                            {
                                Source = context,
                                Path = propPath,
                                ValueType = type,
                                OptionsValue = BindProxy.Options.MaterialProperty
                            };

                            if (bindings is Object bindingsObj)
                            {
                                _undo.RegisterCompleteObjectUndo(bindingsObj, bindText);
                            }

                            if (bindings?.AddProxy(bindProxy) != true) return;

                            foreach (var inspector in GetInspectorsBindings(context))
                            {
                                inspector.AddBinding(bindProxy);
                            }
                        });
                }
            }
        }

        private static void UndoRedoPerformed(in UndoRedoInfo undo)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                var inspectorBindings = _bindings[i];
                if (!inspectorBindings.source)
                {
                    inspectorBindings.Dispose();
                    _bindings.RemoveAt(i--);
                    continue;
                }

                if (!_undo.IsValidUndoOrRedo(undo))
                {
                    continue;
                }
                
                inspectorBindings.Refresh(true);
            }
        }

        public static IEnumerable<Editor> GetEditorsOf(Object target)
        {
            var list = new List<Editor>();
            foreach (var inspectorBinding in _bindings)
            {
                if (!inspectorBinding.inspector.IsOpen)
                {
                    continue;
                }

                foreach (var editor in inspectorBinding.inspector.Tracker.activeEditors)
                {
                    var editorTarget = editor.target;
                    if (editorTarget is AssetImporter assetImporter)
                    {
                        editorTarget = AssetDatabase.LoadMainAssetAtPath(assetImporter.assetPath);
                    }

                    if (editorTarget == target)
                    {
                        list.Add(editor);
                    }
                }
            }

            return list;
        }

        private static void OnInspectorOpened(InspectorProxy inspector)
        {
            inspector.Changed += InspectorOnChanged;
        }
        
        private static InspectorBindings GetInspectorBindings(InspectorProxy inspector)
        {
            return _bindings.Find(b => b.inspector == inspector);
        }
        
        private static InspectorBindings GetInspectorBindings(Object source)
        {
            if (source is Component c)
            {
                source = c.gameObject;
            }
            return _bindings.Find(b => b.source == source);
        }

        private static IEnumerable<InspectorBindings> GetInspectorsBindings(Object source)
        {
            if (source is Component c)
            {
                source = c.gameObject;
            }
            return _bindings.FindAll(b => b.source == source);
        }

        private static void InspectorOnChanged(InspectorProxy inspector, Editor[] editors)
        {
            RemoveBindings(inspector);
            
            SaveDirtyBindings();
            
            ReattachNonInspectorBindings();
            
            if (editors.Length == 0)
            {
                return;
            }

            var mainEditor = editors[0];
            var target = mainEditor.target;
            if (target is AssetImporter assetImporter)
            {
                target = AssetDatabase.LoadMainAssetAtPath(assetImporter.assetPath);
            }

            _lastTarget = target;
            
            if(!target)
            {
                return;
            }
            
            var bindings = new InspectorBindings(inspector, target);
            _bindings.Add(bindings);
            
            inspector.OnUIToolkitReady(bindings.AttachBindings);
        }

        private static void SaveDirtyBindings()
        {
            foreach (var pair in _activeAssetBindings)
            {
                if (!pair.Key)
                {
                    continue;
                }
                
                if(pair.Key.name == "global-bindings")
                {
                    continue;
                }
                
                if(pair.Value == null)
                {
                    continue;
                }

                if (pair.Value.IsEmpty)
                {
                    var isVisible = pair.Value.hideFlags == HideFlags.None;
                    AssetDatabase.RemoveObjectFromAsset(pair.Value);
                    Object.DestroyImmediate(pair.Value, true);
                    if (isVisible && BindingSettings.Current.ShowProxyBindings)
                    {
                        var keyPath = AssetDatabase.GetAssetPath(pair.Key);
                        if (!string.IsNullOrEmpty(keyPath))
                        {
                            AssetDatabase.ImportAsset(keyPath);
                        }
                    }

                    continue;
                }
                
                var path = AssetDatabase.GetAssetPath(pair.Key);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.Contains(pair.Key))
                {
                    AssetDatabase.RemoveObjectFromAsset(pair.Value);
                    Object.DestroyImmediate(pair.Value, true);
                    continue;
                }

                if (AssetDatabase.Contains(pair.Value))
                {
                    continue;
                }
                
                AssetDatabase.AddObjectToAsset(pair.Value, pair.Key);
                AssetDatabase.SaveAssetIfDirty(pair.Value);
            }
            
            _activeAssetBindings.Clear();
        }

        private static void OnInspectorClosed(InspectorProxy inspector)
        {
            RemoveBindings(inspector);

            ReattachNonInspectorBindings();
        }

        private static void ReattachNonInspectorBindings()
        {
            foreach (var inspectorBinding in _bindings)
            {
                if (inspectorBinding.inspector is not { IsOpen: true, IsInspector: false })
                {
                    continue;
                }
                
                inspectorBinding.ReattachBindings(inspectorBinding.inspector.Root);
            }
        }

        private static bool RemoveBindings(InspectorProxy inspector)
        {
            var bindings = GetInspectorBindings(inspector);
            if(bindings != null)
            {
                bindings.Dispose();
                return _bindings.Remove(bindings);
            }
            
            return false;
        }

        public static void AddBindMenuItems(GenericMenu menu, SerializedProperty property)
            => OnContextualPropertyMenu(menu, property);

        private static void OnContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            menu.AddSeparator("/");
            var bindProxies = GetProxyBindings(property.serializedObject.targetObject, createIfMissing: false);
            var source = property.serializedObject.targetObject;
            var path = property.propertyPath;
            var serializedObject = property.serializedObject;
            
#if BS_DEBUG
            menu.AddDisabledItem(new GUIContent($"{property.propertyPath}: {property.contentHash}"));
#endif

            if (bindProxies is Object bindProxiesObj && source == bindProxiesObj)
            {
                return;
            }
            
            if (bindProxies?.TryGetProxy(source, path, out var proxy, out _) == true)
            {
                menu.AddItem(new GUIContent(DisableBindingText), false,
                    () =>
                    {
                        if (bindProxies is Object bindingsObj)
                        {
                            _undo.RegisterCompleteObjectUndo(bindingsObj, DisableBindingText);
                        }

                        if (!bindProxies.RemoveProxy(source, path)) return;
                        
                        if (serializedObject.IsAlive())
                        {
                            serializedObject.Update();
                        }
                        
                        foreach (var inspector in GetInspectorsBindings(source))
                        {
                            inspector.RemoveBinding(source, path);
                        }

                        if (bindProxies.IsEmpty)
                        {
                            // Remove the bindings object if it is empty
                            foreach (var inspector in GetInspectorsBindings(source))
                            {
                                inspector.DestroyBindingsObject();
                            }
                        }
                        _undo.IncrementCurrentGroup();
                    });
            }
            else
            {
#if BS_DEBUG
                Debug.Log("Binding Property: " + property.propertyPath);
#endif
                Type propertyType = null;
                try
                {
                    propertyType = property.GetPropertyType(pathMayBeComplex: false);
                    if (ShouldNotBind(property, propertyType))
                    {
                        return;
                    }
                }
#if BS_DEBUG
                catch
                {
                    throw;
                }
                Debug.Log($"Clicked to make bindable: {property.propertyPath} - {propertyType}");
#else
                catch
                {
                    return;
                }
#endif

                menu.AddItem(new GUIContent(EnableBindingText), false, () =>
                {
                    var bindProxy = new BindProxy()
                    {
                        Source = source,
                        Path = path,
                        ValueType = propertyType
                    };
                    
                    _undo.IncrementCurrentGroup();
                    
                    bindProxies ??= GetProxyBindings(source, createIfMissing: true);
                    
                    if (bindProxies is Object bindingsObj)
                    {
                        _undo.RegisterCompleteObjectUndo(bindingsObj, EnableBindingText);
                    }

                    if (bindProxies?.AddProxy(bindProxy) != true) return;

                    if (serializedObject.IsAlive())
                    {
                        serializedObject.Update();
                    }
                    
                    foreach (var inspector in GetInspectorsBindings(source))
                    {
                        inspector.AddBinding(bindProxy);
                    }
                    
                    // EditorApplication.delayCall += Undo.IncrementCurrentGroup;
                });
            }
        }

        private static bool ShouldNotBind(SerializedProperty property, Type propertyType)
        {
            if(typeof(IBind).IsAssignableFrom(propertyType))
            {
                return true;
            }

            if (typeof(IBind).IsAssignableFrom(property.GetParent()?.GetPropertyType()))
            {
                return true;
            }

            return false;
        }

        private static IBindProxyProvider GetProxyBindings(Object target, bool createIfMissing = true)
        {
            if (target is GameObject go)
            {
                return GetFromGameObject(go, createIfMissing);
            }

            if (target is Component c)
            {
                return GetFromGameObject(c.gameObject, createIfMissing);
            }

            return GetBindingsForAsset(target, createIfMissing);
        }

        private static IBindProxyProvider GetBindingsForAsset(Object target, bool createIfMissing)
        {
            if (target is ScriptableObject so)
            {
                var path = AssetDatabase.GetAssetPath(so);
                if (_activeAssetBindings.TryGetValue(so, out var bindings))
                {
                    if (!string.IsNullOrEmpty(path) && bindings.HasBindings && !AssetDatabase.Contains(bindings))
                    {
                        AssetDatabase.AddObjectToAsset(bindings, target);
                        AssetDatabase.SaveAssetIfDirty(bindings);
                        _activeAssetBindings.Remove(so);
                    }
                    return bindings;
                }
                if (!string.IsNullOrEmpty(path))
                {
                    bindings = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(a => a is ProxyBindingsAsset) as ProxyBindingsAsset;
                }
                
                if (bindings)
                {
                    _activeAssetBindings[so] = bindings;
                    return bindings;
                }
                
                if (!createIfMissing)
                {
                    return null;
                }
                
                bindings = ScriptableObject.CreateInstance<ProxyBindingsAsset>();
                bindings.name = "Bindings";
                bindings.hideFlags = BindingSettings.Current.ShowProxyBindings ? HideFlags.None : HideFlags.HideInHierarchy;

                if (!_undo.isProcessing)
                {
                    _undo.RegisterCreatedObjectUndo(bindings, CreateBindingsAssetText);
                }

                _activeAssetBindings[so] = bindings;

                return bindings;
            }
            if (_globalBindings)
            {
                return _globalBindings;
            }

            _globalBindings = Resources.Load<ProxyBindingsAsset>("global-bindings");
            
            if (!_globalBindings)
            {
                if(!createIfMissing)
                {
                    return null;
                }
                
                var path = BindingSystemIO.GetResourcePath("global-bindings.asset");
                _globalBindings = ScriptableObject.CreateInstance<ProxyBindingsAsset>();
                AssetDatabase.CreateAsset(_globalBindings, path);
            }

            return _globalBindings;
        }

        private static IBindProxyProvider GetFromGameObject(GameObject go, bool createIfMissing)
        {
            if(!go)
            {
                return null;
            }
            if(!go.TryGetComponent(out ProxyBindings proxyBindings) || !proxyBindings)
            {
                if (!createIfMissing)
                {
                    return null;
                }
                proxyBindings = _undo.AddComponent<ProxyBindings>(go, EnableBindingText);
            }

            var showBindings = BindingSettings.Current.ShowProxyBindings;
            
            proxyBindings.hideFlags = showBindings ? HideFlags.None : HideFlags.HideInInspector;
            
            return proxyBindings;
        }
        
        private static string GetPathPrefix(Object source, Material material)
        {
            var renderer = source switch
            {
                Renderer r => r,
                Component c => c.GetComponent<Renderer>(),
                GameObject go => go.GetComponent<Renderer>(),
                _ => null
            };
            
            if(!renderer)
            {
                return "";
            }
            
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] == material)
                {
                    return $"sharedMaterials.Array.data[{i}].";
                }
            }
                
            return "";
        }

        private class InspectorBindings : IDisposable
        {
            private SerializedObject _serializedObject;
            private SerializedProperty _bindingsProperty;
            private List<BindProxy> _allBindings;
            private List<BindProxy> _inOrderBindings;
            private IBindProxyProvider _bindingsObject;
            private VisualElement _lastRoot;
            
            public readonly Object source;
            public readonly InspectorProxy inspector;
            public readonly Dictionary<(Object owner, string path), BindProxyDrawer.BindProxyView> proxyViewsByPath = new();
            public readonly Dictionary<(Object owner, string path), VisualElement> rootViews = new();

            public IBindProxyProvider BindingsObject
            {
                get
                {
                    InitializeIfNeeded();

                    return _bindingsObject;
                }
            }

            public List<BindProxy> InOrderBindings
            {
                get
                {
                    if (_inOrderBindings != null)
                    {
                        for (int i = 0; i < _inOrderBindings.Count; i++)
                        {
                            if (!_inOrderBindings[i].Source)
                            {
                                _inOrderBindings.RemoveAt(i);
                                i--;
                            }
                        }
                        return _inOrderBindings;
                    }
                    
                    _inOrderBindings = new List<BindProxy>();
                    for (int i = 0; i < BindingsProperty.arraySize; i++)
                    {
                        var element = BindingsProperty.GetArrayElementAtIndex(i);
                        var bindProxy = element.GetValue() as BindProxy;
                        if(bindProxy == null || !bindProxy.Source)
                        {
                            continue;
                        }
                        _inOrderBindings.Add(bindProxy);
                    }
                    
                    return _inOrderBindings;
                }
            }
            
            private SerializedProperty BindingsProperty
            {
                get
                {
                    if (!_bindingsProperty.IsAlive() && BindingsObject is Object obj)
                    {
                        if (!_serializedObject.IsAlive() || _serializedObject.targetObject != obj)
                        {
                            _serializedObject = new SerializedObject(obj);
                        }
                        _bindingsProperty = _serializedObject.FindProperty("bindings");
                    }

                    if (_serializedObject.targetObject)
                    {
                        _bindingsProperty.serializedObject.Update();
                    }

                    return _bindingsProperty;
                }
            }
            
            public InspectorBindings(InspectorProxy inspector, Object source)
            {
                this.source = source;
                this.inspector = inspector;
            }

            private void InitializeIfNeeded(bool avoidAttach = false)
            {
                if (_bindingsObject != null && _bindingsObject as Object)
                {
                    return;
                }
                
                _bindingsObject = GetProxyBindings(this.source, createIfMissing: true);
                if (_bindingsObject is Object obj)
                {
                    _serializedObject = new SerializedObject(obj);
                    _bindingsProperty = _serializedObject.FindProperty("bindings");
                }

                _allBindings = GetAllBindingsForSource();
                _inOrderBindings = InOrderBindings;
                
                if (_lastRoot != null && !avoidAttach)
                {
                    AttachBindings(_lastRoot);
                }
            }

            public void AttachBindings(VisualElement root)
            {
                _lastRoot = root;
                if (_bindingsObject == null && GetProxyBindings(source, createIfMissing: false) as Object == null)
                {
                    return;
                }
                root.schedule.Execute(() =>
                {
                    rootViews.Clear();
                    foreach (var proxyView in proxyViewsByPath.Values)
                    {
                        proxyView?.RemoveFromHierarchy();
                    }
                    proxyViewsByPath.Clear();
                    InitializeIfNeeded(avoidAttach: true);
                    ProcessRootPropertyFields(root, ProcessProperty);
                    ProcessIMGUIProperties(root, overwrite: true);
                    ProcessMaterialProperties(root, overwrite: true);
                });
            }
            
            public void ReattachBindings(VisualElement root)
            {
                _lastRoot = root;
                if (_bindingsObject == null && GetProxyBindings(source, createIfMissing: false) as Object == null)
                {
                    return;
                }
                root.schedule.Execute(() =>
                {
                    InitializeIfNeeded();
                    ProcessRootPropertyFields(root, ProcessPropertyFast);
                    ProcessIMGUIProperties(root, overwrite: false);
                    ProcessMaterialProperties(root, overwrite: false);
                });
            }

            private void ProcessRootPropertyFields(VisualElement root, Action<PropertyField, SerializedProperty> processor)
            {
                var propertyFields = root.Query<PropertyField>()
                    .Where(f => f.name.StartsWith("PropertyField:", StringComparison.Ordinal))
                    .Build();
                foreach (var propertyField in propertyFields)
                {
                    propertyField.OnBind(processor);
                }
            }

            private void ProcessIMGUIProperties(VisualElement root, bool overwrite)
            {
                var inspectorsViews = root.Q(null, "unity-inspector-editors-list");
                foreach (var editor in inspector.Tracker.activeEditors)
                {
                    ProcessIMGUIEditor(editor, inspectorsViews, overwrite);
                }
            }

            private void ProcessIMGUIEditor(Editor editor, VisualElement inspectorsViews, bool overwrite)
            {
                var serializedObject = editor.serializedObject;
                if (serializedObject.targetObject is AssetImporter)
                {
                    // We do not want to bind to asset importers
                    return;
                }

                var rootView = GetRootView(serializedObject.targetObject);
                if (rootView == null)
                {
                    return;
                }

                using var iterator = serializedObject.GetIterator();
                
                if (iterator.propertyPath != "m_Script")
                {
                    iterator.Next(true);
                }
                
                while (iterator.Next(false))
                {
                    if(rootViews.ContainsKey((serializedObject.targetObject, iterator.propertyPath)))
                    {
                        continue;
                    }
                    ProcessProperty(rootView, iterator.Copy(), overwrite);
                }

                return;

                VisualElement GetRootView(Object target)
                {
                    if (!target)
                    {
                        return null;
                    }
                    
                    if(target is Component cTarget && _rootTypeReplacements.TryGetValue(cTarget.GetType().FullName, out var replacementType))
                    {
                        target = cTarget.GetComponent(replacementType);
                    }
                    var key = $"_{target.GetType().Name}_{target.GetInstanceID()}";
                    foreach (var view in inspectorsViews.Children())
                    {
                        if (!view.name.EndsWith(key, StringComparison.Ordinal)) continue;
                        
                        foreach (var child in view.Children())
                        {
                            if (child.ClassListContains("unity-inspector-element--imgui"))
                            {
                                return child;
                            }
                        }
                        return view;
                    }

                    return null;
                }
            }
            
            private void ProcessMaterialProperties(VisualElement root, bool overwrite)
            {
                var inspectorsViews = root.Q(null, "unity-inspector-editors-list");
                foreach (var editor in inspector.Tracker.activeEditors)
                {
                    ProcessMaterialEditor(editor, inspectorsViews, overwrite);
                }
            }

            private void ProcessMaterialEditor(Editor editor, VisualElement inspectorsViews, bool overwrite)
            {
                if (editor is not MaterialEditor materialEditor)
                {
                    // We want to bind only Material Editors
                    return;
                }

                var rootView = GetRootView(editor.serializedObject.targetObject);
                if (rootView == null)
                {
                    return;
                }

                var material = materialEditor.target as Material;
                if (material == null)
                {
                    return;
                }
                
                var materialSource = source switch
                {
                    Renderer r => r,
                    GameObject go => go.GetComponent<Renderer>(),
                    Component c => c.GetComponent<Renderer>(),
                    _ => source
                };
                
                var prefix = GetPathPrefix(materialSource, material);
                rootViews[(materialSource, prefix.TrimEnd('.'))] = rootView;
                
                // Process all properties
                var properties = MaterialEditor.GetMaterialProperties(new Object[] { material });
                foreach (var property in properties)
                {
                    ProcessProperty(materialSource, rootView, property, null, overwrite);
#if UNITY_6000_2_OR_NEWER
                    if (property.propertyType == ShaderPropertyType.Texture)
#else
                    if (property.type == MaterialProperty.PropType.Texture)
#endif
                    {
                        ProcessProperty(materialSource, rootView, property, DrawerSystem.MaterialEditor.TilingText.text, overwrite);
                        ProcessProperty(materialSource, rootView, property, DrawerSystem.MaterialEditor.OffsetText.text, overwrite);
                    }
                }

                return;

                VisualElement GetRootView(Object target)
                {
                    var key = $"_{target.GetType().Name}_{target.GetInstanceID()}";
                    VisualElement candidate = null;
                    foreach (var view in inspectorsViews.Children())
                    {
                        if (!view.name.EndsWith(key, StringComparison.Ordinal)) continue;
                        
                        foreach (var child in view.Children())
                        {
                            if (child.ClassListContains("unity-inspector-element--imgui"))
                            {
                                return child;
                            }
                        }
                        candidate = view;
                    }

                    return candidate;
                }
                
            }

            public void Refresh(bool isUndoRedoOperation)
            {
                InitializeIfNeeded();
                
                var newAllBindings = GetAllBindingsForSource();

                var removedBindings = _allBindings.Except(newAllBindings).ToList();
                var addedBindings = newAllBindings.Except(_allBindings).ToList();
                
                if(removedBindings.Count == 0 && addedBindings.Count == 0)
                {
                    _allBindings = newAllBindings;
                    _inOrderBindings = null;
                    return;
                }
                
                foreach (var bindProxy in removedBindings)
                {
                    RemoveBinding(bindProxy.Source, bindProxy.Path, isUndoRedoOperation);
                }
                
                foreach (var bindProxy in addedBindings)
                {
                    AddBinding(bindProxy, isUndoRedoOperation);
                }

                _allBindings = newAllBindings;
                _inOrderBindings = null;
            }

            private List<BindProxy> GetAllBindingsForSource()
            {
                if (_bindingsObject == null)
                {
                    return new List<BindProxy>();
                }
                var allBindings = new HashSet<BindProxy>(_bindingsObject.GetProxies(source));
                if (source is GameObject go && go)
                {
                    foreach (var component in go.GetComponents<Component>())
                    {
                        foreach(var bindProxy in _bindingsObject.GetProxies(component))
                        {
                            allBindings.Add(bindProxy);
                        }
                    }
                }

                return allBindings.ToList();
            }

            private void ProcessProperty(PropertyField field, SerializedProperty property)
                => ProcessProperty(field, property, overwrite: true);
            
            private void ProcessPropertyFast(PropertyField field, SerializedProperty property)
                => ProcessProperty(field, property, overwrite: false);
            
            private void ProcessProperty(VisualElement rootView, SerializedProperty property, bool overwrite)
            {
                if (rootView is not PropertyField)
                {
                    var propertyField = rootView.Q<PropertyField>("PropertyField:" + property.propertyPath) 
                                        ?? rootView.Query<VisualElement>().Where(p => p is IBindable bindable && bindable.bindingPath == property.propertyPath).First();
                    if (propertyField != null)
                    {
                        rootView = propertyField;
                    }
                }
                rootViews[(property.serializedObject.targetObject, property.propertyPath)] = rootView;
                
                if (!_bindingsObject.TryGetProxiesInTree(property.serializedObject.targetObject, property.propertyPath, out var list))
                {
                    return;
                }

                if (BindingsProperty == null)
                {
                    // Have to do something about it
                    return;
                }

                foreach (var (proxy, index) in list)
                {
                    if (!overwrite && proxyViewsByPath.TryGetValue((proxy.Source, proxy.Path), out var view) && view.panel != null)
                    {
                        continue;
                    }
                    
                    var proxyProperty = BindingsProperty.GetArrayElementAtIndex(index);

                    var proxyView = new BindProxyDrawer.BindProxyView(rootView, proxyProperty, null, false);
                    proxyView.AttachToRoot(rootView, proxy.Path?.Contains("Array.data") ?? false);
                    proxyView.OnTargetRemoved += TargetRemoved;
                
                    void TargetRemoved()
                    {
                        if(proxyView == null)
                        {
                            return;
                        }
                        proxyView.OnTargetRemoved -= TargetRemoved;
                        if (!Undo.isProcessing)
                        {
                            _undo.RegisterCompleteObjectUndo(_bindingsObject as Object, DisableBindingText);
                        }

                        RemoveBinding(proxy.Source, proxy.Path);
                        proxyView?.Dispose();
                        proxyViewsByPath.Remove((proxy.Source, proxy.Path));
                    }
                
                    proxyViewsByPath[(proxy.Source, proxy.Path)] = proxyView;
                }
            }

            private void ProcessProperty(Object materialSource, VisualElement rootView, MaterialProperty property, string suffixText, bool overwrite = true)
            {
                var suffix = GetMaterialPropertyName(property, suffixText);
                var prefix = GetPathPrefix(source, property.targets[0] as Material);
                var path = prefix + suffix;
                
                rootViews[(materialSource, path)] = rootView;
                
                if (!_bindingsObject.TryGetProxy(materialSource, path, out _, out var index))
                {
                    return;
                }

                if (!overwrite && proxyViewsByPath.TryGetValue((materialSource, path), out var view) && view.panel != null)
                {
                    return;
                }
                
                var proxyProperty = BindingsProperty.GetArrayElementAtIndex(index);
                
                var proxyView = new BindProxyDrawer.BindProxyView(rootView, proxyProperty, null, true);
                proxyView.AttachToRoot(rootView, false);
                proxyView.OnTargetRemoved += TargetRemoved;
                
                void TargetRemoved()
                {
                    if(proxyView == null)
                    {
                        return;
                    }
                    proxyView.OnTargetRemoved -= TargetRemoved;
                    if (!Undo.isProcessing)
                    {
                        _undo.RegisterCompleteObjectUndo(_bindingsObject as Object, DisableBindingText);
                    }
                    RemoveBinding(materialSource, path);
                    proxyView?.Dispose();
                    proxyViewsByPath.Remove((materialSource, path));
                }
                
                proxyViewsByPath[(materialSource, path)] = proxyView;
            }

            public bool AddBinding(BindProxy proxy, bool isUndoRedoOperation = false)
            {
                BindingsObject.AddProxy(proxy);

                if (!_allBindings.Contains(proxy))
                {
                    _allBindings.Add(proxy);
                }
                
                if(!InOrderBindings.Contains(proxy))
                {
                    InOrderBindings.Add(proxy);
                }

                VisualElement rootView = null;

                if (proxy.OptionsValue.HasFlag(BindProxy.Options.MaterialProperty))
                {
                    var indexOfDot = proxy.Path.LastIndexOf('.');
                    var firstPart = indexOfDot < 0 ? proxy.Path : proxy.Path[..indexOfDot];
                    if (!rootViews.TryGetValue((proxy.Source, firstPart), out rootView))
                    {
                        return false;
                    }
                }
                else
                {
                    var indexOfDot = proxy.Path.IndexOf('.');
                    var firstPart = indexOfDot < 0 ? proxy.Path : proxy.Path[..indexOfDot];
                    if (!rootViews.TryGetValue((proxy.Source, firstPart), out rootView))
                    {
                        return false;
                    }
                }

                BindingsProperty.serializedObject.Update();
                
                var index = BindingsProperty.arraySize - 1;
                var proxyProperty = BindingsProperty.GetArrayElementAtIndex(index);
                var isMaterialProperty = proxy.OptionsValue.HasFlag(BindProxy.Options.MaterialProperty);
                
                var proxyView = new BindProxyDrawer.BindProxyView(rootView, proxyProperty, null, isMaterialProperty);
                proxyView.AttachToRoot(rootView, isUndoRedoOperation);
                proxyViewsByPath[(proxy.Source, proxy.Path)] = proxyView;
                
                proxyView.OnTargetRemoved += TargetRemoved;
                
                void TargetRemoved()
                {
                    if(proxyView == null)
                    {
                        return;
                    }
                    proxyView.OnTargetRemoved -= TargetRemoved;
                    _undo.Record();
                    RemoveBinding(proxy.Source, proxy.Path);
                    proxyView?.Dispose();
                    proxyViewsByPath.Remove((proxy.Source, proxy.Path));
                }
                
                return true;
            }

            public bool RemoveBinding(Object sourceObj, string path, bool isUndoRedoOperation = false)
            {
                BindingsObject.RemoveProxy(sourceObj, path);
                _allBindings.RemoveAll(p => p.Source == sourceObj && p.Path == path);

                // Update all views with their respective indexes
                var indexOfRemoved = InOrderBindings.FindIndex(p => p.Source == sourceObj && p.Path == path);
                if (indexOfRemoved < 0)
                {
                    return false;
                }

                InOrderBindings.RemoveAll(p => p.Source == sourceObj && p.Path == path);
                
                if (!proxyViewsByPath.TryGetValue((sourceObj, path), out var view)) return false;

                view.OnRemoveFromList();
                view.Dispose();
                proxyViewsByPath.Remove((sourceObj, path));

                if (BindingsProperty.serializedObject.targetObject)
                {
                    BindingsProperty.serializedObject.Update();
                }

                // Get at least one bound view and reset it.
                // If none is found, it's fine since there is nothing to reset then.
                foreach (var proxyView in proxyViewsByPath.Values)
                {
                    var bindDataUI = proxyView.Q<BindDataDrawer.BindDataUI>();
                    if(bindDataUI != null)
                    {
                        bindDataUI.Reset();
                        break;
                    }
                }
                
                for (var i = indexOfRemoved; i < InOrderBindings.Count; i++)
                {
                    var bindProxy = InOrderBindings[i];
                    if (!proxyViewsByPath.TryGetValue((bindProxy.Source, bindProxy.Path), out var proxyView))
                    {
                        continue;
                    }
                    if(i >= BindingsProperty.arraySize)
                    {
                        proxyViewsByPath.Remove((bindProxy.Source, bindProxy.Path));
                        proxyView.Dispose();
                        continue;
                    }
                    proxyView.UpdatePropertyField(BindingsProperty.GetArrayElementAtIndex(i));
                }
                
                
                return true;
            }

            public void Dispose()
            {
                _serializedObject?.Dispose();
                foreach (var view in proxyViewsByPath.Values.ToArray())
                {
                    view?.Dispose();
                }

                DestroyBindingsObject(false);
            }

            public void DestroyBindingsObject(bool withUndo = true)
            {
                if (_bindingsObject is Component obj && obj && _bindingsObject.IsEmpty)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(obj);
                    }
                    else if (withUndo)
                    {
                        _undo.DestroyObjectImmediate(obj);
                    }
                    else
                    {
                        Object.DestroyImmediate(obj, true);
                    }
                }
                
                _bindingsObject = null;
                _bindingsProperty?.Dispose();
                _serializedObject?.Dispose();
                _serializedObject = null;
                _bindingsProperty = null;
            }
        }

        private class UndoManager
        {
            public SortedList<int, UndoRecord> records = new();
            
            public bool isProcessing => Undo.isProcessing;

            public void Record()
            {
                records[Undo.GetCurrentGroup()] = new UndoRecord()
                {
                    groupId = Undo.GetCurrentGroup()
                };
            }
            
            public bool IsValidUndoOrRedo(in UndoRedoInfo undo)
            {
                var groupIndex = undo.isRedo ? undo.undoGroup : undo.undoGroup;
                return records.ContainsKey(groupIndex);
            }

            public void DestroyObjectImmediate(Object obj)
            {
                Undo.DestroyObjectImmediate(obj);
                Record();
            }
            
            public void RegisterCompleteObjectUndo(Object obj, string operation)
            {
                if (obj == null)
                {
                    return;
                }
                
                Undo.RegisterCompleteObjectUndo(obj, operation);
                Record();
            }

            public void IncrementCurrentGroup()
            {
                Undo.IncrementCurrentGroup();
                // Record();
            }

            public void RegisterCreatedObjectUndo(Object obj, string operation)
            {
                if (obj == null)
                {
                    return;
                }
                
                Undo.RegisterCreatedObjectUndo(obj, operation);
                Record();
            }

            public T AddComponent<T>(GameObject go, string operation) where T : Component
            {
                if (go == null)
                {
                    return null;
                }
                
                var component = Undo.AddComponent<T>(go);

                Record();
                return component;
            }
        }
        
        private struct UndoRecord
        {
            public int groupId;

            public void Update()
            {
                groupId = Undo.GetCurrentGroup();
            }
        }
    }
}