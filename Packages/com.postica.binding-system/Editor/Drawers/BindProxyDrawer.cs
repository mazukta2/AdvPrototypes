using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoHook;
using Postica.BindingSystem.ProxyBinding;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using Postica.Common;
using Postica.Common.Reflection;
using UnityEditor.Callbacks;
using UnityEditorInternal;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(BindProxy))]
    internal class BindProxyDrawer : StackedPropertyDrawer
    {
        
        public static Action<BindProxyDrawer, BindProxyView> OnInitializeUIViews;

        private struct Properties
        {
            public SerializedProperty property;
            public SerializedProperty source;
            public SerializedProperty path;
            public SerializedProperty bindData;
            public SerializedProperty isBound;
            public SerializedProperty options;
            
            public GUIContent label;
            public MixedValueProperty isBoundValue;

            public Properties(SerializedProperty prop, GUIContent label)
            { 
                property = prop.Copy();
                bindData = property.FindPropertyRelative("_bindData");
                source = property.FindPropertyRelative("_proxySource");
                path = property.FindPropertyRelative("_proxyPath");
                isBound = property.FindPropertyRelative("_isBound");
                options = property.FindPropertyRelative("_options");
                
                this.label = label;

                isBoundValue = isBound != null 
                    ? new MixedValueProperty(property.serializedObject, isBound.propertyPath)
                    : null;
                
                UpdateMetaValues(true, out _);
            }
            
            public void UpdateMetaValues(bool updateSerializedObject, out bool targetsChanged)
            { 
                targetsChanged = false;
                if (bindData == null)
                {
                    return;
                }

                var pPath = bindData.FindPropertyRelative("_ppath");
                var context = bindData.FindPropertyRelative("_context");

                if (!string.IsNullOrEmpty(bindData.propertyPath))
                {
                    pPath.stringValue = bindData.propertyPath;
                }

                var targets = property.serializedObject.targetObjects;
                if (targets.Length == 1)
                {
                    if (context.objectReferenceValue != targets[0])
                    {
                        context.objectReferenceValue = targets[0];
                        if (updateSerializedObject)
                        {
                            targetsChanged = property.serializedObject.ApplyModifiedProperties();
                        }
                    }

                    return;
                }

                var shouldApply = false;
                foreach (var target in targets)
                {
                    using (var serObj = new SerializedObject(target))
                    {
                        var prop = serObj.FindProperty(context.propertyPath);
                        prop.objectReferenceValue = target;
                        shouldApply |= serObj.ApplyModifiedProperties();
                    }
                }

                if (shouldApply && updateSerializedObject)
                {
                    targetsChanged = true;
                    property.serializedObject.Update();
                }
            }
        }

        // UI Toolkit -----------------------------------------------------------
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var percent50 = new StyleLength(new Length(50, LengthUnit.Percent));
            var sourceField = new PropertyField().EnsureBind(property.FindPropertyRelative("_proxySource"),f => f.WithLabel(null));
            var pathField = new PropertyField().EnsureBind(property.FindPropertyRelative("_proxyPath"), f => f.WithLabel(null));
            var isBoundField = new PropertyField {tooltip = "Is Bound?"}.EnsureBind(property.FindPropertyRelative("_isBound"), f => f.WithLabel(null).WithStyle(
                s =>
                {
                    s.alignSelf = Align.Center;
                    s.marginTop = 2;
                    s.marginRight = 6;
                }));
            var bindDataField = new PropertyField().EnsureBind(property.FindPropertyRelative("_bindData"));
            var view = new VisualElement()
                .WithChildren(
                    new VisualElement().Horizontal().WithChildren(
                        sourceField.WithStyle(s => s.width = percent50),
                        pathField.WithStyle(s => s.width = percent50)), 
                    new VisualElement().Horizontal().WithChildren(
                        isBoundField, 
                        bindDataField.WithStyle(s => s.flexGrow = 1)));
            return view;
        }
        
        // MATERIAL PROPERTIES HANDLER -------------------------------------------
        private static class EditorGUIHelper
        {
            private const string ReinstallHooksKey = "__BS_REINSTALL_EDITORGUIHELPER_HOOKS";
            private static MethodHook _handlePrefixLabelHook;

            public struct PrefixLabel
            {
                public bool ready;
                public int frame;
                public GUIContent label;
            }
            
            private static int _nextCleanupFrame = -1;
            private static readonly Dictionary<Rect, PrefixLabel> _capturedPrefixLabels = new();
            
            public static bool TryGetCapturedPrefixLabel(Rect totalPosition, out PrefixLabel label)
            {
                if(_capturedPrefixLabels.TryGetValue(totalPosition, out label) && label.frame >= Time.frameCount - 2)
                {
                    label.frame = Time.frameCount;
                    return label.ready;
                }

                _capturedPrefixLabels[totalPosition] = new PrefixLabel
                {
                    frame = Time.frameCount
                };
                
                TryCleanup();
                
                label = default;
                return false;
            }

            public static void TryCleanup()
            {
                if(_nextCleanupFrame < Time.frameCount)
                {
                    _nextCleanupFrame = Time.frameCount + 180; // Cleanup every 60 frames
                    _capturedPrefixLabels.RemoveAll(l => l.frame < Time.frameCount - 2);
                }
            }

            [InitializeOnLoadMethod]
            [DidReloadScripts]
            internal static void ReinstallHooksIfNeeded()
            {
                if (!PlayerPrefs.HasKey(ReinstallHooksKey))
                {
                    return;
                }
                EnsureHooked();
            }

            private static void UninstallHooksBeforeAssemblyReload()
            {
                PlayerPrefs.SetInt(ReinstallHooksKey, 1);
                PlayerPrefs.Save();
                UninstallHooks();
            }
            
            public static void UninstallHooks()
            {
                if (_handlePrefixLabelHook != null)
                {
                    _handlePrefixLabelHook.Uninstall();
                    _handlePrefixLabelHook = null;
                }
                
                _capturedPrefixLabels.Clear();
            }

            public static void EnsureHooked()
            {
                if (_handlePrefixLabelHook != null)
                {
                    return;
                }
                
                PlayerPrefs.DeleteKey(ReinstallHooksKey);
                PlayerPrefs.Save();
                
                var original = typeof(EditorGUI).GetVoidMethod<Rect, Rect, GUIContent, int>(nameof(EditorGUI.HandlePrefixLabel));
                var replacement = typeof(EditorGUIHelper).GetVoidMethod<Rect, Rect, GUIContent, int>(nameof(PrefixLabelOverride));
                var proxy = typeof(EditorGUIHelper).GetVoidMethod<Rect, Rect, GUIContent, int>(nameof(BasePrefixLabel));
                
                AssemblyReloadEvents.beforeAssemblyReload -= UninstallHooksBeforeAssemblyReload;
                AssemblyReloadEvents.beforeAssemblyReload += UninstallHooksBeforeAssemblyReload;
                
                _handlePrefixLabelHook = new MethodHook(original, replacement, proxy);
                _handlePrefixLabelHook.Install();
            }
            
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void PrefixLabelOverride(Rect totalPosition,
                Rect labelPosition,
                GUIContent label,
                int id)
            {
                if (_capturedPrefixLabels.TryGetValue(totalPosition, out var prefixLabel))
                {
                    if (prefixLabel.frame < Time.frameCount - 2)
                    {
                        // Remove it, it is stale
                        _capturedPrefixLabels.Remove(totalPosition);
                    }
                    else
                    {
                        prefixLabel = new PrefixLabel
                        {
                            ready = true,
                            frame = Time.frameCount,
                            // controlId = id,
                            // totalPosition = totalPosition,
                            // labelPosition = labelPosition,
                            label = label
                        };
                        _capturedPrefixLabels[totalPosition] = prefixLabel;
                    }
                }
                
                BasePrefixLabel(totalPosition, labelPosition, label, id);
            }
            
            [MethodImpl(MethodImplOptions.NoOptimization)]
            private static void BasePrefixLabel(Rect totalPosition,
                Rect labelPosition,
                GUIContent label,
                int id)
            {
                Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
            }
        }
        
        // MATERIAL PROPERTIES HANDLER -------------------------------------------
        private static class MaterialProxyDrawer
        {
            private const string ReinstallHooksKey = "__BS_REINSTALL_MATERIAL_HOOKS";
            private static MethodHook _beginPropertyHook;
            private static MethodHook _endPropertyHook;
            private static MethodHook _prefixLabelHook;
            private static readonly Dictionary<(Material material, string property), (BindProxyView view, MaterialDrawer drawer)> _views = new();
            private static readonly Stack<PropertyData> _currentProperties = new();
            private static Rect _lastPosition;
            
            
            private class PropertyData
            {
                public MaterialProperty property;
                public Rect position;
                public List<(Rect position, string label)> labels;
            }
            
            [InitializeOnLoadMethod]
            [DidReloadScripts]
            internal static void ReinstallHooksIfNeeded()
            {
                if (!PlayerPrefs.HasKey(ReinstallHooksKey))
                {
                    return;
                }
                EnsureHooked();
            }

            private class MaterialDrawer : StackedMaterialPropertyDrawer
            {
                private MaterialProperty _property;
                
                public MaterialProperty Property => _property;

                public MaterialDrawer(MaterialProperty property)
                {
                    _property = property;
                }
                
                public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
                {
                    if (_views.Count == 0 || _currentProperties.Count == 0)
                    {
                        return base.GetPropertyHeight(prop, label, editor);
                    }
                
                    var propertyId = GetPropertyId(prop.name, label);
                    if (!_views.TryGetValue((prop.targets[0] as Material, propertyId), out var viewPair))
                    {
                        return base.GetPropertyHeight(prop, label, editor);
                    }
                
                    viewPair.view.imguiData.lastFrame = Time.frameCount;
                    
                    var view = viewPair.view;
                    if (!view.IsBound || view.layout.height <= 1)
                    {
                        return base.GetPropertyHeight(prop, label, editor);
                    }
                
                    return view.layout.height;
                }
                
                public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
                {
                    base.OnGUI(position, prop, label, editor);
                    
                    if (_views.Count == 0)
                    {
                        return;
                    }
                
                    if(Event.current.type != EventType.Repaint)
                    {
                        return;
                    }
                
                    if (_currentProperties.Count == 0)
                    {
                        return;
                    }
                
                    var data = _currentProperties.Peek();
                    if (data.property.name == prop.name)
                    {
                        data.position = position;
                        data.labels ??= new();
                        data.labels.Add((position, label.text));
                    }
                }
            }

            public static void RegisterView(Material material, string propertyName, string propertyType, BindProxyView view)
            {
                EnsureHooked();
                var propertyId = GetPropertyId(propertyName, propertyType);

                var property = MaterialEditor.GetMaterialProperty(new Object[] { material }, propertyName);
                
                var drawer = new MaterialDrawer(property);
                DrawerSystem.SetMaterialPropertyDrawer(material, propertyName, drawer);
                _views[(material, propertyId)] = (view, drawer);
            }

            private static string GetPropertyId(string propertyName, string propertyType)
            {
                var propertyId = propertyName;
                if (propertyType.Equals(DrawerSystem.MaterialEditor.TilingText.text, StringComparison.OrdinalIgnoreCase))
                {
                    propertyId += '-' + DrawerSystem.MaterialEditor.TilingText.text.ToLower();
                }
                else if (propertyType.Equals(DrawerSystem.MaterialEditor.OffsetText.text, StringComparison.OrdinalIgnoreCase))
                {
                    propertyId += '-' + DrawerSystem.MaterialEditor.OffsetText.text.ToLower();
                }

                return propertyId;
            }

            public static void UnregisterView(Material material, string propertyName)
            {
                var propertyId = GetPropertyId(propertyName, null);
                
                if (!_views.TryGetValue((material, propertyId), out var viewPair)) return;
                
                viewPair.view.Dispose();
                _views.Remove((material, propertyId));
            }

            public static void UnregisterView(BindProxyView view, bool dispose = false)
            {
                var keys = new List<(Material, string)>();
                foreach (var (key, value) in _views)
                {
                    if (value.view == view)
                    {
                        keys.Add(key);
                    }
                }
                
                foreach (var key in keys)
                {
                    var drawer = _views[key].drawer;
                    if (drawer != null)
                    {
                        DrawerSystem.RestoreMaterialPropertyDrawer(drawer.Property.targets[0] as Material, drawer.Property.name);
                    }
                    _views.Remove(key);
                }
                
                if(dispose)
                {
                    view.Dispose();
                }
                
                if (_views.Count == 0)
                {
                    UninstallHooks();
                }
            }

            private static void UninstallHooksBeforeAssemblyReload()
            {
                PlayerPrefs.SetInt(ReinstallHooksKey, 1);
                PlayerPrefs.Save();
                UninstallHooks();
            }
            
            private static void UninstallHooks()
            {
                if (_beginPropertyHook != null)
                {
                    _beginPropertyHook.Uninstall();
                    _beginPropertyHook = null;
                }
                if (_endPropertyHook != null)
                {
                    _endPropertyHook.Uninstall();
                    _endPropertyHook = null;
                }
                if (_prefixLabelHook != null)
                {
                    _prefixLabelHook.Uninstall();
                    _prefixLabelHook = null;
                }
            }

            private static void EnsureHooked()
            {
                if (_beginPropertyHook != null)
                {
                    return;
                }
                
                PlayerPrefs.DeleteKey(ReinstallHooksKey);
                PlayerPrefs.Save();

                var original = typeof(MaterialEditor).GetVoidMethod<Rect, MaterialProperty>(nameof(MaterialEditor.BeginProperty));
                var replacement = typeof(MaterialProxyDrawer).GetVoidMethod<Rect, MaterialProperty>(nameof(BeginPropertyOverride));
                var proxy = typeof(MaterialProxyDrawer).GetVoidMethod<Rect, MaterialProperty>(nameof(BaseBeginProperty));
                
                _beginPropertyHook = new MethodHook(original, replacement, proxy);
                _beginPropertyHook.Install();
                
                // Do the same for EndProcedure
                original = typeof(MaterialEditor).GetVoidMethod(nameof(MaterialEditor.EndProperty));
                replacement = typeof(MaterialProxyDrawer).GetVoidMethod(nameof(EndPropertyOverride));
                proxy = typeof(MaterialProxyDrawer).GetVoidMethod(nameof(BaseEndProperty));
                
                _endPropertyHook = new MethodHook(original, replacement, proxy);
                _endPropertyHook.Install();
                
                original = typeof(EditorGUI).GetReturnMethod<Rect, GUIContent, Rect>(nameof(EditorGUI.PrefixLabel));
                replacement = typeof(MaterialProxyDrawer).GetReturnMethod<Rect, GUIContent, Rect>(nameof(PrefixLabelOverride));
                proxy = typeof(MaterialProxyDrawer).GetReturnMethod<Rect, GUIContent, Rect>(nameof(BasePrefixLabel));
                
                AssemblyReloadEvents.beforeAssemblyReload -= UninstallHooksBeforeAssemblyReload;
                AssemblyReloadEvents.beforeAssemblyReload += UninstallHooksBeforeAssemblyReload;
                
                _prefixLabelHook = new MethodHook(original, replacement, proxy);
                _prefixLabelHook.Install();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void BeginPropertyOverride(Rect position, MaterialProperty property)
            {
                BaseBeginProperty(position, property);
                if (_views.Count == 0 || property.targets.Length != 1)
                {
                    return;
                }
                
                if(Event.current.type != EventType.Repaint)
                {
                    return;
                }
                
                
                _lastPosition = Rect.zero;
                
                var propertyId = GetPropertyId(property.name, "");
                var propertyIdTiling = GetPropertyId(property.name, DrawerSystem.MaterialEditor.TilingText.text);
                var propertyIdOffset = GetPropertyId(property.name, DrawerSystem.MaterialEditor.OffsetText.text);
                if (!_views.TryGetValue((property.targets[0] as Material, propertyId), out var viewPair)
                    && !_views.TryGetValue((property.targets[0] as Material, propertyIdTiling), out viewPair)
                    && !_views.TryGetValue((property.targets[0] as Material, propertyIdOffset), out viewPair))
                {
                    return;
                }
                
                _currentProperties.Push(new PropertyData
                {
                    property = property,
                    position = position,
                });
                
                if (viewPair.view.IsBound && position.Contains(Event.current.mousePosition))
                {
                    Event.current.mousePosition = new Vector2(-1000, -1000);
                }
                
                viewPair.view.imguiData.lastFrame = Time.frameCount;
                viewPair.view.imguiData.lastUpdate = Time.realtimeSinceStartup;
            }
            
            [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
            private static void BaseBeginProperty(Rect position, MaterialProperty property)
            {
                Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
            }
            
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void EndPropertyOverride()
            {
                BaseEndProperty();
                if (_views.Count == 0)
                {
                    return;
                }
                
                if(Event.current.type != EventType.Repaint)
                {
                    return;
                }
                
                if (_currentProperties.Count == 0)
                {
                    return;
                }
                
                var data = _currentProperties.Pop();
                var property = data.property;

                if (data.labels == null)
                {
                    var propertyId = GetPropertyId(property.name, "");

                    if (!_views.TryGetValue((property.targets[0] as Material, propertyId), out var viewPair))
                    {
                        return;
                    }

                    var position = data.position;
                    if (position.Overlaps(_lastPosition))
                    {
                        if (position.x > _lastPosition.x)
                        {
                            position.width -= position.x - _lastPosition.x;
                            position.x = _lastPosition.xMax + 15f;
                        }
                        else
                        {
                            position.width -= _lastPosition.x - position.x;
                            position.xMax = _lastPosition.x - 15f;
                        }
                    }
                    _lastPosition = position;
                    
                    var view = viewPair.view;
                    
                    view.AdaptToMaterialPropertyRect(position, -15f);

                    if (!view.IsBound) return;
                    
                    view.TrySetLabel(property.displayName);

                    return;
                }

                foreach (var (position, label) in data.labels)
                {
                    var propertyId = GetPropertyId(property.name, label);

                    if (!_views.TryGetValue((property.targets[0] as Material, propertyId), out var viewPair))
                    {
                        continue;
                    }

                    var totalPosition = position;
                    totalPosition.width = Mathf.Max(position.width, data.position.width);
                    
                    if (totalPosition != _lastPosition && totalPosition.Overlaps(_lastPosition))
                    {
                        if (totalPosition.x > _lastPosition.x)
                        {
                            totalPosition.width -= totalPosition.x - _lastPosition.x;
                            totalPosition.x = _lastPosition.xMax + 15f;
                        }
                        else
                        {
                            totalPosition.width -= _lastPosition.x - totalPosition.x;
                            totalPosition.x = totalPosition.xMax - 15f;
                        }
                    }
                    _lastPosition = totalPosition;
                    
                    var view = viewPair.view;
                    view.AdaptToMaterialPropertyRect(totalPosition, -15f);

                    if (!view.IsBound) continue;
                    
                    view.TrySetIMGUILabel(label);
                }
            }
            
            [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
            private static void BaseEndProperty()
            {
                Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Rect PrefixLabelOverride(Rect totalPosition, GUIContent label)
            {
                if (_currentProperties.Count > 0)
                {
                    var labels = _currentProperties.Peek().labels ??= new();
                    labels.Add((totalPosition, label.text));
                }
                return BasePrefixLabel(totalPosition, label);
            }
            
            [MethodImpl(MethodImplOptions.NoOptimization)]
            private static Rect BasePrefixLabel(Rect totalPosition, GUIContent label)
            {
                Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
                return totalPosition;
            }
        }
        
        // BIND PROXIES IMGUI HANDLER ------------------------------------------------
        internal class BindProxyIMGUIDrawer : PropertyDrawer
        {
            private SerializedProperty _idProperty;
            private Object _targetObject;
            private string _path;
            private Dictionary<(Object source, string path), DrawData> _propertiesToDraw
                = new();

            private class DrawData
            {
                public VisualElement root;
                public BindProxyView view;
                public GUIContent label;
                public float initialX;
                public bool requiresLabelShift;
                public bool isEnabledProperty;
                public string firstListAncestorPath;
                public ReorderableList list;
                
                public DrawData(SerializedProperty property, VisualElement root, BindProxyView view)
                {
                    this.view = view;
                    this.root = root;
                    label = new GUIContent(property.displayName);
                    requiresLabelShift = property.ShouldShiftLabel();
                    initialX = view.parent is PropertyField field && field.name?.EndsWith(property.propertyPath) == true 
                        ? 15 
                        : property.propertyPath.EndsWith(']') 
                        ? 3
                        : 0;
                    var parentProperty = property.GetParent();
                    var isPartOfReorderableList = false;
                    while (parentProperty != null && !isPartOfReorderableList)
                    {
                        isPartOfReorderableList = DrawerSystem.PropertyHandler.UseReorderabelListControl(parentProperty);
                        if(isPartOfReorderableList)
                        {
                            firstListAncestorPath = parentProperty.propertyPath;
                            break;
                        }
                        parentProperty = parentProperty.GetParent();
                    }
                    
                    list = null;

                    isEnabledProperty = property.propertyPath == "m_Enabled"
                        || (property.propertyPath == "m_IsActive" && property.serializedObject.targetObject is GameObject);
                    if (isEnabledProperty)
                    {
                        if(view.parent is InspectorElement)
                        {
                            view.RemoveFromHierarchy();
                            this.root = root.parent;
                            this.root.Add(view);
                        }
                        view.IsSpecialBind = true;
                    }
                }
            }
            
            public static BindProxyIMGUIDrawer GetIMGUIDrawer(SerializedProperty property)
            {
                BindProxyIMGUIDrawer drawer = null;

                BindProxyIMGUIDrawer GetDrawer()
                {
                    if (drawer == null)
                    {
                        drawer = new BindProxyIMGUIDrawer();
                        drawer._idProperty = property.Copy();
                        drawer._targetObject = property.serializedObject.targetObject;
                        drawer._path = property.propertyPath;
                    }

                    return drawer;
                }
                
                var editors = ProxyBindingSystem.GetEditorsOf(property.serializedObject.targetObject);
                foreach (var editor in editors)
                {
                    DrawerSystem.EditorProxy editorProxy = editor;
                    var cache = editorProxy.propertyHandlerCache;
                    if (cache == null)
                    {
                        continue;
                    }

                    var handler = cache.GetHandler(property);
                    if (handler == null || handler.Instance == DrawerSystem.ScriptAttributeUtility.SharedNullHandler.Instance)
                    {
                        handler = new DrawerSystem.PropertyHandlerProxy();
                        handler.NewInstance();
                        cache.SetHandler(property, handler);
                    }
                    var drawers = handler.EnsureInitialized().PropertyDrawers;
                    drawer ??= drawers.Find(d => d is BindProxyIMGUIDrawer) as BindProxyIMGUIDrawer;
                    drawers.Remove(GetDrawer());
                    drawers.Insert(0, GetDrawer());
                }

                return GetDrawer();
            }

            public void AddView(SerializedProperty property, VisualElement root, BindProxyView view)
            {
                SanitizeProperties();
                var key = (property.serializedObject.targetObject, property.propertyPath);
                if (_propertiesToDraw.TryGetValue(key, out var existing) && view != existing.view)
                {
                    existing.view?.Dispose();
                }
                _propertiesToDraw[key] = new (property, root, view);
            }
            
            public void RemoveView(Object source, string path)
            {
                if (!_propertiesToDraw.TryGetValue((source, path), out var existing)) return;
                
                existing.view?.Dispose();
                _propertiesToDraw.Remove((source, path));
            }
            
            public void RemoveView(BindProxyView view, bool disposeView = false)
            {
                var key = _propertiesToDraw.FirstOrDefault(kvp => kvp.Value.view == view).Key;
                if (key == default) return;

                if (disposeView)
                {
                    view.Dispose();
                }

                _propertiesToDraw.Remove(key);

                if (_propertiesToDraw.Count == 0)
                {
                    if (!_idProperty.IsAlive() && _targetObject)
                    {
                        using var serObj = new SerializedObject(_targetObject);
                        _idProperty = serObj.FindProperty(_path);
                        RemoveFromHandlers(_idProperty);
                    }
                    else
                    {
                        RemoveFromHandlers(_idProperty);
                    }
                }
            }

            private void RemoveFromHandlers(SerializedProperty property)
            {
                if (!property.IsAlive())
                {
                    return;
                }
                
                var editors = ProxyBindingSystem.GetEditorsOf(property.serializedObject.targetObject);
                foreach (var editor in editors)
                {
                    DrawerSystem.EditorProxy editorProxy = editor;
                    var cache = editorProxy.propertyHandlerCache;
                    if (cache == null)
                    {
                        continue;
                    }

                    var handler = cache.GetHandler(property);
                    if (handler == null)
                    {
                        continue;
                    }
                    
                    var drawers = handler.PropertyDrawers;
                    drawers?.RemoveAll(d => d == this);
                }
            }

            private void SanitizeProperties()
            {
                List<(Object source, string path)> keysToRemove = null;
                foreach (var (key, value) in _propertiesToDraw)
                {
                    if (!key.source)
                    {
                        if (keysToRemove == null)
                        {
                            keysToRemove = new List<(Object source, string path)>();
                        }
                        keysToRemove.Add(key);
                    }
                }
                
                if (keysToRemove != null)
                {
                    foreach (var key in keysToRemove)
                    {
                        _propertiesToDraw.Remove(key);
                    }
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
#if BS_DEBUG
                EditorGUI.DrawRect(position, Color.green.WithAlpha(0.2f));
#endif

                if (property.serializedObject.isEditingMultipleObjects)
                {
                    EditorGUI.PropertyField(position, property, label, property.isExpanded);
                    return;
                }

                if (!_propertiesToDraw.TryGetValue((property.serializedObject.targetObject, property.propertyPath),
                        out var value))
                {
                    EditorGUI.PropertyField(position, property, label, property.isExpanded);
                    return;
                }
                
                value.view.imguiData.lastFrame = Time.frameCount;

                var shiftX = value.initialX + (EditorGUI.indentLevel - 1) * 15;
                var labelToUse = label;
                if (value.requiresLabelShift)
                {
                    value.label.text = "     " + label.text;
                    shiftX += 15;
                    labelToUse = value.label;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    if (value.isEnabledProperty)
                    {
                        value.view.AdaptToIMGUIRect(position, shiftX, absolute: true);
                    }
                    else
                    {
                        value.view.AdaptToIMGUIRect(position, shiftX);
                    }
                }
                
                CheckAndCreatePotentialList(property, value);

                if (!value.view.IsBound)
                {
                    EditorGUI.PropertyField(position, property, labelToUse, property.isExpanded);
                    return;
                }
                
                if (string.IsNullOrEmpty(label.text))
                {
                    value.label.text = property.displayName;
                    value.view.TrySetIMGUILabel(value.label);
                }
                else
                {
                    value.view.TrySetIMGUILabel(label);
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.isEditingMultipleObjects)
                {
                    return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
                }
                
                if (!_propertiesToDraw.TryGetValue((property.serializedObject.targetObject, property.propertyPath), out var value))
                {
                    return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
                }

                CheckAndCreatePotentialList(property, value);

                if (!value.view.IsBound || value.view.layout.height <= 1)
                {
                    return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
                }
                
                return value.view.layout.height;
            }

            private static void CheckAndCreatePotentialList(SerializedProperty property, DrawData value)
            {
                if (!string.IsNullOrEmpty(value.firstListAncestorPath) && value.list == null)
                {
                    var parentProperty = property.serializedObject.FindProperty(value.firstListAncestorPath);
                    if (DrawerSystem.PropertyHandler.TryGetReorderableList(parentProperty, out value.list))
                    {
                        value.view.RegisterToAllReorderableLists(value.root, value.list);
                    }
                }
            }

            public void UpdatePropertyField(BindProxyView view, Object target, string propertyPath)
            {
                var key = _propertiesToDraw.FirstOrDefault(kvp => kvp.Value.view == view).Key;
                if (key == default) return;

                var data = _propertiesToDraw[key];
                _propertiesToDraw.Remove(key);
                var newKey = (target, propertyPath);
                _propertiesToDraw[newKey] = data;
                
                SanitizeProperties();
            }
        }

        
        // BIND PROXY VIEW -----------------------------------------------------------
        
        public class BindProxyView : VisualElement, IDisposable
        {
            public delegate bool TryAttachToExtensionDelegate(BindProxyView view, VisualElement root,
                string propertyPath);
            
            internal class IMGUIData
            {
                public int lastFrame;
                public int lastRepaintFrame;
                public float lastUpdate;
                public Rect lastRect;
            }

            private class ReorderableListData
            {
                public readonly string listPath;
                public List<object> previousValues;
                public int frame;

                public ReorderableListData(string listPath)
                {
                    this.listPath = listPath;
                }

                
                public bool UpdateValues(ReorderableList list)
                {
                    var listProp = list.serializedProperty;
                    var prevList = previousValues;
                    var changed = false;
                    previousValues = new List<object>();
                    
                    for (var i = 0; i < listProp.arraySize; i++)
                    {
                        var value = listProp.GetArrayElementAtIndex(i).GetValue();
                        changed |= prevList == null || i >= prevList.Count || !Equals(prevList[i], value);
                        previousValues.Add(value);
                    }
                    
                    return changed;
                }

                public bool UpdateValues(IList itemsSource)
                {
                    var prevList = previousValues;
                    var changed = false;
                    previousValues = new List<object>();
                    if (itemsSource.Count > 0 && itemsSource[0] is SerializedProperty initialProp)
                    {
                        initialProp.serializedObject.Update();
                    }
                    
                    for (var i = 0; i < itemsSource.Count; i++)
                    {
                        var value = itemsSource[i] is SerializedProperty prop ? prop.GetValue() : itemsSource[i];
                        changed |= prevList == null || i >= prevList.Count || !Equals(prevList[i], value);
                        previousValues.Add(value);
                    }
                    
                    return changed;
                }

                public int GetIndex(string fullPath)
                {
                    var path = fullPath;
                    var openBracketIndex = path.IndexOf('[', this.listPath.Length);
                    if (openBracketIndex < 0)
                    {
                        // Something went wrong
                        return -1;
                    }
                    var closeBracketIndex = path.IndexOf(']', openBracketIndex);
                    if (closeBracketIndex < 0)
                    {
                        // Something went wrong
                        return -1;
                    }
                
                    var index = int.Parse(path[(openBracketIndex + 1)..closeBracketIndex]);
                    return index;
                }

                public string GetPath(string fullPath)
                {
                    var closeBracketIndex = fullPath.IndexOf(']', this.listPath.Length);
                    if (closeBracketIndex < 0)
                    {
                        // Something went wrong
                        return fullPath;
                    }
                
                    var listPath = fullPath[..(closeBracketIndex + 1)];
                    return listPath;
                }
            }
            
            private static readonly List<TryAttachToExtensionDelegate> _extensionAttachments = new();
            
            public static void RegisterExtension(TryAttachToExtensionDelegate extension)
            {
                if(_extensionAttachments.Contains(extension))
                {
                    return;
                }
                _extensionAttachments.Add(extension);
            }
            
            public static void UnregisterExtension(TryAttachToExtensionDelegate extension)
            {
                _extensionAttachments.Remove(extension);
            }
            
            private Properties _properties;
            private VisualElement _root;
            private VisualElement _background;

            public Toggle isBoundView;
            public Button specialBindButton;
            public VisualElement bindDataView;
            public VisualElement valueView;
            public Label valueLabel;
            
            public (VisualElement parent, int index) isBindedSlot;
            public (VisualElement parent, int index) isNotBindedSlot;
            
            internal IMGUIData imguiData;
            
            private bool? _wasBound;
            private bool _disposed;
            private Action _onCleanup;

            private bool _isMaterialProperty;
            private bool _isSpecialBind;
            private bool _isPartOfReorderableList;
            private ReorderableList _imguiReorderableList;
            private Label _imguiLabel;
            private BindProxyIMGUIDrawer _imguiDrawer;
            private EditorGUIUtility.PropertyCallbackScope _propertyStackScope;
            private IMGUIContainer _imguiVisibilityTester;
            
            private SerializedObject _targetSerializedObject;

            private Object[] _sources;
            
            private SerializedProperty IsBoundProperty => _properties.isBound;

            private EditorGUIUtility.PropertyCallbackScope PropertyStackScope
            {
                get => _propertyStackScope;
                set
                {
                    if(value == _propertyStackScope)
                    {
                        return;
                    }
                    
                    if (_propertyStackScope != null)
                    {
                        _propertyStackScope.Dispose();
                        EditorGUIHelper.TryCleanup();
                    }
                    _propertyStackScope = value;
                    if (_propertyStackScope != null)
                    {
                        EditorGUIHelper.EnsureHooked();
                    }
                }
            }
            public event Action<BindProxyView> OnPathChanged;
            
            public event Action OnTargetRemoved;
            public event Action<BindProxyView> OnDisposed;

            public string PropertyPath { get; private set; }
            
            public string TargetPath => _properties.path.stringValue;
            
            public bool IsBound => isBoundView?.value == true;

            public bool IsSpecialBind
            {
                get => _isSpecialBind;
                internal set
                {
                    EnableInClassList("bs-bind-proxy--special", _isSpecialBind = value);
                    if (!value) return;
                    
                    if (isBoundView != null)
                    {
                        isBoundView.value = true;
                    }

                    _properties.isBound.boolValue = true;
                }
            }

            public BindProxyView(VisualElement root, SerializedProperty property, GUIContent label, bool isMaterialProperty)
            {
                _root = root;
                _isMaterialProperty = isMaterialProperty;
                _properties = new Properties(property, label ?? new GUIContent(property.displayName));
                
                _sources = property.serializedObject.targetObjects;
                
                PropertyPath = property.propertyPath;

                viewDataKey = _properties.path.stringValue;

                this.AddBSStyle().WithClass("bs-bind", "bs-bind-proxy");
                
                BuildUIViews();
            }

            public void ChangePath(string newTargetPath, bool withUndo = true)
            {
                if (withUndo)
                {
                    _properties.path.stringValue = newTargetPath;
                    _properties.path.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    _properties.path.stringValue = newTargetPath;
                    _properties.path.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                OnPathChanged?.Invoke(this);
            }

            public void RemoveTarget()
            {
                OnTargetRemoved?.Invoke();
            }
            
            private bool TryGetPathProperty(out SerializedProperty property)
            {
                if (!_properties.source.IsAlive())
                {
                    property = null;
                    return false;
                }
                if(!_properties.source.objectReferenceValue)
                {
                    property = null;
                    return false;
                }
                if (_targetSerializedObject == null || !_targetSerializedObject.IsAlive())
                {
                    _targetSerializedObject?.Dispose();
                    _targetSerializedObject = new SerializedObject(_properties.source.objectReferenceValue);
                }
                _targetSerializedObject.Update();
                property = _targetSerializedObject.FindProperty(_properties.path.stringValue);
                return property != null;
            }
            
            private bool EnsureValidProperties()
            {
                if (_properties.property.IsAlive())
                {
                    return true;
                }

                foreach (var source in _sources)
                {
                    if(!source)
                    {
                        return false;
                    }
                }
                
                var newSerializedObject = new SerializedObject(_sources);
                var property = newSerializedObject.FindProperty(PropertyPath);
                if (property == null)
                {
                    return false;
                }
                
                _properties = new Properties(property, new GUIContent(_imguiLabel?.text ?? property.displayName));
                return true;
            }

            public void UpdatePropertyField(SerializedProperty property)
            {
                if (PropertyPath == property.propertyPath)
                {
                    return;
                }
                
                _properties = new Properties(property, new GUIContent(_imguiLabel?.text ?? property.displayName));
                isBoundView.BindProperty(IsBoundProperty);
                PropertyPath = property.propertyPath;
                if(bindDataView is PropertyField field)
                {
                    var bindDataUI = field.Q<BindDataDrawer.BindDataUI>();
                    bindDataUI?.Reset();
                    field.BindProperty(_properties.bindData);
                    field.schedule.Execute(() =>
                    {
                        field.OnBind(p =>
                        {
                            var labelText = valueLabel?.text ?? _properties.label?.text;
                            TrySetLabel(labelText);
                            if(!_properties.property.IsAlive())
                            {
                                return;
                            }
                            p.tooltip = _properties.property.tooltip;
                        });
                    }).ExecuteLater(1);
                }

                // Reset the label, so it can be updated
                _imguiLabel = null;
            }
            
            private void BuildUIViews()
            {
                isBindedSlot = (this, 0);
                isNotBindedSlot = (this, 0);

                if (IsBoundProperty != null)
                {
                    isBoundView = new Toggle()
                        {
                            tooltip = "Bind this field",
                            usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor
                        }.AddBSStyle()
                        .WithClass("bs-bind-toggle", "bs-bind-proxy__toggle", "transparent");

                    isBoundView.UnregisterValueChangedCallback(ApplyBoundValueChanged);
                    isBoundView.RegisterValueChangedCallback(ApplyBoundValueChanged);
                    isBoundView.BindProperty(IsBoundProperty);
                }
                else
                {
                    isBoundView = new Toggle()
                        {
                            tooltip = "Bind this field",
                            value = true,
                            pickingMode = PickingMode.Ignore,
                            usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor
                        }.AddBSStyle()
                        .WithClass("bs-bind-toggle", "bs-bind-proxy__toggle", "transparent");
                }

                BuildSpecialButton();

                Add(specialBindButton);

                _properties.isBoundValue?.Update();

                ApplyIsBoundValue(IsBoundProperty == null || IsBoundProperty.boolValue);
                
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

                OnInitializeUIViews?.Invoke(null /*_owner*/, this);
                
                _background = new VisualElement().WithClass("bs-bind-proxy__background");
                
                Insert(0, _background);
            }

            private void BuildSpecialButton()
            {
                specialBindButton = new Button
                    {
                        focusable = false,
                    }.WithClass("bs-bind-proxy__special-button", "transparent")
                    .WithChildren(new Image().WithClass("bs-bind-proxy__special-icon"));
                
                specialBindButton.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Disable Binding", _ =>
                    {
                        RemoveTarget();
                    });
                }));
                
                specialBindButton.clickable = new MultiClickable(
                    new Clickable(ShowBindPanel),
                    CreateCtrlClickable(RemoveTarget, "bs-bind-proxy__special-button--remove"));
                
                specialBindButton.RegisterCallback<TooltipEvent>(evt =>
                {
                    evt.tooltip = ClassListContains("path-not-set") ?
                        "Bind enabled, " + "but not set up".RT().Bold().Color(BindColors.Debug) + ", click to show bind panel":
                        ClassListContains("runtime-fail") ? 
                            "Bind enabled, " + "but runtime failed".RT().Bold().Color(BindColors.Error) + ", click to show bind panel":
                        "Bind enabled, click to show bind panel";
                    evt.rect = specialBindButton.worldBound;
                    evt.StopPropagation();
                });

                Clickable CreateCtrlClickable(Action action, string previewClass)
                {
                    var clickable = new PreviewClickable(action, previewClass);
                    clickable.activators.Clear();
                    clickable.activators.Add(new ManipulatorActivationFilter()
                    {
                        modifiers = Application.platform == RuntimePlatform.OSXEditor
                            ? EventModifiers.Command
                            : EventModifiers.Control
                    });
                    return clickable;
                }
            }

            private void ShowBindPanel()
            {
                var view = BindDataDrawer.ShowBindView(GUIUtility.GUIToScreenRect(worldBound), _properties.property.GetValue() as BindProxy);
                if (view != null)
                {
                    view.BindProxyView = this;
                }
            }

            private void ApplyBoundValueChanged(ChangeEvent<bool> evt)
            {
                ApplyIsBoundValue(evt.newValue, updateImmediately: true);
            }

            private void OnDetachedFromPanel(DetachFromPanelEvent evt)
            {
                valueView?.RemoveBSStyle();
            }

            private void OnAttachToPanel(AttachToPanelEvent evt)
            {
                var root = _root ?? evt.destinationPanel.visualTree;
                root[0].schedule.Execute(() => AttachToRoot(evt.destinationPanel.visualTree)).ExecuteLater(0);
            }
            
            public void AttachToRoot(VisualElement root, bool delayed = false)
            {
                UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                
                if (delayed)
                {
                    root.schedule.Execute(() => TryAttach()).ExecuteLater(200);
                    return;
                }
                
                TryAttach();

                bool TryAttach()
                {
                    bool success;
                    if (_extensionAttachments?.Count > 0)
                    {
                        EnsureValidProperties();
                        foreach (var tryAttach in _extensionAttachments)
                        {
                            if(tryAttach(this, root, _properties.path.stringValue))
                            {
                                MarkPropertyWithHeader(root);
                                return true;
                            }
                        }
                    }
                    
                    if (_isMaterialProperty)
                    {
                        success = TryAttachToMaterialProperty(root);
                    }
                    else if (root is PropertyField)
                    {
                        success = TryAttachToPropertyField(root);
                    }
                    else if (root is IBindable bindable && bindable.bindingPath == _properties.path.stringValue)
                    {
                        success = TryAttachToBindableField(bindable);
                    }
                    else
                    {
                        success = TryAttachToIMGUIRoot(root);
                    }

                    if (!BindingSettings.Current.ShowTargetGroupReplacement)
                    {
                        return success;
                    }

                    MarkPropertyWithHeader(root);
                    return success;
                }
            }
            
            public void AdaptToIMGUIRect(Rect position, float shiftX, bool absolute = false)
            {
                if (PropertyStackScope != null)
                {
                    PropertyStackScope = null;
                    AddToClassList("precise-drawer");
                }

                AdaptToIMGUIRectInternal(position, shiftX, absolute: absolute);
            }

            public void AdaptToMaterialPropertyRect(Rect position, float shiftX)
            {
                imguiData.lastFrame = Time.frameCount;
                AdaptToIMGUIRectInternal(position, shiftX);
            }

            private void AdaptToIMGUIRectInternal(Rect position, float shiftX, float addWidth = 0, bool absolute = false)
            {
                var isHidden = position.height <= 1 || position.width <= 1 || (position.x == 0 && position.y == 0);

                if (Event.current?.type == EventType.Repaint)
                {
                    EnableInClassList("hidden", isHidden);
                    isBoundView?.EnableInClassList("hidden", isHidden);
                }

                if(isHidden)
                {
                    return;
                }

                style.width = position.width - shiftX + addWidth;
                if (absolute)
                {
                    style.top = position.y;
                }
                else
                {
                    style.top = position.y - Mathf.Max(18 - position.height, 0) * 0.5f;
                }

                style.left = position.x + shiftX;
            }

            public void TrySetIMGUILabel(GUIContent label)
            {
                TrySetIMGUILabel(label.text);
            }

            public void TrySetIMGUILabel(string label)
            {
                if(label == null)
                {
                    return;
                }
                
                if (_imguiLabel == null || !_imguiLabel.IsDisplayed())
                {
                    _imguiLabel = TrySetLabel(label);
                    return;
                }
                
                _imguiLabel.text = label;
            }

            public Label TrySetLabel(string value)
            {
                if (isBoundView == null)
                {
                    return null;
                }

                if (!isBoundView.value) return null;
                
                var label = bindDataView?
                            .Query<Label>(null, "bind-field__label")
                            .Where(l => l.IsDisplayed())
                            .First();
                if (label == null) return null;
                
                label.text = value;
                return label;
            }
            
            private bool TryAttachToMaterialProperty(VisualElement root)
            {
                if (!EnsureValidProperties())
                {
                    return false;
                }
                
                var fullPath = _properties.path.stringValue;
                var splitPath = fullPath.Split('-', StringSplitOptions.RemoveEmptyEntries);
                var (propertyPath, propertyType) = (splitPath[0], splitPath[1]);
                var propertyName = propertyPath[(propertyPath.LastIndexOf('.') + 1)..];
                var material = _properties.source.objectReferenceValue as Material;
                if (!material && _properties.source.objectReferenceValue is Renderer renderer)
                {
                    // Extract the index of the material
                    var openBracketIndex = propertyPath.LastIndexOf('[');
                    var closeBracketIndex = propertyPath.LastIndexOf(']');
                    if (openBracketIndex < 0 || closeBracketIndex < 0)
                    {
                        return false;
                    }
                    var materialIndex = int.Parse(propertyPath[(openBracketIndex + 1)..(closeBracketIndex)]);
                    var materials = renderer.sharedMaterials;
                    if (materialIndex < 0 || materialIndex >= materials.Length)
                    {
                        return false;
                    }
                    material = materials[materialIndex];
                }
                
                if (!material)
                {
                    return false;
                }
                
                root.Add(this);
                
                AddToClassList("bs-bind-proxy--imgui");
                
                MaterialProxyDrawer.RegisterView(material, propertyName, propertyType, this);

                AddToClassList("hidden");
                
                imguiData = new IMGUIData();
                
                this.Q("bs-bind-proxy__visibility-tester")?.RemoveFromHierarchy();
                var visibilityTester = new VisualElement();
                visibilityTester.AddToClassList("bs-bind-proxy__visibility-tester");
                Add(visibilityTester);
                visibilityTester.schedule.Execute(() =>
                {
                    if (visibilityTester.panel == null)
                    {
                        return;
                    }

                    if (imguiData.lastFrame >= Time.frameCount - 1)
                    {
                        return;
                    }

                    if (imguiData.lastFrame >= Time.frameCount - 2 || imguiData.lastUpdate >= Time.realtimeSinceStartup - 0.5f)
                    {
                        MarkDirtyRepaint();
                        return;
                    }
                    
                    HideInIMGUI();
                    imguiData.lastFrame = int.MaxValue;
                    imguiData.lastUpdate = float.MaxValue;
                }).Every(100).StartingIn(50);

                isBoundView.schedule.Execute(() => isBoundView.RemoveFromClassList("transparent"));
                
                return true;
            }

            private bool TryAttachToIMGUIRoot(VisualElement root)
            {
                PropertyStackScope = null;
                _imguiLabel = null;

                if (!EnsureValidProperties())
                {
                    return false;
                }
                
                // Create the property it points to
                var propertyPath = _properties.path.stringValue;
                if (string.IsNullOrEmpty(propertyPath))
                {
                    return false;
                }

                if (!TryGetPathProperty(out var property))
                {
                    return false;
                }
                
                var source = _properties.source.objectReferenceValue;
                
                root.Add(this);
                
                _imguiDrawer = BindProxyIMGUIDrawer.GetIMGUIDrawer(property);
                _imguiDrawer.AddView(property, root, this);
                
                var initialX = root is PropertyField field && field.name?.EndsWith(property.propertyPath) == true 
                    ? 15 
                    : property.propertyPath.EndsWith(']') 
                    ? 3
                    : 0;
                
                var tempLabel = new GUIContent();
                var textPrefix = "";
                var usePrefixRect = false;
                var coverWithBackground = true;
                var keepFieldHeight = false;
                var addWidth = 0f;

                imguiData = new IMGUIData()
                {
                    lastRect = new Rect()
                };
                
                AddToClassList("bs-bind-proxy--imgui");
                
                if(ProxyBindingsDrawOverrides.TryGetOverrideFor(property, out var overrides))
                {
                    initialX += overrides.shiftX ?? 0;
                    textPrefix = overrides.textPrefix;
                    usePrefixRect |= overrides.usePrefixRect == true;
                    coverWithBackground &= overrides.coverWithBackground != false;
                    keepFieldHeight = overrides.keepFieldHeight == true;

                    if (overrides.panelShiftX.HasValue)
                    {
                        var imguiEditor =
                            root.Q<IMGUIContainer>(null, "unity-inspector-element__custom-inspector-container");
                        if (imguiEditor != null)
                        {
                            imguiEditor.style.marginLeft = overrides.panelShiftX.Value;
                            addWidth = overrides.panelShiftX.Value;
                        }
                    }
                }
                
                // Set by default invisible
                HideInIMGUI();
                
                _background.EnableInClassList("hidden", !coverWithBackground);

                _imguiVisibilityTester?.RemoveFromHierarchy();
                _imguiVisibilityTester = new IMGUIContainer(() =>
                {
                    if(_imguiVisibilityTester.panel != null && imguiData.lastFrame < Time.frameCount && Event.current.type != EventType.Layout)
                    {
                        HideInIMGUI();
                        imguiData.lastFrame = int.MaxValue;
                    }
                }).WithClass("bßs-bind-proxy__visibility-tester");
                Add(_imguiVisibilityTester);

                PropertyStackScope = new EditorGUIUtility.PropertyCallbackScope((r, p) =>
                {
                    if (p.serializedObject.targetObject != source || p.propertyPath != propertyPath) return;

                    if (panel == null)
                    {
                        // It has detached, dispose this method
                        PropertyStackScope = null;
                        return;
                    }
                    
                    imguiData.lastFrame = Time.frameCount;

                    if (Event.current.type == EventType.Repaint && imguiData.lastRepaintFrame < Time.frameCount)
                    {
                        imguiData.lastRect = r;
                        imguiData.lastRepaintFrame = Time.frameCount;
                    }
                    
                    if (imguiData.lastRect.Overlaps(r))
                    {
                        r = imguiData.lastRect;
                    }
                    
#if BS_DEBUG
                    EditorGUI.DrawRect(r, Color.red.WithAlpha(0.25f));
#endif

                    var exactPrefixSet = false;
                    var shiftX = initialX + (EditorGUI.indentLevel - 1) * 15;

                    if (DrawerSystem.EditorGUI.HasPrefixLabel && DrawerSystem.EditorGUI.PrefixTotalRect == r)
                    {
                        if (usePrefixRect)
                        {
                            r = DrawerSystem.EditorGUI.PrefixTotalRect;
                        }

                        tempLabel.text = DrawerSystem.EditorGUI.PrefixLabel.text;
                        tempLabel.tooltip = DrawerSystem.EditorGUI.PrefixLabel.tooltip;
                        DrawerSystem.EditorGUI.PrefixLabel.text = textPrefix + DrawerSystem.EditorGUI.PrefixLabel.text;
                    }
                    else if (EditorGUIHelper.TryGetCapturedPrefixLabel(r, out var prefixLabel))
                    {
                        tempLabel.text = prefixLabel.label.text;
                        tempLabel.tooltip = prefixLabel.label.tooltip;
                        exactPrefixSet = true;
                    }
                    
                    AdaptToIMGUIRectInternal(r, shiftX, addWidth);
                    
                    if(!IsBound || imguiData.lastRect.height == 0)
                    {
                        return;
                    }
                    
                    if (imguiData.lastRect.Contains(Event.current.mousePosition))
                    {
                        Event.current.mousePosition = new Vector2(-1000, -1000);
                        // return;
                    }
                    
                    if(Event.current.type == EventType.Repaint || Event.current.type == EventType.Used)
                    {
                        return;
                    }

                    var layoutEntry = DrawerSystem.GUILayoutUtility.GetLastLayoutEntry();
                    if (layoutEntry == null)
                    {
                        return;
                    }


                    var priorityLabel = DrawerSystem.EditorGUI.PropertyFieldTempContent;
                    if (!exactPrefixSet && !string.IsNullOrEmpty(priorityLabel?.text))
                    {
                        tempLabel.text = priorityLabel.text;
                        tempLabel.tooltip = priorityLabel.tooltip;
                    }
                    else if (string.IsNullOrEmpty(tempLabel.text))
                    {
                        tempLabel.text = p.displayName;
                    }

                    if (coverWithBackground && keepFieldHeight)
                    {
                        _background.style.height = r.height;
                    }
                    else if(!coverWithBackground)
                    {
                        DrawerSystem.EditorGUI.PrefixLabel.text = "";
                        var propertyFieldTempContent = DrawerSystem.EditorGUI.PropertyFieldTempContent;
                        if(propertyFieldTempContent != null)
                        {
                            propertyFieldTempContent.text = "";
                        }
                    }

                    TrySetIMGUILabel(tempLabel);
                    
                    if (_isPartOfReorderableList)
                    {
                        var height = EditorGUI.GetPropertyHeight(p);
                        layoutEntry.minHeight += layout.height - height;
                        layoutEntry.maxHeight += layout.height - height;
                    }
                    else if(!keepFieldHeight)
                    {
                        layoutEntry.minHeight = layout.height;
                        layoutEntry.maxHeight = layout.height;
                    }
                });

                isBoundView.schedule.Execute(() => isBoundView.RemoveFromClassList("transparent"));
                specialBindButton?.schedule.Execute(() => specialBindButton.RemoveFromClassList("transparent"));

                return true;
            }

            public void RegisterToAllReorderableLists(VisualElement root, ReorderableList list)
            {
                var listPropertyPath = list.serializedProperty.propertyPath;
                if (!_properties.path.stringValue.StartsWith(listPropertyPath))
                {
                    return;
                }

                var data = new ReorderableListData(listPropertyPath);
                data.UpdateValues(list);
                
                void HookEvents()
                {
                    list.onChangedCallback += ValidateSizeChanged;
                    list.onReorderCallbackWithDetails += OnItemIndexChanged;
                    Undo.undoRedoPerformed += UndoRedoPerformed;
                }
                
                void UnhookEvents()
                {
                    list.onChangedCallback -= ValidateSizeChanged;
                    list.onReorderCallbackWithDetails -= OnItemIndexChanged;
                    Undo.undoRedoPerformed -= UndoRedoPerformed;
                }

                UnhookEvents();
                HookEvents();
                
                _onCleanup += UnhookEvents;

                var parentProperty = list.serializedProperty.GetParent();
                while (parentProperty != null)
                {
                    if(DrawerSystem.PropertyHandler.TryGetReorderableList(parentProperty, out var listParent))
                    {
                        RegisterToAllReorderableLists(root, listParent);
                        return;
                    }
                    parentProperty = parentProperty.GetParent();
                }
                
                void UndoRedoPerformed()
                {
                    UpdateIMGUIDrawer();
                }
                
                void OnItemsSizeChanged(IEnumerable<int> indices, bool added)
                {
                    var currentIndex = data.GetIndex(_properties.path.stringValue);
                    var step = 0;
                    foreach (var i in indices.OrderBy(i => i))
                    {
                        if (i == currentIndex && !added)
                        {
                            UnhookEvents();
                            OnTargetRemoved?.Invoke();
                            return;
                        }
                        
                        if (i <= currentIndex)
                        {
                            step++;
                            continue;
                        }

                        break;
                    }

                    data.UpdateValues(list);

                    if (step == 0)
                    {
                        return;
                    }

                    if (!added)
                    {
                        step = -step;
                    }
                    var newIndex = currentIndex + step;
                    if (UpdatePath(newIndex, true))
                    {
                        UpdateIMGUIDrawer();
                    }
                    
                    OnPathChanged?.Invoke(this);
                }

                void UpdateIMGUIDrawer()
                {
                    if (_imguiDrawer == null)
                    {
                        _imguiDrawer = BindProxyIMGUIDrawer.GetIMGUIDrawer(_properties.path);
                        _imguiDrawer.AddView(_properties.path, root, this);
                    }
                    else
                    {
                        _imguiDrawer.UpdatePropertyField(this, _properties.source.objectReferenceValue,
                            _properties.path.stringValue);
                    }
                }

                void OnItemIndexChanged(ReorderableList reorderableList, int from, int to)
                {
                    data.UpdateValues(list);
                    
                    if (from == to)
                    {
                        return;
                    }

                    var currentIndex = data.GetIndex(_properties.path.stringValue);
                    
                    var (min, max) = from < to ? (from, to) : (to, from);
                    if(currentIndex < min || currentIndex > max)
                    {
                        return;
                    }

                    if (from == currentIndex)
                    {
                        currentIndex = to;
                    }
                    else if (from > to)
                    {
                        currentIndex++;
                    }
                    else
                    {
                        currentIndex--;
                    }

                    if (UpdatePath(currentIndex, true))
                    {
                        UpdateIMGUIDrawer();
                    }
                    OnPathChanged?.Invoke(this);
                }

                bool UpdatePath(int newIndex, bool applyModifiedProperties)
                {
                    if(data.frame == Time.frameCount)
                    {
                        return false;
                    }
                    
                    data.frame = Time.frameCount;
                    
                    var newPath = listPropertyPath + $".Array.data[{newIndex}]";
                    var oldPath = data.GetPath(_properties.path.stringValue);
                    
                    if (newPath == oldPath)
                    {
                        return false;
                    }
                    
                    // _properties.path.serializedObject.Update();
                    
                    var fullPath = _properties.path.stringValue;
                    var newFullPath = fullPath.Replace(oldPath, newPath);
                    _properties.path.stringValue = newFullPath;
                    
                    _imguiLabel = null;
                    _properties.UpdateMetaValues(applyModifiedProperties, out var changed);

                    
                    if (applyModifiedProperties && !changed)
                    {
                        changed = _properties.path.serializedObject.ApplyModifiedProperties();
                    }
                    return changed;

                }

                void ValidateSizeChanged(ReorderableList reorderableList)
                {
                    var listCount = list.serializedProperty.arraySize;
                    if (data.previousValues.Count == listCount)
                    {
                        return;
                    }

                    var previousList = data.previousValues;
                    var difference = listCount - previousList.Count;
                    var indices = new List<int>();

                    var newCount = 0;
                    for (int i = 0; i < previousList.Count; i++)
                    {
                        var value = previousList[i];
                        if(newCount >= listCount)
                        {
                            for (int j = i; j < previousList.Count; j++)
                            {
                                indices.Add(j);
                            }
                            break;
                        }
                        var listItem = list.serializedProperty.GetArrayElementAtIndex(newCount++);
                        var newValue = listItem.GetValue();
                        
                        if (Equals(value, newValue)) continue;
                        
                        indices.Add(i);
                        newCount = difference > 0 ? newCount + 1 : newCount - 1;
                    }

                    data.UpdateValues(list);

                    if (indices.Count > 0)
                    {
                        OnItemsSizeChanged(indices, difference > 0);
                    }
                }
            }

            internal void HideInIMGUI()
            {
                AdaptToIMGUIRectInternal(new Rect(), 0);
                AddToClassList("hidden");
            }
            
            private void MarkPropertyWithHeader(VisualElement root)
            {
                var propertyFieldWithHeader = root as PropertyField ?? this.QueryParent<PropertyField>();
                if (propertyFieldWithHeader == null)
                {
                    return;
                }
                
                propertyFieldWithHeader.AddToClassList("bind-property");
                
                var headerAttribute = _properties.property.IsAlive() 
                    ? _properties.property.GetFieldInfo()?.GetCustomAttribute<HeaderAttribute>()
                    : null;
                if (headerAttribute == null)
                {
                    var propParent = propertyFieldWithHeader.parent;
                    var index = propParent.IndexOf(propertyFieldWithHeader);
                    VisualElement header = null;
                    while (index > 0)
                    {
                        if (propParent[index--] is not PropertyField sibling)
                        {
                            continue;
                        }
                        header = sibling.Q<Label>(null, "unity-header-drawer__label");
                        
                        if (header == null) continue;
                        
                        propertyFieldWithHeader = sibling;
                        break;
                    }

                    if (header == null)
                    {
                        return;
                    }
                    header.AddBSStyle();
                }

                propertyFieldWithHeader.AddToClassList("property-with-header");
                propertyFieldWithHeader.parent.AddToClassList("properties-with-header");
            }
            
            private bool TryAttachToBindableField(IBindable bindable)
            {
                valueView?.RemoveBSStyle();
                valueLabel?.WithoutClassDelayed("bs-bind-proxy__label");
                
                if (!_properties.property.IsAlive())
                {
                    return false;
                }

                if(bindable is not VisualElement field || bindable.bindingPath != _properties.path.stringValue)
                {
                    return false;
                }

                // Remove all BindProxyView instances from the field
                field.Query<BindProxyView>().ForEach(v =>
                {
                    if (v != this && v.parent == field)
                    {
                        v.RemoveFromHierarchy();
                    }
                });

                valueView = field is PropertyField 
                            ? field.Q(null, BaseField<int>.ussClassName)
                            : field.Q(null, BaseField<int>.inputUssClassName);
                valueLabel = field.Q<Label>(null, BaseField<int>.labelUssClassName);

                if (valueView == null)
                {
                    return false;
                }
                
                if (valueLabel != null)
                {
                    valueLabel.WithClassDelayed("bs-bind-proxy__label", "bs-bind-proxy__label--animated");
                    isNotBindedSlot = (valueLabel.parent, valueLabel.parent.IndexOf(valueLabel) + 1);
                }

                field.Add(this);

                ApplyIsBoundValue(isBoundView.value, forced: true);
                
                field.AddBSStyle();
                
                RegisterToAllReorderableLists(field);
                
                AddToClassList("bs-bind-proxy--bindable");
                
                return true;
            }

            private bool TryAttachToPropertyField(VisualElement root = null)
            {
                valueView?.RemoveBSStyle();
                valueLabel?.WithoutClassDelayed("bs-bind-proxy__label");
                
                if (!_properties.property.IsAlive())
                {
                    return false;
                }

                var currentRoot = root ?? panel?.visualTree;
                var propertyPath = _properties.path.stringValue;
                
                if (!TryRetrievePropertyField(currentRoot, propertyPath, out var field)) return false;

                if (field is IMGUIContainer)
                {
                    // Could be that the field is under IMGUI
                    return true;
                }

                // Remove all BindProxyView instances from the field
                field.Query<BindProxyView>().ForEach(v =>
                {
                    if (v != this && v.parent == field)
                    {
                        v.RemoveFromHierarchy();
                    }
                });

                TryRetrieveValueField(field, propertyPath);

                if (valueView == null)
                {
                    return false;
                }

                
                if (valueLabel != null)
                {
                    valueLabel.WithClassDelayed("bs-bind-proxy__label", "bs-bind-proxy__label--animated");
                    isNotBindedSlot = (valueLabel.parent, valueLabel.parent.IndexOf(valueLabel) + 1);
                }

                field.Add(this);

                ApplyIsBoundValue(isBoundView.value, forced: true);
                
                field.AddBSStyle();
                
                RegisterToAllReorderableLists(field);
                
                return true;
            }

            private void RegisterToAllReorderableLists(VisualElement result)
            {
                var listView = result?.parent?.GetFirstAncestorOfType<ListView>();
                if (listView == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(listView.bindingPath))
                {
                    return;
                }

                if (!_properties.path.stringValue.StartsWith(listView.bindingPath))
                {
                    return;
                }
                
                var listPropertyPath = listView.bindingPath;

                var data = new ReorderableListData(listPropertyPath);
                data.UpdateValues(listView.itemsSource);
                
                var scrollView = listView.Q<ScrollView>();

                void HookEvents()
                {
                    listView.itemIndexChanged += OnItemIndexChanged;
                    scrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    Undo.undoRedoPerformed += UndoRedoPerformed;
                }
                
                void UnhookEvents()
                {
                    listView.itemIndexChanged -= OnItemIndexChanged;
                    scrollView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    Undo.undoRedoPerformed -= UndoRedoPerformed;
                }

                UnhookEvents();
                HookEvents();
                
                _onCleanup += UnhookEvents;
                
                RegisterToAllReorderableLists(listView);
                
                void UndoRedoPerformed()
                {
                    UnhookEvents();
                    
                    AttachToRoot(listView.GetFirstAncestorOfType<PropertyField>(), true);
                }
                
                void OnItemsSizeChanged(IEnumerable<int> indices, bool added)
                {
                    var currentIndex = data.GetIndex(_properties.path.stringValue);
                    var step = 0;
                    foreach (var i in indices.OrderBy(i => i))
                    {
                        if (i == currentIndex && !added)
                        {
                            OnTargetRemoved?.Invoke();
                            return;
                        }
                        
                        if (i <= currentIndex)
                        {
                            step++;
                            continue;
                        }

                        break;
                    }

                    data.UpdateValues(listView.itemsSource);

                    if (step == 0)
                    {
                        return;
                    }

                    if (!added)
                    {
                        step = -step;
                    }
                    var newIndex = currentIndex + step;
                    UpdatePath(newIndex, true);
                    UnhookEvents();
                    TryAttachToPropertyField(listView);
                    OnPathChanged?.Invoke(this);
                }
                
                void OnItemIndexChanged(int from, int to)
                {
                    data.UpdateValues(listView.itemsSource);
                    
                    if (from == to)
                    {
                        return;
                    }

                    var currentIndex = data.GetIndex(_properties.path.stringValue);
                    
                    var (min, max) = from < to ? (from, to) : (to, from);
                    if(currentIndex < min || currentIndex > max)
                    {
                        return;
                    }

                    if (from == currentIndex)
                    {
                        currentIndex = to;
                    }
                    else if (from > to)
                    {
                        currentIndex++;
                    }
                    else
                    {
                        currentIndex--;
                    }
                    
                    UpdatePath(currentIndex, true);
                    schedule.Execute(() =>
                    {
                        valueLabel = valueView?.Q<Label>(null, BaseField<int>.labelUssClassName);
                        if (valueLabel != null)
                        {
                            TrySetLabel(valueLabel.text);
                        }
                    }).ExecuteLater(0);
                    OnPathChanged?.Invoke(this);
                }

                bool UpdatePath(int newIndex, bool applyModifiedProperties)
                {
                    if(data.frame == Time.frameCount)
                    {
                        return false;
                    }
                    
                    data.frame = Time.frameCount;
                    
                    var newPath = listView.bindingPath + $".Array.data[{newIndex}]";
                    var oldPath = data.GetPath(_properties.path.stringValue);
                    
                    if (newPath == oldPath)
                    {
                        return false;
                    }
                    
                    // _properties.path.serializedObject.Update();
                    
                    var fullPath = _properties.path.stringValue;
                    var newFullPath = fullPath.Replace(oldPath, newPath);
                    _properties.path.stringValue = newFullPath;
                    
                    _properties.UpdateMetaValues(applyModifiedProperties, out var changed);

                    TryGetPathProperty(out var pathProperty);
                    
                    if (!applyModifiedProperties || changed)
                    {
                        return true;
                    }
                    
                    var changesApplied = _properties.path.serializedObject.ApplyModifiedProperties();

                    return changesApplied;

                }
                
                void OnGeometryChanged(GeometryChangedEvent evt)
                {
                    ValidateSizeChanged();
                }

                void ValidateSizeChanged()
                {   
                    if (data.previousValues.Count == listView.itemsSource?.Count)
                    {
                        return;
                    }

                    var previousList = data.previousValues;
                    var difference = listView.itemsSource.Count - previousList.Count;
                    var indices = new List<int>();

                    var newCount = 0;
                    for (int i = 0; i < previousList.Count; i++)
                    {
                        var value = previousList[i];
                        if(newCount >= listView.itemsSource.Count)
                        {
                            for (int j = i; j < previousList.Count; j++)
                            {
                                indices.Add(j);
                            }
                            break;
                        }
                        var listItem = listView.itemsSource[newCount++];
                        var newValue = listItem is SerializedProperty propItem ? propItem.GetValue() : listItem;
                        
                        if (Equals(value, newValue)) continue;
                        
                        indices.Add(i);
                        newCount = difference > 0 ? newCount + 1 : newCount - 1;
                    }

                    data.UpdateValues(listView.itemsSource);

                    if (indices.Count > 0)
                    {
                        OnItemsSizeChanged(indices, difference > 0);
                    }
                }
            }

            private bool TryRetrievePropertyField(VisualElement currentRoot, string propertyPath, out VisualElement result)
            {
                var (parentPath, name) = SplitPath(propertyPath);

                if (currentRoot is not PropertyField field)
                {
                    var inspectorView = currentRoot.Q(null, "unity-inspector-editors-list");
                    currentRoot = inspectorView ?? currentRoot;
                    field = inspectorView?.Q<PropertyField>("PropertyField:" + name);
                }
                
                if(field?.bindingPath == propertyPath)
                {
                    result = field;
                    return true;
                }

                result = currentRoot.Query<PropertyField>().Where(p => p.bindingPath == propertyPath).First();
                if (result != null)
                {
                    if (result[0] is not IMGUIContainer) return true;
                    
                    var imguiRoot = result;
                    result = result[0];
                    return TryAttachToIMGUIRoot(imguiRoot);
                }
                
                while (!string.IsNullOrEmpty(parentPath))
                {
                    var listView = currentRoot.Q<ListView>("unity-list-" + parentPath);
                    if (listView != null)
                    {
                        var foldout = listView.Q<Foldout>("unity-list-view__foldout-header");
                        if (foldout != null)
                        {
                            void FoldoutValueChanged(ChangeEvent<bool> evt)
                            {
                                if (!evt.newValue)
                                {
                                    return;
                                }

                                foldout.UnregisterValueChangedCallback(FoldoutValueChanged);
                                foldout.OnGeometryChanged(_ => TryAttachToPropertyField(foldout), onlyOnce: true);
                            }
                            
                            foldout.UnregisterValueChangedCallback(FoldoutValueChanged);
                            foldout.RegisterValueChangedCallback(FoldoutValueChanged);
                        }
                        result = null;
                        return false;
                    }
                    
                    var foldoutView = currentRoot.Q<Foldout>("unity-foldout-" + parentPath);
                    if (foldoutView != null)
                    {
                        void FoldoutValueChanged(ChangeEvent<bool> evt)
                        {
                            if (!evt.newValue)
                            {
                                return;
                            }

                            foldoutView.UnregisterValueChangedCallback(FoldoutValueChanged);
                            foldoutView.OnGeometryChanged(_ => TryAttachToPropertyField(foldoutView), onlyOnce: true);
                        }
                        
                        foldoutView.UnregisterValueChangedCallback(FoldoutValueChanged);
                        foldoutView.RegisterValueChangedCallback(FoldoutValueChanged);
                        result = null;
                        return false;
                    }
                    
                    var parentField = currentRoot.Q<PropertyField>("unity-property-field-" + parentPath);
                    if (parentField != null && parentField.childCount > 0 && parentField[0] is IMGUIContainer container)
                    {
                        result = container;
                        return TryAttachToIMGUIRoot(parentField);
                    }
                    
                    (parentPath, name) = SplitPath(parentPath);
                }
                
                return false;
            }
            
            private static (string parentPath, string name) SplitPath(string propertyPath, bool collapseArray = true)
            {
                var index = propertyPath.LastIndexOf('.');
                if(index >= 0 && collapseArray && propertyPath[(index + 1)..].EndsWith("]"))
                {
                    index = propertyPath.LastIndexOf('.', index - 1);
                }
                return index < 0 
                    ? (string.Empty, propertyPath) 
                    : (propertyPath[..index], propertyPath[(index + 1)..]);
            }

            private void TryRetrieveValueField(VisualElement field, string propertyPath)
            {
                valueView = field.Q<ListView>("unity-list-" + propertyPath);
                if (valueView != null)
                {
                    valueLabel = valueView.Q<Label>(null, Foldout.textUssClassName);
                    return;
                }
                
                valueView = field.Q<Foldout>("unity-foldout-" + propertyPath);
                if (valueView != null)
                {
                    valueLabel = valueView.Q<Label>(null, Foldout.textUssClassName);
                    return;
                }

                valueView = field.Q(null, BaseField<int>.ussClassName);
                valueLabel = valueView?.Q<Label>(null, BaseField<int>.labelUssClassName);
            }

            private void ApplyIsBoundValue(bool isBound, bool updateImmediately = true, bool forced = false)
            {
                if (isBound)
                {
                    isBoundView.RemoveFromClassList("transparent");
                }
                else if (panel != null && valueLabel != null && isBoundView?.ClassListContains("transparent") == true)
                {
                    valueLabel.schedule.Execute(() => isBoundView.RemoveFromClassList("transparent"));
                }
                
                if (IsBoundProperty != null && _properties.isBoundValue.isMixedValue == true)
                {
                    SetBindDataViewVisibility(false);

                    isBoundView.tooltip = "Bind this field for all targets";
                    isBoundView.RemoveFromHierarchy();
                    isNotBindedSlot.parent.Insert(isNotBindedSlot.index, isBoundView);

                    return;
                }
                
                isBoundView.EnableInClassList("bs-bind-toggle--on", isBound);
                isBoundView.EnableInClassList("bs-bind-toggle--off", !isBound);
                
                EnableInClassList("is-bound", isBound);
                
                SetBindDataViewVisibility(isBound);
                valueView?.EnableInClassList("hidden", isBound);
                valueLabel?.EnableInClassList("hidden", isBound);

                if (valueLabel != null && TrySetLabel(valueLabel.text) == null)
                {
                    valueLabel.schedule.Execute(() => TrySetLabel(valueLabel.text)).ExecuteLater(1);
                }
                
                if(isBound == _wasBound && !forced)
                {
                    return;
                }
                
                _wasBound = isBound;

                if (updateImmediately)
                {
                    UpdateIsBoundView();
                }
                else
                {
                    isBoundView.schedule.Execute(UpdateIsBoundView).ExecuteLater(0);
                }

                _properties.isBoundValue?.Update();

                void UpdateIsBoundView()
                {
                    isBoundView.RemoveFromHierarchy();

                    if (isBound)
                    {
                        isBoundView.tooltip = "Unbind this field";
                        isBindedSlot.parent.Insert(isBindedSlot.index, isBoundView);
                    }
                    else
                    {
                        isBoundView.tooltip = "Bind this field";
                        isNotBindedSlot.parent.Insert(isNotBindedSlot.index, isBoundView);
                    }

                    if (this[0] != _background)
                    {
                        Insert(0, _background);
                    }
                }
            }

            private void SetBindDataViewVisibility(bool visible)
            {
                if (!visible)
                {
                    bindDataView?.SetVisibility(false);
                }
                else
                {
                    GetBindDataView()?.SetVisibility(true);
                }
            }

            private VisualElement GetBindDataView()
            {
                if (bindDataView != null) return bindDataView;

                if (!_properties.property.IsAlive())
                {
                    return null;
                }

                bindDataView = new PropertyField()
                        {
                            tooltip = _properties.property.tooltip,
                        }
                        .AddBSStyle()
                        .WithClass("bs-bind-data")
                        .WithClass("bs-bind-elem")
                        .EnsureBind(_properties.bindData)
                        .WithLabel(valueLabel?.text ?? _properties.label?.text)
                        .OnBind(p =>
                        {
                            var labelText = valueLabel?.text ?? _properties.label?.text;
                            TrySetLabel(labelText);
                            if(!_properties.property.IsAlive())
                            {
                                return;
                            }
                            p.tooltip = _properties.property.tooltip;
                        });

                Add(bindDataView);

                return bindDataView;
            }

            public void Dispose()
            {
                if(_disposed)
                {
                    return;
                }
                _disposed = true;
                
                valueView?.Blur();
                valueLabel?.WithoutClassDelayed("bs-bind-proxy__label");
                isBoundView?.WithClassDelayed("transparent");
                isBoundView?.UnregisterValueChangedCallback(ApplyBoundValueChanged);
                specialBindButton?.WithClassDelayed("transparent");
                schedule.Execute(() =>
                {
                    valueView?.RemoveBSStyle();
                    isBoundView?.RemoveFromHierarchy();
                    RemoveFromHierarchy();
                }).ExecuteLater(800);

                if (_isMaterialProperty)
                {
                    MaterialProxyDrawer.UnregisterView(this);
                }
                
                Cleanup();
                
                OnDisposed?.Invoke(this);
            }

            private void Cleanup()
            {
                PropertyStackScope = null;
                _imguiVisibilityTester?.RemoveFromHierarchy();
                _imguiVisibilityTester = null;
                _imguiLabel = null;
                
                _targetSerializedObject?.Dispose();
                _targetSerializedObject = null;
                
                _imguiDrawer?.RemoveView(this, disposeView: true);
                _onCleanup?.Invoke();
                _onCleanup = null;
            }

            public void OnRemoveFromList()
            {
                if (bindDataView is PropertyField field)
                {
                    var bindDataUI = field.Q<BindDataDrawer.BindDataUI>();
                    bindDataUI?.Cleanup();
                }
            }
        }
    }
}