using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;

using Object = UnityEngine.Object;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Profiling;
using System.Linq;
using Postica.Common;
using Sirenix.OdinInspector.Editor.Internal.UIToolkitIntegration;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Postica.BindingSystem.Odin
{

    class BindDrawer
    {
        private struct Properties
        {
            public InspectorProperty property;
            public InspectorProperty bindData;
            public InspectorProperty value;
            public InspectorProperty isBound;
            public GUIContent label;
            public bool hasLabel;


            public Properties(InspectorProperty prop)
            {
                property = prop;
                bindData = prop.Children["_bindData"] ?? prop.FindChild(p => p.Name == "_bindData", true);
                value = prop.Children["_value"];
                isBound = prop.Children["_isBound"];
                label = prop.Label;

                hasLabel = !string.IsNullOrEmpty(label.text) || !string.IsNullOrEmpty(label.tooltip) || label.image;

                if (hasLabel && property.GetAttribute<HideLabelAttribute>() != null)
                {
                    hasLabel = false;
                }

                if (hasLabel)
                {
                    SetLabel(value, label);
                    SetLabel(bindData, label);
                }
                else
                {
                    SetLabel(value, GUIContent.none);
                    SetLabel(bindData, GUIContent.none);
                }
            }

            private static void SetLabel(InspectorProperty property, GUIContent label)
            {
                if (property == null)
                {
                    return;
                }
                property.Label = label;
            }
        }

        private readonly GUIContent _bindIconContent = new GUIContent(string.Empty, "Bind this field");
        private readonly GUIContent _tempContent = new GUIContent();

        private GUIStyle _bindToggleStyle;
        private GUIStyle _bindMultiValueToggleStyle;

        private Properties _properties;
        private Rect _lastDrawnRect;

        private List<Rect> _lastDrawnValueRects;
        private int _unityPropertyDrawerIndex;

        private Toggle _isBoundToggle;
        private OdinImGuiElement _isBoundOdinElement;
        
        private OdinDrawer _owner;
        private IExposedOdinDrawer _exposedOwner;
        
        protected bool IsBindedMode => _properties.value != null && (bool)IsBoundProperty.ValueEntry.WeakSmartValue;
        protected InspectorProperty IsBoundProperty => _properties.isBound;
        protected InspectorProperty ValueProperty => _properties.value;
        protected InspectorProperty BindDataProperty => _properties.bindData;
        protected Rect LastValuePropertyRect => GetBindValueRect() ?? _lastDrawnValueRects[_unityPropertyDrawerIndex];

        public BindDrawer(OdinDrawer owner, InspectorProperty property)
        {
            _owner = owner;
            _exposedOwner = owner as IExposedOdinDrawer;
            _properties = new Properties(property);

            _bindToggleStyle = new GUIStyle()
            {
                fixedWidth = 14f,
                fixedHeight = 14f,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                stretchHeight = true,
                stretchWidth = true,

            };

            if (EditorGUIUtility.isProSkin)
            {
                _bindToggleStyle.normal.background = Icons.BindIcon_Dark_Off;
                _bindToggleStyle.onNormal.background = Icons.BindIcon_Dark_On;
            }
            else
            {
                _bindToggleStyle.normal.background = Icons.BindIcon_Lite_Off;
                _bindToggleStyle.onNormal.background = Icons.BindIcon_Lite_On;
            }

            _bindMultiValueToggleStyle = new GUIStyle(_bindToggleStyle);
            _bindMultiValueToggleStyle.onNormal.background = null;

            // Need to get the UnityPropertyDrawer of BindDataProperty and enable dontUseVisualElements
            // var drawers = BindDataProperty?.GetActiveDrawerChain().BakedDrawerArray;
            // foreach (var drawer in drawers)
            // {
            //     if (IsUnityDrawerType(drawer.GetType()))
            //     {
            //         var field = drawer.GetType().GetField("dontUseVisualElements", BindingFlags.Instance | BindingFlags.NonPublic);
            //         field?.SetValue(drawer, true);
            //     }
            // }

            var valueProperty = ValueProperty;
            if(valueProperty == null)
            {
                _lastDrawnValueRects = new List<Rect>() { new Rect() };
                _unityPropertyDrawerIndex = 0;
                return;
            }

            // Need to get the private field lastDrawnValueRects from ValueProperty and the index of the UnityPropertyDrawer
            var lastRectsField = typeof(InspectorProperty).GetField("lastDrawnValueRects", BindingFlags.Instance | BindingFlags.NonPublic);
            _lastDrawnValueRects = (List<Rect>)lastRectsField.GetValue(valueProperty);

            var drawers = valueProperty.GetActiveDrawerChain().BakedDrawerArray;
            for (int i = 0; i < drawers.Length; i++)
            {
                if (IsUnityDrawerType(drawers[i].GetType()))
                {
                    _unityPropertyDrawerIndex = i;
                    break;
                }
            }
            
            // Need to transfer BindType attribute to BindDataProperty if serialization backend is Odin
            if (property.Info.SerializationBackend.IsUnity)
            {
                return;
            }

            if (!property.Info.SerializationBackend.IsUnity)
            {
                owner.SkipWhenDrawing = true;
                return;
            }

            // TODO: Uncomment these lines when Full Odin Property support is really needed

#if ODIN_BS_PROPERTIES_SUPPORT
            var propertyType = property.ValueEntry.TypeOfValue;
            var bindType = propertyType.GetInterfaces().FirstOrDefault(t => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IBind<>));
            if (bindType == null)
            {
                return;
            }

            var bindDataType = bindType.GetGenericArguments()[0];

            BindDataProperty.Info.GetEditableAttributesList().Add(new BindTypeAttribute(bindDataType));
            BindDataProperty.RefreshSetup();
#endif
        }

        protected static bool IsUnityDrawerType(Type type)
        {
            return type.IsSubclassOf(typeof(UnityPropertyDrawer<,>))
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UnityPropertyDrawer<,>));
        }

        public void DrawPropertyLayout(GUIContent label)
        {
            Profiler.BeginSample("BindDrawerOdin.DrawPropertyLayout");

#if UNITY_2022_3_OR_NEWER
            if (GeneralDrawerConfig.Instance.EnableUIToolkitSupport && (IsBoundProperty == null ||
                                                                        IsBoundProperty.TryGetTypedValueEntry<bool>()
                                                                            ?.SmartValue == true))
            {
                OdinExtensions.SetPropertyData(_properties.property);
                _exposedOwner?.CallNextDrawer(_properties.label);
                return;
            }
#endif

            if (_lastDrawnRect.width == 0)
            {
                _lastDrawnRect = _properties.property.LastDrawnValueRect;
            }
            
            var position = _lastDrawnRect;
            position = position.Resized(position.width, position.height - EditorGUIUtility.singleLineHeight);

            var leftOffset = position.x + EditorGUI.indentLevel * 15/* - _bindToggleStyle.fixedWidth - 2*/;
            var iconRect = new Rect(leftOffset, position.y + 2, 14, 14);

            var isBoundEntry = IsBoundProperty?.TryGetTypedValueEntry<bool>();
            var isBound = isBoundEntry?.SmartValue;
            var bindStyle = isBoundEntry != null && HasMultipleDifferentValues(isBoundEntry) ? _bindMultiValueToggleStyle : _bindToggleStyle; ;
            
            if (isBound.HasValue)
            {
                if(Event.current.type == EventType.Repaint && _isBoundToggle != null)
                {
                    _isBoundToggle.RemoveFromHierarchy();
                    _isBoundToggle = null;
                    _isBoundOdinElement = null;
                }
                
                var weakValue = _properties.value?.ValueEntry.WeakSmartValue;
                if (isBound != true && weakValue is IEnumerable && weakValue is not Object && weakValue is not string)
                {
                    if (weakValue is Array array && array.Length > 0
                        || weakValue is ICollection collection && collection.Count > 0)
                    {
                        iconRect.x += 17;
                        iconRect.y += 1;
                    }
                    else
                    {
                        iconRect.x += 4;
                        iconRect.y += 1;
                    }
                }
                else if (isBound != true && _isBoundToggle != null)
                {
                    iconRect.x -= 3;
                    iconRect.y -= 1;
                }
                else if (!GeneralDrawerConfig.Instance.EnableUIToolkitSupport && _isBoundToggle == null)
                {
                    iconRect.x -= 2;
                }
                

                // Let's disable the right click over this toggle
                using (new EditorGUI.DisabledScope(Event.current.button == 1
                                                   && iconRect.Contains(Event.current.mousePosition)))
                {
                    EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Arrow);
                    if (_isBoundToggle == null)
                    {
                        isBound = DrawBindIconWorkaround(isBound);
                    }
                }
            }

            if (IsBoundProperty != null && HasMultipleDifferentValues(IsBoundProperty.ValueEntry))
            {
                var valueLabel = _tempContent;
                if (_properties.hasLabel)
                {
                    valueLabel.text = "     " + _properties.label.text;
                    valueLabel.image = _properties.label.image;
                    valueLabel.tooltip = _properties.label.tooltip;
                }
                else
                {
                    valueLabel = GUIContent.none;
                }
                iconRect.x += 2;
                GUI.Label(iconRect, "-");
                EditorGUILayout.LabelField(valueLabel, GUITools.Content("Hybrid Bind Values"), EditorStyles.centeredGreyMiniLabel);
                if (Event.current.type == EventType.Repaint)
                {
                    _lastDrawnRect = GUILayoutUtility.GetLastRect();
                }
            }
            else if (isBound == true)
            {
                Profiler.BeginSample("BindDrawerOdin.BindDataProperty");
#if !UNITY_2022_3_OR_NEWER
                OdinExtensions.SetIndentLevel(BindDataProperty);
#endif
                BindDataProperty.Draw();
                Profiler.EndSample();
                _lastDrawnRect = BindDataProperty.LastDrawnValueRect;
            }
            else
            {
                Profiler.BeginSample("BindDrawerOdin.ValueProperty");

                var valueLabel = _tempContent;
                if (_properties.hasLabel)
                {
                    valueLabel.text = "     " + _properties.label.text;
                    valueLabel.image = _properties.label.image;
                    valueLabel.tooltip = _properties.label.tooltip;
                }
                else
                {
                    valueLabel = GUIContent.none;
                }

                if (ValueProperty != null)
                {
                    if (_properties.hasLabel)
                    {
                        ValueProperty.Draw(valueLabel);
                        if (Event.current.type == EventType.Repaint)
                        {
                            _lastDrawnRect = LastValuePropertyRect;
                        }
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(iconRect.width + 3);
                        ValueProperty.Draw(valueLabel);
                        EditorGUILayout.EndHorizontal();
                        if (Event.current.type == EventType.Repaint)
                        {
                            _lastDrawnRect = LastValuePropertyRect;
                            _lastDrawnRect.x -= iconRect.width;
                        }
                    }
                    if (iconRect.Contains(Event.current.mousePosition))
                    {
                        EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Arrow);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(valueLabel, GUITools.Content("Incompatible Type"), EditorStyles.centeredGreyMiniLabel);
                    if (Event.current.type == EventType.Layout)
                    {
                        _lastDrawnRect = _properties.property.LastDrawnValueRect;
                    }
                }

                Profiler.EndSample();

            }

            // This is a hack to draw over the value property
            if (isBound.HasValue)
            {
                // Workaround as it seems the style is not considered when using GUI.Toggle
                isBound = DrawBindIconWorkaround(isBound);
            }

            var canApplyIsBoundValue = _isBoundToggle == null || Event.current.type == EventType.Repaint;
            if (isBound.HasValue && isBound != isBoundEntry.SmartValue && canApplyIsBoundValue)
            {
                isBoundEntry.SmartValue = isBound.Value;
                //Property.Update(true);
            }

            Profiler.EndSample();

            bool? DrawBindIconWorkaround(bool? value)
            {
                if(!value.HasValue)
                {
                    return null;
                }
                
                if (_isBoundToggle != null)
                {
                    GUILayout.BeginArea(iconRect);
                    ImguiElementUtils.EmbedVisualElementAndDrawItHere(_isBoundOdinElement);
                    GUILayout.EndArea();
                    return _isBoundToggle.value;
                }
                
                var newValue = GUI.Toggle(iconRect, value.Value, _bindIconContent, bindStyle);
                return newValue;
            }
        }

        private void ApplyIsBindedValue(bool isBound)
        {
            _isBoundToggle.SetValueWithoutNotify(isBound);
            _isBoundToggle.EnableInClassList("bs-bind-toggle--on", isBound);
            _isBoundToggle.tooltip = isBound ? "Unbind this field" : "Bind this field";
        }
        
        private Rect? GetBindValueRect()
        {
            var valueProperty = ValueProperty;
            if (valueProperty == null)
            {
                return null;
            }
            foreach (var drawer in valueProperty.GetActiveDrawerChain().BakedDrawerArray)
            {
                if (drawer is BindValueRectAttributeDrawer bindValueRectDrawer)
                {
                    return bindValueRectDrawer.Rect;
                }
            }
            return null;
        }

        private static bool HasMultipleDifferentValues(IPropertyValueEntry value)
        {
            if (value == null)
            {
                return false;
            }
            return value.ValueState == PropertyValueState.PrimitiveValueConflict
                || value.ValueState == PropertyValueState.ReferenceValueConflict
                || value.ValueState == PropertyValueState.CollectionLengthConflict
                || value.ValueState == PropertyValueState.ReferencePathConflict;
        }
    }
}