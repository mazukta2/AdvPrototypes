using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;

using Object = UnityEngine.Object;
using Postica.Common;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MonoHook;
using Postica.BindingSystem.Odin;
using Postica.BindingSystem.PinningLogic;
using Postica.BindingSystem.ProxyBinding;
using Postica.Common.Reflection;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.Internal.UIToolkitIntegration;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    class OdinExtensions
    {
        private static bool _isInitialized;
        private static bool _odinInspectorEnabled;
        private static MethodHook _propertyMenuHook;
        private static Func<GeneralDrawerConfig, bool> _EnableUIToolkitSupport;
        private static Dictionary<string, BindDrawer.BindView> _currentViews = new();
        private static List<(string path, int indentLevel, int expireFrame)> _indentLevels = new();
        private static Regex _arrayIndexRegex = new(@"Array\.data\[(\d+)\]");

        public static void SetPropertyData(InspectorProperty property)
        {
            if (string.IsNullOrEmpty(property.UnityPropertyPath))
            {
                return;
            }
            
            if(!_currentViews.TryGetValue(property.UnityPropertyPath, out var view))
            {
                return;
            }

            if (EditorGUI.indentLevel > 0 && view.parent is PropertyField propertyField)
            {
                propertyField.style.marginLeft = EditorGUI.indentLevel * 15;
            }

            if (view.TrySetLabel(property.Label.text))
            {
                _currentViews.Remove(property.UnityPropertyPath);
            }
        }

        public static bool IsUsingUIToolkit
        {
            get
            {
                if(_EnableUIToolkitSupport == null)
                {
                    var property = typeof(GeneralDrawerConfig).GetProperty("EnableUIToolkitSupport");
                    if(property != null)
                    {
                        _EnableUIToolkitSupport = (Func<GeneralDrawerConfig, bool>)Delegate.CreateDelegate(typeof(Func<GeneralDrawerConfig, bool>), property.GetGetMethod());
                    }
                    else
                    {
                        _EnableUIToolkitSupport = v => false;
                    }
                }
                return _EnableUIToolkitSupport(GeneralDrawerConfig.Instance);
            }
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            EditorApplication.update -= OnExtensionUpdate;
            EditorApplication.update += OnExtensionUpdate;

            EditorApplication.delayCall += HookToContextMenu;
        }

        private static void HookToContextMenu()
        {
            if (_propertyMenuHook != null)
            {
                return;
            }

            var originalMethod = typeof(PropertyContextMenuDrawer).GetVoidMethod<InspectorProperty, GenericMenu>("PopulateGenericMenu");
            var localMethod = MethodUtils.GetMethod<InspectorProperty, GenericMenu>(PopulateGenericMenuOverride);
            var proxyMethod = MethodUtils.GetMethod<InspectorProperty, GenericMenu>(BasePopulateGenericMenu);
            
            _propertyMenuHook = new MethodHook(originalMethod, localMethod, proxyMethod);
            _propertyMenuHook.Install();
            
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                _propertyMenuHook.Uninstall();
                _propertyMenuHook = null;
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PopulateGenericMenuOverride(InspectorProperty property, GenericMenu menu)
        {
            var propertyPath = property.UnityPropertyPath;
            if (string.IsNullOrEmpty(propertyPath))
            {
                BasePopulateGenericMenu(property, menu);
                return;
            }

            var unityProperty = property.Tree.UnitySerializedObject.FindProperty(propertyPath);
            ProxyBindingSystem.AddBindMenuItems(menu, unityProperty);
            PinningSystem.PinContextualMenuProperty(menu, unityProperty);
            BasePopulateGenericMenu(property, menu);
        }
        
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void BasePopulateGenericMenu(InspectorProperty property, GenericMenu menu)
        {
            Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
        }

        private static void OnExtensionUpdate()
        {
            if (!InspectorConfig.HasInstanceLoaded)
            {
                return;
            }
            

            var odinIsEnabled = InspectorConfig.Instance.EnableOdinInInspector;
            if(_odinInspectorEnabled == odinIsEnabled)
            {
                return;
            }

            BindDataDrawer.BindDataUI.ShouldDisposeImmediately -= ShouldDisposeImmediately;
            _odinInspectorEnabled = odinIsEnabled;
            
            BindProxyDrawer.BindProxyView.UnregisterExtension(TryAttachProxyView);
            
            BindDataDrawer.OnInitialized -= OnDrawerInitialized;
            BindDataDrawer.ShouldIndentFoldouts -= ShouldIndentFoldouts;
            #if !UNITY_2022_3_OR_NEWER
            BindDataDrawer.UpdatePositionRect -= UpdatePositionRect;
            #endif

            if (!_odinInspectorEnabled)
            {
                return;
            }
            
            BindDataDrawer.BindDataUI.ShouldDisposeImmediately += ShouldDisposeImmediately;

            BindProxyDrawer.BindProxyView.RegisterExtension(TryAttachProxyView);
            
            BindDrawer.OnInitializeUIViews += (drawer, bindView) =>
            {
                bindView.isBoundView.OnAttachToPanel(evt =>
                {
                    var odinElementUI = bindView.isBoundView.GetFirstAncestorOfType<OdinImGuiElement>();
                    if (odinElementUI == null)
                    {
                        return;
                    }

                    bindView.AddToClassList("bs-bind--odin");
                    bindView.isBoundView.AddToClassList("bs-bind-toggle--odin");

                    _currentViews[bindView.PropertyPath] = bindView;
                });
                bindView.RegisterCallback<DetachFromPanelEvent>(evt => _currentViews.Remove(bindView.PropertyPath));
            };
            BindDataDrawer.OnInitialized += OnDrawerInitialized;
            BindDataDrawer.ShouldIndentFoldouts += ShouldIndentFoldouts;
#if !UNITY_2022_3_OR_NEWER
            BindDataDrawer.UpdatePositionRect += UpdatePositionRect;
#endif
        }
        
        private static bool ShouldDisposeImmediately(BindDataDrawer.BindDataUI view)
        {
            return view.IsForProxy;
        }

        private static bool TryAttachProxyView(BindProxyDrawer.BindProxyView view, VisualElement root, string propertypath)
        {
            var inspectorElement = root as InspectorElement ?? root.GetFirstAncestorOfType<InspectorElement>();
            if (inspectorElement == null)
            {
                return false;
            }

            DrawerSystem.InspectorElementProxy inspectorProxy = inspectorElement;

            if(inspectorProxy?.editor is not OdinEditor odinEditor)
            {
                return false;
            }

            view.AddToClassList("bs-bind--odin");
            view.isBoundView.AddToClassList("bs-bind-toggle--odin");

            odinEditor.Tree.UpdateTree();
            var property = GetPreciseProperty(odinEditor.Tree, propertypath);
            if (property == null)
            {
                return false;
            }
            
            var odinUpdater = new VisualElement().WithClass("bs-bind-updater--odin");

            view.AddToClassList("bs-bind-proxy--imgui");
            view.Add(odinUpdater);
            root.Add(view);
            
            view.HideInIMGUI();

            view.isBoundView.schedule.Execute(() => view.isBoundView.RemoveFromClassList("transparent"));
            _ = new BindProxyOdinDrawer(property, view);

            // odinEditor.Tree.Dispose();
            
            return true;
        }
        
        private static InspectorProperty GetPreciseProperty(PropertyTree tree, string originalPath)
        {
            var path = _arrayIndexRegex.Replace(originalPath, "$$$1");
            var property = tree.GetPropertyAtPath(path, out var closestProperty);
            if (property != null)
            {
                return property;
            }
            if(closestProperty == null)
            {
                return null;
            }
            var residualPath = path;
            var openBracketIndex = residualPath.IndexOf('[');
            if (openBracketIndex >= 0)
            {
                var arrayIndex = int.Parse(residualPath.Substring(openBracketIndex + 1, residualPath.IndexOf(']') - openBracketIndex - 1));
                return closestProperty.Children[arrayIndex];
            }
            
            return tree.GetPropertyAtUnityPath(originalPath) ?? closestProperty;
        }

        public static void SetIndentLevel(InspectorProperty property)
        {
            if (string.IsNullOrEmpty(property.UnityPropertyPath))
            {
                return;
            }

            _indentLevels.RemoveAll(l => l.path == property.UnityPropertyPath || Time.frameCount > l.expireFrame);
            _indentLevels.Add((property.UnityPropertyPath, EditorGUI.indentLevel, Time.frameCount + 3));
        }

        private static Rect UpdatePositionRect(string propertyPath, Rect position)
        {
            if(!GeneralDrawerConfig.Instance.EnableUIToolkitSupport)
            {
                return position;
            }

            for (var index = 0; index < _indentLevels.Count; index++)
            {
                var (path, indentLevel, expireFrame) = _indentLevels[index];
                
                if(Time.frameCount > expireFrame)
                {
                    _indentLevels.RemoveAt(index);
                    index--;
                    continue;
                }
                
                if (path != propertyPath)
                {
                    continue;
                }

                var offset = indentLevel * 15;
                position.x += offset;
                position.width -= offset;
                return position;
            }

            return position;
        }

        private static bool ShouldIndentFoldouts() => _odinInspectorEnabled;

        private static void OnDrawerInitialized(BindDataDrawer drawer, SerializedProperty property)
        {
            var drawerExtensions = new BindDataDrawerExtensions();
            drawerExtensions.Initialize(drawer);
        }

        private class BindDataDrawerExtensions : IDisposable
        {
            private bool _allowTargetSceneObjects = true;
            private bool _allowTargetAssetsObjects = true;

            private Rect _previewRect;
            private PropertyTree _previewTree;
            private MemberInfo _memberInfo;

            public void Initialize(BindDataDrawer drawer)
            {
                var onlySceneObjects = drawer.TryGetAttribute(null, false, out SceneObjectsOnlyAttribute _, out _);
                var onlyAssets = drawer.TryGetAttribute(null, false, out AssetsOnlyAttribute _, out _);

                _allowTargetAssetsObjects = onlyAssets || !onlySceneObjects;
                _allowTargetSceneObjects = onlySceneObjects || !onlyAssets;

                drawer.OnDrawObjectField -= OnDrawObjectField;
                drawer.OnDrawObjectField += OnDrawObjectField;

                drawer.OnTryPrepareDataPreview -= OnTryPrepareDataPreview;
                drawer.OnTryPrepareDataPreview += OnTryPrepareDataPreview;

#if ODIN_BS_PROPERTIES_SUPPORT
                drawer.GetMemberInfo -= GetMemberInfo;
                drawer.GetMemberInfo += GetMemberInfo;

                drawer.GetAttributes -= GetAttributes;
                drawer.GetAttributes += GetAttributes;
#endif
                drawer.OnDisposed -= OnDrawerDisposed;
                drawer.OnDisposed += OnDrawerDisposed;
            }

            private void OnDrawerDisposed(BindDataDrawer drawer, SerializedProperty property)
            {
                drawer.OnDrawObjectField -= OnDrawObjectField;
                drawer.OnTryPrepareDataPreview -= OnTryPrepareDataPreview;
#if ODIN_BS_PROPERTIES_SUPPORT
                drawer.GetMemberInfo -= GetMemberInfo;
                drawer.GetAttributes -= GetAttributes;
#endif
                drawer.OnDisposed -= OnDrawerDisposed;

                Dispose();
            }

            private IEnumerable<Attribute> GetAttributes(SerializedProperty property)
            {
                if (_previewTree == null || _previewTree.WeakTargets.Intersect(property.serializedObject.targetObjects)?.Count() != _previewTree.WeakTargets.Count)
                {
                    _previewTree?.Dispose();
                    _previewTree = PropertyTree.Create(property.serializedObject);
                }

                var odinProperty = _previewTree.GetPropertyAtUnityPath(property.propertyPath);
                if (odinProperty == null)
                {
                    return Array.Empty<Attribute>();
                }

                return odinProperty.Info.GetAttributes<Attribute>();
            }

            private MemberInfo GetMemberInfo(SerializedProperty property)
            {
                if (_memberInfo != null)
                {
                    return _memberInfo;
                }

                if (_previewTree == null || _previewTree.WeakTargets.Intersect(property.serializedObject.targetObjects)?.Count() != _previewTree.WeakTargets.Count)
                {
                    _previewTree?.Dispose();
                    _previewTree = PropertyTree.Create(property.serializedObject);
                }

                var odinProperty = _previewTree.GetPropertyAtUnityPath(property.propertyPath);
                if (odinProperty == null)
                {
                    _memberInfo = property.GetFieldInfo();
                }
                else
                {
                    _memberInfo = odinProperty.Info.GetMemberInfo();
                }

                return _memberInfo;
            }

            internal Object OnDrawObjectField(BindDataDrawer drawer, ref Rect rect, Object currentObject, out bool invalidValue)
            {
                invalidValue = false;
                var denyLabel = default(GUIContent);

                if (!_allowTargetAssetsObjects
                    && rect.Contains(Event.current.mousePosition)
                    && DragAndDrop.objectReferences.Length > 0)
                {
                    var refs = DragAndDrop.objectReferences;
                    for (int i = 0; i < refs.Length; i++)
                    {
                        if (EditorUtility.IsPersistent(refs[i]))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.None;
                            invalidValue = true;
                            denyLabel = drawer.contents.targetAssetsNotAllowed;
                            if (Event.current.type != EventType.Repaint
                                && Event.current.type != EventType.Layout)
                            {
                                Event.current.Use();
                            }
                            break;
                        }
                    }
                }
                else if (!_allowTargetSceneObjects
                    && Event.current.type == EventType.Repaint
                    && rect.Contains(Event.current.mousePosition)
                    && DragAndDrop.objectReferences.Length > 0)
                {
                    var refs = DragAndDrop.objectReferences;
                    for (int i = 0; i < refs.Length; i++)
                    {
                        if (!EditorUtility.IsPersistent(refs[i]))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.None;
                            invalidValue = true;
                            denyLabel = drawer.contents.targetSceneObjectsNotAllowed;
                            break;
                        }
                    }
                }

                var value = SirenixEditorFields.UnityObjectField(rect, GUIContent.none, currentObject, typeof(object), _allowTargetSceneObjects);
                if (!_allowTargetAssetsObjects && EditorUtility.IsPersistent(value))
                {
                    value = currentObject;
                }

                if (invalidValue && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rect.Inflate(-1), Color.black);
                    GUI.Label(rect, denyLabel, drawer.styles.targetInvalidValue);
                }


                rect.width -= 16;

                return value;
            }

            internal bool OnTryPrepareDataPreview(BindDataDrawer.PropertyData data, Object source, string path)
            {
                var serObject = new SerializedObject(source);
                if (!serObject.TryFindLastProperty(path, out var prop))
                {
                    return false;
                }

                if(_previewTree == null || _previewTree.WeakTargets.Count > 1 || !_previewTree.WeakTargets.Contains(source))
                {
                    _previewTree?.Dispose();
                    _previewTree = PropertyTree.Create(serObject);
                }

                var tree = _previewTree;
                var property = tree.GetPropertyAtUnityPath(prop.propertyPath);
                if(property == null)
                {
                    property = tree.GetPropertyAtPath(path.Replace('/', '.'));
                }
                if(property == null)
                {
                    return false;
                }

                property.State.Expanded = true;

                data.previewDraw = (rect, save) =>
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginArea(rect);
                    tree.BeginDraw(save);
                    property.Draw();
                    if (Event.current.type == EventType.Repaint)
                    {
                        _previewRect = property.LastDrawnValueRect;
                    }
                    tree.EndDraw();
                    GUILayout.EndArea();
                    if (EditorGUI.EndChangeCheck() && save)
                    {
                        property.ValueEntry.ApplyChanges();
                    }

                };
                data.getPreviewHeight = () => Mathf.Max(_previewRect.height, 16);

                data.previewView = new IMGUIContainer(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    tree.BeginDraw(data.pathPreviewIsEditing);
                    property.Draw();
                    if (Event.current.type == EventType.Repaint)
                    {
                        _previewRect = property.LastDrawnValueRect;
                    }
                    tree.EndDraw();
                    if (EditorGUI.EndChangeCheck() && data.pathPreviewIsEditing)
                    {
                        property.ValueEntry.ApplyChanges();
                    }
                });
                data.pathPreviewCanEdit = true;
                data.canPathPreview = true;

                return true;
            }

            public void Dispose()
            {
                _previewTree?.Dispose();
                _previewTree = null;
                _memberInfo = null;
            }
        }
    }
}