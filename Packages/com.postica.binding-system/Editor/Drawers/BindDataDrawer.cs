using Postica.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using Postica.BindingSystem.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(BindData<>))]
    [CustomPropertyDrawer(typeof(BindData))]
    [CustomPropertyDrawer(typeof(BindDataLite))]
    partial class BindDataDrawer : StackedPropertyDrawer, IDisposable
    {
        const string FocusKey = "BindDataDrawer_Focus";
        internal delegate void OnInitializedDelegate(BindDataDrawer drawer, SerializedProperty property);
        internal delegate void OnDisposedDelegate(BindDataDrawer drawer, SerializedProperty property);
        internal delegate MemberInfo GetMemberInfoDelegate(SerializedProperty property);
        internal delegate IEnumerable<Attribute> GetAttributesDelegate(SerializedProperty property);
        internal delegate void OnFocusedDelegate(Object source, string path);

        internal static OnInitializedDelegate OnInitialized;
        internal static OnFocusedDelegate OnFocused;
        private static Action OnFocusedInternal;

        private Styles _styles;
        private Contents _contents;

        internal GetMemberInfoDelegate GetMemberInfo;
        internal GetAttributesDelegate GetAttributes;
        internal OnDisposedDelegate OnDisposed;

        private bool _initialized = false;
        private bool _canChangeMode = true;
        private bool _targetLabelShift = false;
        private float _windowWidth;

        private VisualElement _uiContainer;

        private Color _editorColor;

        private Dictionary<string, PropertyData> _propertyData = new Dictionary<string, PropertyData>();

        // private Type _bindType;
        //private Action _onChanged;
        private MethodInfo _onChangedMethod;
        
        private SerializedProperty _property;
        private SerializedObject _serializedObject;

        private List<Attribute> _selfAttributes = new();
        private List<Attribute> _parentAttributes = new();

        private bool _isUIToolkit = false;
        
        private Dictionary<string, GUIContent> _tempLabels = new();
        private Dictionary<string, Type> _bindTypes = new();

        internal Styles styles => _styles ??= new Styles();
        internal Contents contents => _contents ??= new Contents();

        protected MemberInfo GetField() => GetMemberInfo?.Invoke(_property) ?? fieldInfo;
        
        public static void InitializeGlobally()
        {
            ReflectionFactory.OnClearCache -= ClearCache;
            ReflectionFactory.OnClearCache += ClearCache;
        }

        public static void Focus(Object source, string path, bool ping = true)
        {
            SessionState.SetString(FocusKey, $"{source.GetInstanceID()}:{path}");
            if(ping)
            {
                Selection.activeObject = source;
            }
            OnFocused?.Invoke(source, path);
            OnFocusedInternal?.Invoke();
        }
        
        public static bool MustBeFocused(Object source, string path, bool clearIfTrue = true)
        {
            var focus = SessionState.GetString(FocusKey, null);
            if (string.IsNullOrEmpty(focus))
            {
                return false;
            }

            var parts = focus.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var id) || source.GetInstanceID() != id
                || parts[1] != path)
            {
                return false;
            }
            
            if (clearIfTrue)
            {
                SessionState.EraseString(FocusKey);
            }
            return true;
        }
        
        private static void ClearFocus()
        {
            SessionState.EraseString(FocusKey);
        }
        
        private static bool FocusRequested()
        {
            return !string.IsNullOrEmpty(SessionState.GetString(FocusKey, null));
        }
        
        protected bool MustBeFocused(bool clearIfTrue = true)
        {
            return MustBeFocused(_serializedObject.targetObject, _property.propertyPath, clearIfTrue);
        }

        protected virtual void Initialize(SerializedProperty property)
        {
            _property = property;
            
            _serializedObject = property.serializedObject;
            _editorColor = EditorGUIUtility.isProSkin ? new Color(49, 49, 49, 1f) : new Color(200, 200, 200, 1f);

            //PrefabUtility.prefabInstanceUpdated -= PrefabUpdated;
            //PrefabUtility.prefabInstanceUpdated += PrefabUpdated;
            //Undo.undoRedoPerformed -= RefreshDrawer;
            //Undo.undoRedoPerformed += RefreshDrawer;

            InitializeAttributes(property);

            OnInitialized?.Invoke(this, property);

            AddAdditionalAttributes(property);
            
            var onChangedAttribute = GetField()?.GetCustomAttribute<BindValuesOnChangeAttribute>();
            if (onChangedAttribute != null)
            {
                var method = GetField()?.DeclaringType.GetMethod(onChangedAttribute.MethodName,
                                                                BindingFlags.Public
                                                              | BindingFlags.Instance
                                                              | BindingFlags.Static
                                                              | BindingFlags.NonPublic);
                if (method == null || method.ReturnType != typeof(void) || method.GetParameters()?.Length > 0)
                {
                    Debug.LogError($"{GetField()?.DeclaringType.FullName}: Unable to find method '{onChangedAttribute.MethodName}()' " +
                        $"specified in {nameof(BindValuesOnChangeAttribute)} on field {GetField()?.Name}");
                }
                else
                {
                    _onChangedMethod = method;
                }
            }
        }

        private void RegisterLabel(SerializedProperty property, GUIContent label)
        {
            if (_tempLabels.ContainsKey(property.propertyPath))
            {
                return;
            }
            var tempLabel = label;
            if(label.text.Equals(property.displayName, StringComparison.Ordinal)
               && property.GetParent() is { } parentProperty
               && typeof(IBind).IsAssignableFrom(parentProperty.GetPropertyType()))
            {
                tempLabel = new GUIContent(parentProperty.displayName);
            }
            _tempLabels[property.propertyPath] = tempLabel;
        }

        private void AddAdditionalAttributes(SerializedProperty property)
        {
            try
            {
                if (GetAttributes != null)
                {
                    foreach (var attr in GetAttributes(property))
                    {
                        if (_selfAttributes.Contains(attr))
                        {
                            continue;
                        }
                        _selfAttributes.Add(attr);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InitializeAttributes(SerializedProperty property)
        {
            try
            {
                var fieldInfo = property.GetFieldInfo() ?? GetField();
                _selfAttributes.AddRange(fieldInfo.GetCustomAttributes());

                var parentProperty = property.GetParent();
                if(parentProperty == null)
                {
                    return;
                }

                var parentFieldInfo = parentProperty.GetFieldInfo();
                _parentAttributes.AddRange(parentFieldInfo.GetCustomAttributes());
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal bool TryGetAttribute<T>(SerializedProperty property, bool selfAttributesOnly, out T attribute, out SerializedProperty propertyWithAttribute) where T : Attribute
        {
            property ??= _property;
            
            attribute = _selfAttributes.Find(a => a is T) as T;
            if(attribute != null)
            {
                propertyWithAttribute = property;
                return true;
            }

            if (selfAttributesOnly)
            {
                propertyWithAttribute = null;
                return false;
            }

            attribute = _parentAttributes.Find(a => a is T) as T;
            propertyWithAttribute = property.GetParent();
            return attribute != null;
        }

        internal T GetAttribute<T>(bool selfAttributesOnly) where T : Attribute
        {
            var attr = _selfAttributes.Find(a => a is T) as T;

            if (selfAttributesOnly)
            {
                return attr;
            }

            return attr ?? _parentAttributes.Find(a => a is T) as T;
        }

        public void Dispose()
        {
            PrefabUtility.prefabInstanceUpdated -= PrefabUpdated;
            Undo.undoRedoPerformed -= RefreshDrawer;

            OnDisposed?.Invoke(this, _property);
        }

        private void RefreshDrawer()
        {
            if(_propertyData == null)
            {
                return;
            }

            foreach(var pair in _propertyData)
            {
                pair.Value?.Invalidate();
            }
        }

        private void PrefabUpdated(GameObject instance)
        {
            RefreshDrawer();
        }

        private void InitializeData(SerializedProperty property, PropertyData data)
        {
            data.initialized = true;
            data.label = _tempLabels.GetValueOrDefault(property.propertyPath, new GUIContent(property.displayName));

            if (_bindTypes.TryGetValue(property.propertyPath, out var bindType) && !TryGetAttribute(property, false, out DynamicBindTypeAttribute _, out _))
            {
                data.bindType = bindType;
            }
            else if (TryGetAttribute(property, false, out BindTypeSourceAttribute bindTypeSourceAttribute, out var prop)
                && bindTypeSourceAttribute.TryGetType(prop, out var type))
            {
                data.bindType = type;
                _bindTypes[property.propertyPath] = type;
            }
            else if (TryGetAttribute(property, true, out BindTypeAttribute bindTypeAttribute, out _))
            {
                data.bindType = bindTypeAttribute.BindType
                            ?? (GetField()?.DeclaringType.IsGenericType == true ? GetField().DeclaringType.GetGenericArguments()[0] : null);
                _bindTypes[property.propertyPath] = data.bindType;
            }

            if (data.bindType != null)
            {
                data.typeIcon = new GUIContent(ObjectIcon.GetFor(data.bindType), StringUtility.NicifyName(data.bindType.GetAliasName()));
            }
            if (_onChangedMethod != null)
            {
                var parentObj = property.GetParent()?.GetValue();
                if (parentObj != null)
                {
                    data.onChanged = (Action)Delegate.CreateDelegate(typeof(Action), parentObj, _onChangedMethod);
                }
            }
            
            if (data.bindType == null)
            {
                data.properties.readConverter.managedReferenceValue = null;
            }

            data.properties.UpdateMetaValues(true);
            
            if (TryGetAttribute(property, true, out BindOverrideAttribute bindOverride, out _))
            {
                _canChangeMode = false;
                data.properties.mode.enumValueIndex = (int)bindOverride.BindMode;
                data.fixedBindMode = bindOverride.BindMode;
            }
            else if (TryGetAttribute(property, false, out BindModeAttribute bindModeAttribute, out _))
            {
                _canChangeMode = false;
                data.properties.mode.enumValueIndex = (int)bindModeAttribute.BindMode;
                data.fixedBindMode = bindModeAttribute.BindMode;
            }
            else if(TryGetAttribute(property, false, out BindAttribute bindAttribute, out _))
            {
                _canChangeMode = bindAttribute.BindMode == BindMode.ReadWrite;
                if (!_canChangeMode)
                {
                    data.properties.mode.enumValueIndex = (int)bindAttribute.BindMode;
                    data.fixedBindMode = bindAttribute.BindMode;
                }
            }

            _isUIToolkit = DrawerSystem.IsIMGUIInspector() == false;
            
            var (fromType, toType) = GetTypeMapping(data, !_isUIToolkit);

            data.preModifiers = new Modifiers(data, fromType, _isUIToolkit, true);
            data.modifiers = new Modifiers(data, toType, _isUIToolkit, false);

            data.prevType = toType ?? fromType;
            data.readConverter = fromType != null && toType != null && !toType.IsAssignableFrom(fromType)
                           ? new ConverterHandler(fromType, toType, data, data.properties.readConverter, true, _isUIToolkit)
                           : default;


            data.writeConverter = fromType != null && toType != null && !fromType.IsAssignableFrom(toType)
                           ? new ConverterHandler(toType, fromType, data, data.properties.writeConverter, false, _isUIToolkit)
                           : default;

            var parentType = GetField()?.DeclaringType;
            if (parentType?.IsGenericType == true)
            {
                var genericParentType = parentType.GetGenericTypeDefinition();
                _targetLabelShift = genericParentType == typeof(Bind<>)
                                 || genericParentType == typeof(ReadOnlyBind<>)
                                 || genericParentType == typeof(ReadOnlyBindLite<>)
                                 || genericParentType == typeof(WriteOnlyBind<>);
            }

            data.parameters = new Parameters(data);
            UpdatePathPreview(data);
        }

        private bool ShouldRenderMultiConverters(PropertyData data)
        {
            var showImplicitConverters = BindingSettings.Current.ShowImplicitConverters;
            if (data.commonReadConverter.isMixedValue == true || data.commonWriteConverter.isMixedValue == true)
            {
                return true;
            }
            else if (data.commonReadConverter.commonValue != null
                && (showImplicitConverters || !data.commonReadConverter.commonValue.IsSafe != true))
            {
                return true;
            }
            else if (data.commonWriteConverter.commonValue != null
                && (showImplicitConverters || !data.commonWriteConverter.commonValue.IsSafe != true))
            {
                return true;
            }
            return false;
        }
    }
}