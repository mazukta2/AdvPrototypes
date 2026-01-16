using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    internal class PopupWindow : EditorWindow
    {
        const int RELAX_FRAMES = 5;
        
        private static readonly Dictionary<object, PopupWindow> _currentWindows = new();
        
        private Action<PopupWindow> _drawCall;
        private Action<PopupWindow> _onClose;
        private VisualElement _drawElement;
        private VisualElement _sourceElement;
        private VisualElement _container;
        private Rect _screenRect;
        private int _relaxFrame = 0;
        private int _lastModifiedFrame = 0;
        private float _sizeDelta = 0;
        private float _lastHeight = float.MinValue;
        private float _maxHeight = 0;
        private bool _isSticky = false;

        private Rect _initialPosition;
        private Rect _initialScreenRect;

        public PopupWindow HasToFit(bool value)
        {
            _relaxFrame = value ? Time.frameCount + RELAX_FRAMES : 0;
            return this;
        }
        
        public static PopupWindow Show(Rect activationRect, Vector2 size, Action<PopupWindow> drawCall)
        {
            var window = CreateInstance<PopupWindow>();
            window._drawCall = drawCall ?? throw new ArgumentNullException(nameof(drawCall));
            window.ShowAsDropDown(activationRect, size);
            return window;
        }

        public static PopupWindow Show(Rect activationRect, Vector2 size, VisualElement drawElement, bool isDynamicTransform = false, string windowId = null)
        {
            var windowKey = windowId ?? drawElement.GetHashCode().ToString();
            if(_currentWindows.TryGetValue(windowKey, out var window))
            {
                window.Close();
            }
            
            window = CreateInstance<PopupWindow>();
            _currentWindows[windowKey] = window;
            
            activationRect.width = Mathf.Min(activationRect.width, size.x);
            window._screenRect = activationRect;
            window.SetDrawElement(drawElement, isDynamicTransform);
            
            var position = window._screenRect;
            
            window.position = position;
            window.minSize = size;
            window.ShowPopup();
            
            window._initialPosition = window.position;
            window.wantsMouseMove = true;
            
            return window;
        }
        
        public static PopupWindow Show(VisualElement sourceElement, Vector2 size, VisualElement drawElement, bool isDynamicTransform = false, bool sticky = true)
        {
            if(_currentWindows.TryGetValue(sourceElement, out var window))
            {
                window.Close();
            }
            
            window = CreateInstance<PopupWindow>();
            _currentWindows[sourceElement] = window;
            
            window._sourceElement = sourceElement;
            window._screenRect = window._initialScreenRect = sourceElement.worldBound;
            window.SetDrawElement(drawElement, isDynamicTransform);
            var position = GUIUtility.GUIToScreenRect(window._screenRect);
            // window.ShowAsDropDown(position, size);
            // window.position = position;
            // window.minSize = size;
            // window.Show();
            
            window.position = position;
            window.minSize = size;
            window.ShowPopup();
            
            window._initialPosition = window.position;
            window._isSticky = sticky;

            if (sticky)
            {
                sourceElement.AddToClassList("popup-window-source");
                window.wantsMouseEnterLeaveWindow = false;
                window.wantsMouseMove = false;
            }
            else
            {
                window.wantsMouseMove = true;
            }
            
            return window;
        }
        
        public PopupWindow OnClose(Action<PopupWindow> onClose)
        {
            _onClose = onClose;
            return this;
        }

        private void OnEnable()
        {
            rootVisualElement.AddManipulator(GetWindowDragger());
            if (_drawElement == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if(_drawElement == null)
                    {
                        try
                        {
                            Close();
                        }
                        catch
                        {
                            if (this)
                            {
                                DestroyImmediate(this);
                            }
                        }
                    }
                };
            }
        }

        private DraggerEvent GetWindowDragger()
        {
            return new DraggerEvent(() => position.position, d =>
            {
                var rect = position;
                rect.position = d;
                position = rect;
                _isSticky = false;
            });
        }

        private void OnDestroy()
        {
            _sourceElement?.RemoveFromClassList("popup-window-source");
            if(_drawElement != null)
            {
                _currentWindows.Remove(_drawElement);
            }
            if(_sourceElement != null)
            {
                _currentWindows.Remove(_sourceElement);
            }
            _onClose?.Invoke(this);
        }
        
        private Rect GetSourceRect()
        {
            if (_sourceElement == null)
            {
                return new Rect();
            }
            var rect = _sourceElement.worldBound;
            rect.width = Mathf.Min(rect.width, minSize.x);
            return rect;
        }

        private void SetDrawElement(VisualElement drawElement, bool isDynamicTransform)
        {
            _container = new VisualElement().WithStyle(s =>
            {
                s.paddingLeft = 18;
            });
            _drawElement = drawElement;
            rootVisualElement.Add(_container);
            _container.StretchToParentSize();
            _container.Add(new Button(Close){ text = "\u2715", tooltip = "Close this popup", focusable = false, pickingMode = PickingMode.Position }
                .WithStyle(s =>
                {
                    s.width = 20;
                    // s.height = 20;
                    s.paddingLeft = 0;
                    s.paddingRight = 0;
                    s.paddingTop = 0;
                    s.paddingBottom = 2;
                    s.position = Position.Absolute;
                    s.marginBottom = 0;
                    s.marginTop = 0;
                    s.marginLeft = 0;
                    s.marginRight = 0;
                    // s.color = Color.red;
                    s.backgroundColor = Color.red.WithAlpha(0.5f);
                    s.unityTextAlign = TextAnchor.MiddleCenter;
                    s.fontSize = 14;
                    s.top = 0;
                    s.left = 0;
                    s.bottom = 0;
                }));
            _container.Add(drawElement);

            HasToFit(true);

            if (isDynamicTransform)
            {
                EditorApplication.delayCall += FitToContent;
                _drawElement.usageHints = UsageHints.DynamicTransform;
                HookToGeometryChanged(_drawElement);
                foreach (var child in _drawElement.Children())
                {
                    HookToGeometryChanged(child);
                }
                
                ComputeHeightExpansion();
            }
            else
            {
                EditorApplication.delayCall += FitToContent;
            }
            
            void HookToGeometryChanged(VisualElement view)
            {
                view.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    if (_lastModifiedFrame == Time.frameCount)
                    {
                        return;
                    }

                    _lastModifiedFrame = Time.frameCount;
                    if (_relaxFrame < Time.frameCount)
                    {
                        HasToFit(true);
                    }

                    ComputeHeightExpansion();
                    Repaint();
                });
            }
        }

        private void Update()
        {
            if (!_isSticky || _sourceElement == null) return;
            
            var screenRect = GetSourceRect();
            if (_screenRect.position != screenRect.position)
            {
                var delta = screenRect.position - _initialScreenRect.position;
                var positionRect = position;
                positionRect.position = _initialPosition.position + delta;
                position = positionRect;
                _screenRect = screenRect;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if(_drawCall == null)
            {
                if (_relaxFrame > Time.frameCount)
                {
                    FitToContent();
                    ReadjustOnScreen();
                    Repaint();
                    _relaxFrame = 0;
                }
                return;
            }
            try
            {
                _drawCall(this);
            }
            catch(Exception ex)
            {
                EditorGUILayout.HelpBox($"Cannot complete draw: {ex.GetType().Name}: {ex.Message}", MessageType.Error);
                Debug.LogException(ex);
            }
        }

        private void FitToContent()
        {
            ComputeHeightExpansion();
            
            var startY = _container.worldBound.yMin;
            var endY = _drawElement.worldBound.yMax;
            var height = endY - startY;
            
            if (height > _maxHeight)
            {
                _maxHeight = height;
            }

            if (float.IsNaN(height))
            {
                height = 100f; // Fallback height if the content is not defined
            }

            _sizeDelta = position.height - _maxHeight;

            maxSize = new Vector2(maxSize.x, height);
            minSize = new Vector2(minSize.x, height);

            _container.visible = true;
        }
        
        private float GetUIElementHeight()
        {
            var height = 0f;
            foreach (var child in _drawElement.Children())
            {
                height += child.layout.height;
            }
            return height;
        }
        
        private void ComputeHeightExpansion()
        {
            var height = GetUIElementHeight();
            if(_lastHeight <= height)
            {
                _drawElement.style.flexGrow = 1;
                _drawElement.style.flexShrink = 0;
            }
            else
            {
                _drawElement.style.flexGrow = 0;
                _drawElement.style.flexShrink = 1;
            }
            _lastHeight = height;
        }

        private void ReadjustOnScreen()
        {
            if (_isSticky)
            {
                return;
            }
            
            var rect = GUIUtility.GUIToScreenPoint(position.position);
            if (_screenRect.y <= rect.y || _sizeDelta <= 10)
            {
                // Correct position
                return;
            }

            var newPosition = new Vector2(position.x, position.y + _sizeDelta);
            position = new Rect(GUIUtility.GUIToScreenPoint(newPosition), position.size);
        }
    }
}