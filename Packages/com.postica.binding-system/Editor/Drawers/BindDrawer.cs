using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using Postica.Common;
using UnityEngine.EventSystems;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(Bind<>))]
    [CustomPropertyDrawer(typeof(ReadOnlyBind<>))]
    [CustomPropertyDrawer(typeof(ReadOnlyBindLite<>))]
    [CustomPropertyDrawer(typeof(WriteOnlyBind<>))]
    class BindDrawer : StackedPropertyDrawer
    {
        
        public static Action<BindDrawer, BindView> OnInitializeUIViews;

        private struct Properties
        {
            public SerializedProperty property;
            public SerializedProperty bindData;
            public SerializedProperty value;
            public SerializedProperty isBoundd;
            public SerializedProperty surrogate;
            public GUIContent label;
            public bool shouldCopyValue;
            public bool hasLabel;

            public Type unityObjectType;
            public MixedValueProperty commonValue;
            public MixedValueProperty isBoundValue;

            public Properties(SerializedProperty prop, GUIContent label)
            { 
                property = prop.Copy();
                bindData = property.FindPropertyRelative("_bindData");
                value = property.FindPropertyRelative("_value");
                isBoundd = property.FindPropertyRelative("_isBound");
                this.label = label;
                shouldCopyValue = false;
                commonValue = value == null ? null : new MixedValueProperty(property.serializedObject, value.propertyPath);
                isBoundValue = isBoundd != null 
                              ? new MixedValueProperty(property.serializedObject, isBoundd.propertyPath)
                              : null;
                if (property.name.StartsWith("__b__") && property.GetFieldInfo()?.GetCustomAttribute<GeneratedBindAttribute>() != null)
                {
                    surrogate = property.serializedObject.FindProperty(property.name.Replace("__b__", ""));
                    if (surrogate != null)
                    {
                        shouldCopyValue = true;
                        this.label = new GUIContent(surrogate.displayName);
                    }
                    else
                    {
                        var labelText = label.text?.Replace("_b__", "");
                        this.label = new GUIContent(char.ToUpper(labelText[0]) + labelText.Substring(1));
                    }
                }
                else
                {
                    surrogate = null;
                }

                hasLabel = !string.IsNullOrEmpty(label.text) || !string.IsNullOrEmpty(label.tooltip) || label.image;

                unityObjectType = null;

                var bindTypeSourceAttribute = property.GetFieldInfo()?.GetCustomAttribute<BindTypeSourceAttribute>();
                if (bindTypeSourceAttribute != null
                    && bindTypeSourceAttribute.TryGetType(property, out var bindType)
                    && typeof(Object).IsAssignableFrom(bindType))
                {
                    unityObjectType = bindType;
                }
                
                UpdateMetaValues(true);
            }
            
            public void UpdateMetaValues(bool updateSerializedObject)
            {
                if (bindData == null)
                {
                    return;
                }

                var pPath = bindData.FindPropertyRelative("_ppath");
                var context = bindData.FindPropertyRelative("_context");

                pPath.stringValue = bindData.propertyPath;
                var targets = property.serializedObject.targetObjects;
                if (targets.Length == 1)
                {
                    if (context.objectReferenceValue != targets[0])
                    {
                        context.objectReferenceValue = targets[0];
                        if (updateSerializedObject)
                        {
                            property.serializedObject.ApplyModifiedProperties();
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
                    property.serializedObject.Update();
                }
            }

            private readonly bool TryGetAttribute<T>(SerializedProperty property, out T attribute) where T : Attribute
            {
                var parent = property;
                while(parent != null)
                {
                    var attr = parent.GetFieldInfo()?.GetCustomAttribute<T>();
                    if(attr != null)
                    {
                        attribute = attr;
                        return true;
                    }

                    parent = parent.GetParent();
                }

                attribute = null;
                return false;
            }
        }

        private readonly GUIContent _bindIconContent = new GUIContent(string.Empty, "Bind this field");
        private readonly GUIContent _tempContent = new GUIContent();
        private bool _initialized = false;
        private VisualElement _uiContainer;

        private GUIStyle _bindToggleStyle;
        private GUIStyle _bindMultiValueToggleStyle;

        private float _bindDataHeight;
        private Dictionary<VisualElement, Properties> _allUIProperties;
        private Properties _properties;

        private BindView _bindView;
        private string _currentPropertyPath;

        protected bool IsBoundMode => _properties.value != null && IsBoundProperty.boolValue;
        protected SerializedProperty IsBoundProperty => _properties.isBoundd;
        protected SerializedProperty ValueProperty => _properties.value;
        protected SerializedProperty BindDataProperty => _properties.bindData;
        protected SerializedProperty SurrogateProperty => _properties.surrogate;
        protected MixedValueProperty IsBoundValue => _properties.isBoundValue;
        protected MixedValueProperty CommonValue => _properties.commonValue;

        
        protected void CacheProperty(SerializedProperty property, GUIContent label)
        {
            if (!property.IsAlive())
            {
                return;
            }
            if (!_properties.property.IsAlive() || !_properties.bindData.IsAlive())
            {
                _properties = new Properties(property, label);
            }
            else if (_properties.property.propertyPath != property.propertyPath)
            {
                _properties = new Properties(property, label);
            }
        }

        protected void CacheProperty(VisualElement view, SerializedProperty property, GUIContent label)
        {
            if (!property.IsAlive())
            {
                return;
            }
            if (_allUIProperties == null)
            {
                _allUIProperties = new Dictionary<VisualElement, Properties>();
            }
            if (!_allUIProperties.TryGetValue(view, out var properties))
            {
                properties = new Properties(property, label);
                _allUIProperties[view] = properties;
            }
            else if (properties.property.propertyPath != property.propertyPath)
            {
                properties = new Properties(property, label);
                _allUIProperties[view] = properties;
            }
        }

        private void SetCurrentProperty(VisualElement view)
        {
            if (_allUIProperties.TryGetValue(view, out var properties))
            {
                _properties = properties;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheProperty(property, label);
            if (_properties.shouldCopyValue)
            {
                _properties.value.CopyFrom(_properties.surrogate);
                _properties.shouldCopyValue = false;
            }

            if(IsBoundProperty == null)
            {
                // Not a bind field
                base.OnGUI(position, property, label);
                return;
            }

            var leftOffset = position.x + EditorGUI.indentLevel * 15/* - _bindToggleStyle.fixedWidth - 2*/;
            var iconRect = new Rect(leftOffset - 1, position.y + 2, 14, 14);
#if UNITY_2022_3_OR_NEWER
            if (ValueProperty != null 
                && ValueProperty.propertyType == SerializedPropertyType.Generic 
                && !IsBoundProperty.boolValue && !DrawerSystem.IsIMGUIInspector())
            {
                iconRect.x += 15f;
            }
#endif
            var bindToggleStyle = _properties.isBoundValue.isMixedValue == true ? _bindMultiValueToggleStyle : _bindToggleStyle;

            EditorGUI.BeginProperty(iconRect, _bindIconContent, IsBoundProperty);
            // Let's disable the right click over this toggle
            using (new EditorGUI.DisabledScope(Event.current.button == 1 
                    && iconRect.Contains(Event.current.mousePosition)))
            {
                EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Arrow);
                EditorGUI.BeginChangeCheck();
                var bindValue = GUI.Toggle(iconRect, IsBoundProperty.boolValue, _bindIconContent, bindToggleStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    IsBoundProperty.boolValue = bindValue;
                    _properties.isBoundValue.Update();
                }
            }
            EditorGUI.EndProperty();
            if (_properties.isBoundValue.isMixedValue == true)
            {
                var valueLabel = _tempContent;
                if (_properties.hasLabel)
                {
                    valueLabel.text = "     " + _properties.label.text;
                    valueLabel.image = _properties.label.image;
                    valueLabel.tooltip = _properties.property.tooltip ?? _properties.label.tooltip;
                }
                else
                {
                    valueLabel = GUIContent.none;
                }
                iconRect.x += 2;
                GUI.Label(iconRect, "-");
                EditorGUI.LabelField(position, valueLabel, new GUIContent("Hybrid Bind Values"), EditorStyles.centeredGreyMiniLabel);
            }
            else if (IsBoundProperty.boolValue)
            {
                DrawBinding(position, property, _properties.label);
            }
            else
            {
                var valueLabel = _tempContent;
                if (_properties.hasLabel)
                {
                    valueLabel.text = "     " + _properties.label.text;
                    valueLabel.image = _properties.label.image;
                    valueLabel.tooltip = _properties.property.tooltip ?? _properties.label.tooltip;
                }
                else
                {
                    valueLabel = GUIContent.none;
                    position.x += iconRect.width;
                    position.width -= iconRect.width;
                }

                if (ValueProperty != null)
                {
                    var valueProperty = ValueProperty;
                    var isMixedValue = EditorGUI.showMixedValue;
                    EditorGUI.BeginProperty(position, valueLabel, ValueProperty);
                    EditorGUI.showMixedValue = _properties.commonValue.isMixedValue == true;
                    EditorGUI.BeginChangeCheck();
                    if (valueProperty.propertyType == SerializedPropertyType.ObjectReference && _properties.unityObjectType != null)
                    {
                        valueProperty.objectReferenceValue = EditorGUI.ObjectField(position,
                                              valueLabel,
                                              valueProperty.objectReferenceValue,
                                              _properties.unityObjectType,
                                              true);
                    }
                    else
                    {
                        base.OnGUI(position, valueProperty, valueLabel);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        _properties.commonValue.Update();
                    }
                    EditorGUI.showMixedValue = isMixedValue;
                    EditorGUI.EndProperty();
                    if (_properties.surrogate != null)
                    {
                        _properties.surrogate.CopyFrom(valueProperty);
                    }

                    if (valueProperty.isArray && Event.current.type == EventType.Repaint)
                    {
                        // Special draw for arrays
                        bindToggleStyle.Draw(iconRect, false, false, false, false);
                    }
                }
                else
                {
                    EditorGUI.LabelField(position, valueLabel, new GUIContent("No Editor Available"), EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
            {
                _initialized = true;
                Initialize(property, label);
            }
            CacheProperty(property, label);

            if(IsBoundProperty == null)
            {
                // Most probably not a bind field
                return base.GetPropertyHeight(ValueProperty, label);
            }
            if (IsBoundProperty.boolValue)
            {
                return GetBindingHeight(property, label);
            }
            if(ValueProperty != null)
            {
                return base.GetPropertyHeight(ValueProperty, label);
            }
            return EditorGUIUtility.singleLineHeight;
        }

        protected virtual void DrawBinding(Rect position, SerializedProperty property, GUIContent label)
        {
            var bindDataRect = new Rect(position.x, position.y, position.width, _bindDataHeight);
            EditorGUI.PropertyField(bindDataRect, BindDataProperty, label);
        }

        protected virtual float GetBindingHeight(SerializedProperty property, GUIContent label)
        {
            _bindDataHeight = EditorGUI.GetPropertyHeight(BindDataProperty);
            return _bindDataHeight/* + base.GetPropertyHeight(property, label)*/;
        }

        protected virtual void Initialize(SerializedProperty property, GUIContent label)
        {
            SetDrawProperty(property.FindPropertyRelative("_value"));
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
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _bindView = new BindView(this, property)
#if UNITY_2022_3_OR_NEWER
                .WithClass(PropertyField.inspectorElementUssClassName)
#endif
                ;

            // Temporary workaround to fix missing serialized reference properties
            _properties = default;

            // Uncomment to use the new UIElements based drawer
            CacheProperty(property, GetLabel(property));

            return _bindView;
        }

        private VisualElement BaseCreatePropertyGUI(SerializedProperty property) => base.CreatePropertyGUI(property);

        internal class BindView : VisualElement
        {
            private BindDrawer _owner;
            private Properties _properties;
            
            public Toggle isBoundView;
            public VisualElement bindDataView;
            public VisualElement valueView;
            public VisualElement hybridView;
            
            public (VisualElement parent, int index) isBindedSlot;
            public (VisualElement parent, int index) isNotBindedSlot;

            protected SerializedProperty IsBoundProperty => _properties.isBoundd;

            public string PropertyPath { get; }

            public bool TrySetLabel(string value)
            {
                if (isBoundView == null)
                {
                    return true;
                }

                if (isBoundView.value)
                {
                    var label = bindDataView?.Query<Label>(null, "bind-field__label").Where(l => l.IsDisplayed()).First();
                    if (label != null)
                    {
                        label.text = value;
                    }

                    return true;
                }

                if(valueView == null || valueView.childCount == 0)
                {
                    return false;
                }
                
                valueView.WithLabel(value);
                return true;
            }

            public BindView(BindDrawer owner, SerializedProperty property)
            {
                _owner = owner;
                _properties = new Properties(property, owner.GetLabel(property));
                
                PropertyPath = property.propertyPath;

                this.AddBSStyle().WithClass("bs-bind");
                
                BuildUIViews();
            }
            
            private void BuildUIViews()
            {
                isBindedSlot = (this, 0);
                isNotBindedSlot = (this, 0);

                if (IsBoundProperty != null)
                {
                    isBoundView = new Toggle() { tooltip = "Bind this field" }.AddBSStyle()
                        .WithClass("bs-bind-toggle");

                    isBoundView.RegisterValueChangedCallback(v => ApplyIsBindedValue(v.newValue, false));
                    isBoundView.BindProperty(IsBoundProperty);
                }
                else
                {
                    isBoundView = new Toggle()
                        {
                            tooltip = "Bind this field",
                            value = true,
                            pickingMode = PickingMode.Ignore
                        }.AddBSStyle()
                        .WithClass("bs-bind-toggle");
                }

                _properties.isBoundValue?.Update();

                ApplyIsBindedValue(IsBoundProperty == null || IsBoundProperty.boolValue);
                
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

                OnInitializeUIViews?.Invoke(_owner, this);
            }

            private void OnAttachToPanel(AttachToPanelEvent evt)
            {
                UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                
                if (!BindingSettings.Current.ShowTargetGroupReplacement)
                {
                    return;
                }
                
                var propertyFieldWithHeader = this.QueryParent<PropertyField>();
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
                    while (index-- > 0)
                    {
                        if (propParent[index] is not PropertyField sibling)
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
                }
                
                propertyFieldWithHeader.AddToClassList("property-with-header");
                propertyFieldWithHeader.parent.AddToClassList("properties-with-header");
            }

            private void ApplyIsBindedValue(bool isBound, bool updateImmediately = true)
            {
                if (IsBoundProperty != null && _properties.isBoundValue.isMixedValue == true)
                {
                    SetBindDataViewVisibility(false);
                    SetValueViewVisibility(false);
                    SetHybridBindViewVisibility(true);

                    isBoundView.tooltip = "Bind this field for all targets";
                    isNotBindedSlot.parent.Insert(isNotBindedSlot.index, isBoundView);

                    return;
                }

                SetHybridBindViewVisibility(false);
                SetBindDataViewVisibility(isBound);
                SetValueViewVisibility(!isBound);

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
                    isBoundView.EnableInClassList("bs-bind-toggle--on", isBound);

                    if (isBound && isBoundView.parent != isBindedSlot.parent)
                    {
                        isBoundView.tooltip = "Unbind this field";
                        isBoundView.RemoveFromHierarchy();
                        isBindedSlot.parent.Insert(isBindedSlot.index, isBoundView);
                    }
                    else if(!isBound && isBoundView.parent != isNotBindedSlot.parent)
                    {
                        isBoundView.tooltip = "Bind this field";
                        isBoundView.RemoveFromHierarchy();
                        isNotBindedSlot.parent.Insert(isNotBindedSlot.index, isBoundView);
                    }
                }
            }

            private void SetHybridBindViewVisibility(bool visible)
            {
                if (hybridView == null)
                {
                    hybridView = new TextField(_properties.property.displayName)
                        {
                            value = "Hybrid Bind Value",
                            isReadOnly = true,
                            pickingMode = PickingMode.Ignore,
                            showMixedValue = true,
                        }.StyleAsField().WithClass("bs-bind__label--hybrid")
                        .WithChildren(new Label("Hybrid Bind Value").WithClass("hybrid-label"),
                            new Image().WithClass("hybrid-icon"));
                    Add(hybridView);
                }

                hybridView.SetVisibility(visible);
            }

            private void SetBindDataViewVisibility(bool visible)
            {
                if (!visible)
                {
                    bindDataView?.SetVisibility(false);
                }
                else
                {
                    GetBindDataView().SetVisibility(true);
                }
            }

            private VisualElement GetBindDataView()
            {
                if (bindDataView != null) return bindDataView;

                bindDataView = new PropertyField()
                        {
                            label = _properties.label?.text,
                            tooltip = _properties.property.tooltip
                        }
                        .AddBSStyle()
                        .WithClass("bs-bind-data")
                        .WithClass("bs-bind-elem")
                        .EnsureBind(_properties.bindData);

                if (_properties.property.serializedObject.isEditingMultipleObjects)
                {
                    RebindDataView(null);
                }

                bindDataView.RegisterCallback<AttachToPanelEvent>(RebindDataView);
                Add(bindDataView);

                return bindDataView;

                void RebindDataView(AttachToPanelEvent evt)
                {
                    if (evt != null)
                    {
                        bindDataView.UnregisterCallback<AttachToPanelEvent>(RebindDataView);
                    }

                    if (!_properties.bindData.IsAlive())
                    {
                        return;
                    }

                    var propertyField = bindDataView as PropertyField;
                    if (propertyField is { childCount: 0 })
                    {
                        // Ensure the property field is bound to the correct property
                        propertyField.schedule.Execute(() =>
                        {
                            if (propertyField is { childCount: 0 })
                            {
                                propertyField.BindProperty(_properties.bindData);
                            }
                        }).ExecuteLater(0);
                    }

                    propertyField.label = _properties.label?.text;
                    propertyField.tooltip = _properties.property.tooltip;
                }
            }

            private void SetValueViewVisibility(bool visible)
            {
                if (!visible)
                {
                    valueView?.SetVisibility(false);
                }
                else
                {
                    GetValueView().SetVisibility(true);
                }
            }

            private VisualElement GetValueView()
            {
                if (valueView != null) return valueView;
                
                if (_properties.value == null)
                {
                    // No editor available
                    valueView = new VisualElement()
                            { tooltip = _properties.property.tooltip }
                        .WithClass("bs-bind-value", "no-editor").StyleAsField()
                        .WithChildren(new Label(_properties.property.displayName).StyleAsFieldLabel(),
                            new VisualElement().WithChildren(new Image().WithClass("bs-bind-value__icon"),
                                    new Label("NO EDITOR AVAILABLE"))
                                .StyleAsFieldInput().WithClass("bs-bind-value__input"))
                        .AlignField();
                    Add(valueView);
                    return valueView;
                }

                valueView = (_owner.BaseCreatePropertyGUI(_properties.value)
                                              ?.WithLabel(_properties.label.text)
                                          ?? 
                        new PropertyField() { label = _properties.label.text }.EnsureBind(
                            _properties.value))
                    .AddBSStyle()
                    .WithClass("bs-bind-value")
                    .WithClass("bs-bind-elem");
                
                valueView.RegisterCallback<TooltipEvent>(evt =>
                {
                    evt.tooltip = _properties.property.IsAlive() ? _properties.property.tooltip : "";
                    if (valueView.TryGetLabel(out var labelView))
                    {
                        evt.rect = labelView.worldBound;
                    }
                    evt.StopPropagation();
                });
                valueView.OnAttachToPanel(v =>
                {
                    if (valueView is PropertyField { childCount: 0 } propField)
                    {
                        propField.BindProperty(_properties.value);
                        valueView.schedule.Execute(AdaptValuePropertyField).ExecuteLater(0);
                    }
                    else
                    {
                        AdaptValuePropertyField();
                    }

                    if (valueView is PropertyField field)
                    {
                        field.label = _properties.label.text;
                    }
                }, 0);
                Add(valueView);

                return valueView;
            }

            private void AdaptValuePropertyField()
            {
                if (valueView == null)
                {
                    return;
                }

                var propertyLabel = valueView.Q<Label>(null, "unity-property-field__label")
                                    ?? valueView.HFind<Label>(null, "unity-property-field__label");
                propertyLabel?.AddToClassList("bind-field__label");
                
                if(valueView[0] is ObjectField objField && _owner._properties.unityObjectType != null)
                {
                    objField.objectType = _owner._properties.unityObjectType;
                }
                
                MoveBindToggleToFoldout(GetPropertyFoldout(valueView)?.WithClass("bs-bind-value__foldout"));
            }

            private void MoveBindToggleToFoldout(Foldout foldout)
            {
                if (foldout == null)
                {
                    return;
                }

                var foldoutToggle = foldout.Q(className: Foldout.inputUssClassName)
                    .WithClass("bs-bind-toggle--off-slot");
                isNotBindedSlot = (foldoutToggle, 1);
                ApplyIsBindedValue(isBoundView.value);
            }

            protected Foldout GetPropertyFoldout(VisualElement root)
            {
                var foldout = root.Q<Foldout>(className: "unity-foldout--depth-0");
                var count = 1;
                while (foldout == null && count < 5)
                {
                    foldout = root.Q<Foldout>(className: $"unity-foldout--depth-{count}");
                    count++;
                }

                return foldout;
            }
        }

        protected virtual GUIContent GetLabel(SerializedProperty property)
        {
            return new GUIContent(preferredLabel ?? property.displayName);
        }

        private Rect GetInnerDrawRect(SerializedProperty property, GUIContent label)
        {
            var height = GetPropertyHeight(property, label);
            var rect = _uiContainer.layout;
            rect.height = height;
            return rect;
        }
    }
}