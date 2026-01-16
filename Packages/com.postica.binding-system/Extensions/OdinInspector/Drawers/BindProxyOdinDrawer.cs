using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Postica.Common;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Postica.BindingSystem.Odin
{
    internal class BindProxyOdinDrawer : OdinDrawer, IDisposable
    {
        private BindProxyDrawer.BindProxyView _view;
        private bool _isActive;
        private ProxyDrawerChain _chain;
        private Rect _rect;
        private bool _ready;
        private List<ListAuxData> _dataToProcess = new List<ListAuxData>();
        private string _pathToApply;
        private List<string> _groupProperties = new List<string>();
        private Rect _worldParentViewRect;
        
        private EditorWindow _owningWindow;
        
        public EditorWindow OwningWindow
        {
            get
            {
                if (_owningWindow == null && _view?.panel != null)
                {
                    var ownerProperty = _view.panel.GetType().GetProperty("ownerObject");
                    if (ownerProperty == null)
                    {
                        return null;
                    }
                    var owner = ownerProperty.GetValue(_view.panel);
                    if (owner == null)
                    {
                        return null;
                    }

                    var windowProperty = owner.GetType().GetProperty("actualView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (windowProperty == null)
                    {
                        return null;
                    }
                    
                    var view = windowProperty.GetValue(owner);
                    if (view is EditorWindow window)
                    {
                        _owningWindow = window;
                    }
                }
                return _owningWindow;
            }
        }
        
        private class ListAuxData
        {
            public int index;
            public int originalIndex;
            public string path;
            public string unityPath;
            public bool isRemoved;
        }

        private Dictionary<string, ListAuxData> _listData = new Dictionary<string, ListAuxData>();

        public override bool CanDrawProperty(InspectorProperty property)
        {
            return false;
        }

        public BindProxyOdinDrawer()
        {
        }

        public BindProxyOdinDrawer(InspectorProperty property, BindProxyDrawer.BindProxyView view)
        {
            _view = view;
            Initialize(property);
            IsActive = true;

            _view.OnDisposed -= ViewDisposed;
            _view.OnDisposed += ViewDisposed;
            _view.imguiData = new BindProxyDrawer.BindProxyView.IMGUIData();
            var visibilityChecker = new VisualElement();
            _view.Add(visibilityChecker);
            visibilityChecker.schedule.Execute(() =>
            {
                if (_view == null)
                {
                    visibilityChecker.RemoveFromHierarchy();
                    UpdateCollectionStates();
                    return;
                }
                var rect = view.layout;
                if (property.LastDrawnValueRect.width <= 1)
                {
                    view.AddToClassList("hidden");
                }
                else if (view.IsBound && rect.y <= 5 && !(Property.IsChildOf(Property.Tree.RootProperty) && Property.Index == 0))
                {
                    view.AddToClassList("hidden");
                }

                if (_dataToProcess.Count == 0)
                {
                    return;
                }

                UpdateCollectionStates();
            }).Every(10);

            TryHookToCollections(property);

            void UpdateCollectionStates()
            {
                for (int i = 0; i < _dataToProcess.Count; i++)
                {
                    var data = _dataToProcess[i];
                    if (data.isRemoved)
                    {
                        _view?.RemoveTarget();
#if BS_DEBUG
                        Debug.Log($"Removed {data.path}");
#endif
                    }
                    else
                    {
                        // We have a shift
                        var oldPath = data.unityPath + $".Array.data[{data.originalIndex}]";
                        var unityPath = data.unityPath + $".Array.data[{data.index}]";
#if BS_DEBUG
                        Debug.Log($"Local Updated {oldPath} to {unityPath}");
#endif
                        var fullPath = view.TargetPath;
                        var updatedPath = fullPath.Replace(oldPath, unityPath);
#if BS_DEBUG
                        Debug.Log($"Updated {fullPath} to {updatedPath}");
#endif
                        _pathToApply = updatedPath;
                        TryUpdatePropertyTarget();
                        visibilityChecker.RemoveFromHierarchy();
                    }
                }

                _dataToProcess.Clear();
            }
        }

        private void TryUpdatePropertyTarget()
        {
            if (_pathToApply == null)
            {
                return;
            }

            var newPath = _pathToApply;
            _pathToApply = null;
            var newProperty = Property.Tree.GetPropertyAtUnityPath(newPath);
            var view = _view;
            Dispose();
            view.ChangePath(newPath, false);
            _ = new BindProxyOdinDrawer(newProperty, view);
        }

        private void TryHookToCollections(InspectorProperty property)
        {
            var parent = property.FindParent(p => p.ChildResolver is ICollectionResolver, false);
            if (parent == null)
            {
                return;
            }

            var fullPath = property.Path;
            while (parent is { ChildResolver: ICollectionResolver collectionResolver })
            {
                var path = parent.Path;
                var collectionElement = parent.FindChild(c => fullPath.StartsWith(c.Path), false);

                var listData = new ListAuxData()
                {
                    path = path,
                    unityPath = parent.UnityPropertyPath,
                    index = collectionElement.Index,
                    originalIndex = collectionElement.Index
                };

                _listData[path] = listData;

                collectionResolver.OnAfterChange += change =>
                {
                    var needsUpdate = false;
                    if (change.ChangeType == CollectionChangeType.RemoveIndex && change.Index <= listData.index)
                    {
                        listData.isRemoved = change.Index == listData.index;
                        listData.index--;
                        needsUpdate = true;
                        _view?.RemoveTarget();
                    }
                    else if (change.ChangeType == CollectionChangeType.Insert)
                    {
                        if (listData.isRemoved)
                        {
                            listData.index = change.Index;
                            listData.isRemoved = false;
                            needsUpdate = true;
                        }
                        else if (change.Index <= listData.index)
                        {
                            listData.index++;
                            needsUpdate = true;
                        }
                    }

                    if (needsUpdate && !_dataToProcess.Contains(listData))
                    {
                        _dataToProcess.Add(listData);
                    }
                };
                parent = parent.Parent;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_view == null)
            {
                return;
            }

            var parent = Property.Parent;
            while (parent != null)
            {
                if (parent.Info.PropertyType == PropertyType.Group)
                {
                    _groupProperties.Add(parent.Path);
                }
                parent = parent.Parent;
            }
        }

        private void ViewDisposed(BindProxyDrawer.BindProxyView obj) => IsActive = false;

        public override bool CanDrawTypeFilter(Type type) => true;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                {
                    return;
                }

                _isActive = value;
                _chain ??= new ProxyDrawerChain(Property);

                if (_isActive)
                {
                    _chain.Drawers.Remove(this);
                    var lastAttributeDrawer = _chain.GetLastAttributeDrawerIndex();
                    _chain.Drawers.Insert(lastAttributeDrawer - 1, this);
                    _chain.Install();
                }
                else
                {
                    _chain.Uninstall();
                }
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_view == null)
            {
                return;
            }
            
            _view.TrySetIMGUILabel(Property.Label);
            var indentShift = EditorGUI.indentLevel * 15f;
            var isCollectionHeader = Property.ChildResolver is ICollectionResolver;
            var needsFoldoutShift = false;

            if(label == null && Property.Children.Count > 0)
            {
                label = Property.Label;
                needsFoldoutShift = true;
            }
                
            if (!_view.IsBound || !_ready || !_view.IsDisplayed())
            {
                var rect = GUILayoutUtility.GetRect(15, 15, 0, 0);
                
                var shiftedLabel = label != null ? new GUIContent(label) : new GUIContent();
                shiftedLabel.text = "     " + label?.text;

                if (label == null)
                {
                    GUIHelper.PushLabelWidth(15);
                    HorizontalGroupAttributeDrawer.PushLabelWidthDefault(15);
                }

                CallNextDrawer(shiftedLabel);

                if (label == null)
                {
                    GUIHelper.PopLabelWidth();
                    HorizontalGroupAttributeDrawer.PopLabelWidthDefault();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    var lastRect = Property.LastDrawnValueRect;

                    var shiftX = needsFoldoutShift ? 15f : isCollectionHeader ? 6 : 3;
                    var shiftY = isCollectionHeader ? 2f : 0;

                    _rect = GetInspectorRelativeRect(lastRect);

                    _rect.x += shiftX;
                    _rect.y += rect.y - lastRect.y + shiftY;
                    _view.imguiData.lastRect = _rect;
                    _view.AdaptToIMGUIRect(_rect, indentShift);
                    _ready = true;
                }

                return;
            }

            var viewLayout = _view.layout;
            var updatedRect = GUILayoutUtility.GetRect(0, viewLayout.height);
            
            if (Event.current.type == EventType.Repaint)
            {
                if (needsFoldoutShift)
                {
                    updatedRect.x += 12;
                    updatedRect.width -= 12;
                }
                else if (isCollectionHeader)
                {
                    updatedRect.x += 3;
                    updatedRect.width -= 3;
                    updatedRect.y += 2;
                    updatedRect.height -= 2;
                }

                var localRect = GetInspectorRelativeRect(updatedRect);

                _view.imguiData.lastRect = localRect;
                _view.AdaptToIMGUIRect(localRect, indentShift);
            }

            _chain.SkipNextDrawers = true;
            
            Rect GetInspectorRelativeRect(Rect r)
            {
                // Convert to screen coordinates
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(r.position);
            
                // Get the Inspector's screen position
                Rect inspectorRect = OwningWindow.position; 
                Vector2 inspectorPos = new Vector2(inspectorRect.x, inspectorRect.y);
                inspectorPos += _view.parent.worldBound.position;
            
                // Calculate Inspector-relative coordinates
                Rect inspectorRelativeRect = new Rect(
                    screenPos.x - inspectorPos.x,
                    screenPos.y - inspectorPos.y,
                    r.width,
                    r.height
                );
                        
                return inspectorRelativeRect;
            }
        }

        private class ProxyDrawerChain : DrawerChain
        {
            private readonly DrawerChain _original;
            private readonly List<OdinDrawer> _drawers;
            private List<OdinDrawer>.Enumerator _enumerator;
            private bool _skipNextDrawers;

            public List<OdinDrawer> Drawers => _drawers;
            public DrawerChain OriginalChain => _original;

            private static GetterSetter<BakedDrawerChain, DrawerChain> _drawerChainProperty;

            public bool SkipNextDrawers
            {
                get => _skipNextDrawers;
                set => _skipNextDrawers = value;
            }

            public ProxyDrawerChain(InspectorProperty property) : base(property)
            {
                property.GetActiveDrawerChain().Rebake();
                _original = property.GetActiveDrawerChain().BakedChain;
                _drawers = _original.ToList();
                _drawerChainProperty ??=
                    new GetterSetter<BakedDrawerChain, DrawerChain>(
                        typeof(BakedDrawerChain).GetProperty(nameof(BakedDrawerChain.BakedChain)), false);
            }

            public void Install()
            {
                var activeChain = Property.GetActiveDrawerChain();
                _drawerChainProperty.SetValue(activeChain, this);
                activeChain.Rebake();
            }

            public void Uninstall()
            {
                var activeChain = Property.GetActiveDrawerChain();
                _drawerChainProperty.SetValue(activeChain, OriginalChain);
                activeChain.Rebake();
            }

            public int GetLastAttributeDrawerIndex()
            {
                var validatorIndex = Drawers.FindIndex(0, d => d.GetType().Name.StartsWith("ValidationDrawer`"));
                var index = Drawers.FindIndex(validatorIndex + 1, IsAttributeDrawer);
                return index == -1 ? validatorIndex + 2 : index;
            }

            private static bool IsAttributeDrawer(OdinDrawer drawer)
            {
                var parent = drawer.GetType().BaseType;
                var attributeType = typeof(OdinAttributeDrawer<,>);
                
                while (parent != null && (parent.IsGenericType == false || parent.GetGenericTypeDefinition() != attributeType))
                {
                    parent = parent.BaseType;
                }

                return parent?.IsGenericType == true && parent.GetGenericTypeDefinition() == attributeType;
            }

            public override bool MoveNext()
            {
                if (_skipNextDrawers)
                {
                    _skipNextDrawers = false;
                    return false;
                }

                return _enumerator.MoveNext();
            }

            public override void Reset() => _enumerator = _drawers.GetEnumerator();

            public override OdinDrawer Current => _enumerator.Current;

            public void AddUnique(OdinDrawer drawer)
            {
                Drawers.Clear();
                Drawers.Add(drawer);
            }
        }

        public void Dispose()
        {
            IsActive = false;
            if (_view == null)
            {
                return;
            }

            _view.OnDisposed -= ViewDisposed;
            _view = null;
        }
    }
}