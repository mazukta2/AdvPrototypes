using Postica.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        private PropertyData GetData(SerializedProperty property)
        {
            if (!_propertyData.TryGetValue(property.propertyPath, out var data)
                || property.propertyPath != data.properties.pPath.stringValue)
            {
                bool shouldApplyChanges = data != null && !_isUIToolkit;
                data?.Invalidate();
                data = new PropertyData(property);
                
                _propertyData[property.propertyPath] = data;

                if (!data.initialized)
                {
                    InitializeData(property, data);
                }
                
                if (shouldApplyChanges)
                {
                    property.ApplyChanges();
                }
            }
            
            _serializedObject = property.serializedObject;

            return data;
        }

        private PropertyData GetDataFast(SerializedProperty property)
        {
            if(!property.IsAlive())
            {
                return null;
            }

            PropertyData data = null;
            try
            {
                if (!_propertyData.TryGetValue(property.propertyPath, out data)
                    || !data.properties.property.IsAlive()
                    || property.propertyPath != data.properties.property.propertyPath)
                {
                    data?.Invalidate();
                    data = new PropertyData(property);
                    _propertyData[property.propertyPath] = data;

                    if (!data.initialized)
                    {
                        InitializeData(property, data);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                data = new PropertyData(property);
                _propertyData[property.propertyPath] = data;

                if (!data.initialized)
                {
                    InitializeData(property, data);
                }
            }

            _serializedObject = property.serializedObject;
            
            return data;
        }
        
        private PropertyData GetDataRaw(SerializedProperty property)
        {
            if (!property.IsAlive())
            {
                return null;
            }
            
            if (!_propertyData.TryGetValue(property.propertyPath, out var data)
                || !data.properties.pPath.IsAlive()
                || property.propertyPath != data.properties.pPath.stringValue)
            {
                return null;
            }

            return data;
        }

        private PropertyData GetData(string propertyPath)
        {
            PropertyData data = null;
            try
            {
                if (_propertyData == null
                    || !_propertyData.TryGetValue(propertyPath, out data)
                    || propertyPath != data.properties.pPath.stringValue)
                {
                    var property = _serializedObject.FindProperty(propertyPath);
                    if (property != null)
                    {
                        data = GetData(property);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                var path = _serializedObject.FindProperty(propertyPath);
                return path != null ? GetData(path) : null;
            }

            return data;
        }

        private PropertyData GetDataFast(string propertyPath)
        {
            PropertyData data = null;
            if (_propertyData == null
                || !_propertyData.TryGetValue(propertyPath, out data)
                || propertyPath != data.properties.pPath.stringValue)
            {
                var property = _serializedObject.FindProperty(propertyPath);
                if (property != null)
                {
                    data = GetDataFast(property);
                }
            }
            return data;
        }

        internal readonly struct Properties
        {
            public readonly SerializedProperty property;
            public readonly SerializedProperty target;
            public readonly SerializedProperty path;
            public readonly SerializedProperty type;
            public readonly SerializedProperty valueChangedEvent;
            public readonly SerializedProperty readConverter;
            public readonly SerializedProperty writeConverter;
            public readonly SerializedProperty modifiers;
            public readonly SerializedProperty preModifiers;
            public readonly SerializedProperty mode;
            public readonly SerializedProperty pPath;
            public readonly SerializedProperty context;
            public readonly SerializedProperty parameters;
            public readonly SerializedProperty mainParameterIndex;
            public readonly SerializedProperty flags;
            public readonly SerializedProperty updateInterval;

            public BindMode BindMode => (BindMode)mode.enumValueIndex;

            public Properties(SerializedProperty prop)
            {
                property = prop.Copy();
                target = prop.FindPropertyRelative(nameof(BindData.Source));
                path = prop.FindPropertyRelative(nameof(BindData.Path));
                type = prop.FindPropertyRelative("_sourceType");
                valueChangedEvent = prop.FindPropertyRelative("_onValueChanged");
                readConverter = prop.FindPropertyRelative("_readConverter");
                writeConverter = prop.FindPropertyRelative("_writeConverter");
                mode = prop.FindPropertyRelative("_mode");
                modifiers = prop.FindPropertyRelative("_modifiers");
                preModifiers = prop.FindPropertyRelative("_preModifiers");
                pPath = prop.FindPropertyRelative("_ppath");
                context = prop.FindPropertyRelative("_context");
                parameters = prop.FindPropertyRelative("_parameters");
                mainParameterIndex = prop.FindPropertyRelative("_mainParamIndex");
                flags = prop.FindPropertyRelative("_flags");
                updateInterval = prop.FindPropertyRelative("_updateInterval");
                
                UpdateMetaValues(true);
            }

            public void UpdateMetaValues(bool updateSerializedObject)
            {
                if (property == null)
                {
                    return;
                }
                
                var shouldApply = false;
                if(pPath == null)
                {
                    var tempPath = property.FindPropertyRelative("_ppath");
                    if (tempPath == null)
                    {
                        return;
                    }
                    
                    shouldApply = updateSerializedObject && tempPath.stringValue != property.propertyPath;
                    tempPath.stringValue = property.propertyPath;
                }
                else
                {
                    shouldApply = updateSerializedObject && pPath.stringValue != property.propertyPath;
                    pPath.stringValue = property.propertyPath;
                }

                var targets = property.serializedObject.targetObjects;
                if (targets.Length == 1)
                {
                    if (context.objectReferenceValue != targets[0])
                    {
                        context.objectReferenceValue = targets[0];
                        shouldApply = updateSerializedObject;
                    }

                    if (shouldApply)
                    {
                        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }

                    return;
                }

                shouldApply = false;
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
        }

        internal sealed class MixedModifiersValue : MixedValueProperty<int>
        {
            private bool _hasElements;

            public bool hasElements => _hasElements;

            public MixedModifiersValue(SerializedObject serializedObject, string propPath)
                : base(serializedObject, propPath, (p, v) => p.arraySize = v, p => p.arraySize)
            {

            }

            public override void Update()
            {
                _updatePostponed = false;

                _isMixedValue = null;
                _commonValue = default;
                _anyValue = default;
                _hasElements = false;
                
                values.Clear();

                var firstValueSet = false;

                foreach (var target in _serializedObject.targetObjects)
                {
                    using (var so = new SerializedObject(target))
                    {
                        var soProp = so.FindProperty(_path);
                        var soPropValue = _getter(soProp);

                        _values[target] = soPropValue;

                        if (_anyValue == 0 && soPropValue > 0)
                        {
                            _anyValue = soPropValue;
                        }

                        if (!firstValueSet)
                        {
                            _commonValue = soPropValue;
                            _hasElements = soPropValue > 0;
                            firstValueSet = true;
                            continue;
                        }

                        _hasElements |= soPropValue > 0;

                        if (_commonValue == soPropValue)
                        {
                            _isMixedValue = false;
                            continue;
                        }

                        // We have a different value, no need to continue
                        _commonValue = default;
                        _isMixedValue = true;
                        return;
                    }
                }
            }
        }
        
        internal sealed class MixedObjectValue : MixedValueProperty<Object>
        {
            private bool _isMultipleTypes;
            private bool _hasNullValue;
            private Type _commonType;

            public bool isMultipleTypes => _isMultipleTypes;
            public Type commonType => _commonType;
            public bool hasNullValue => _hasNullValue;

            public MixedObjectValue(SerializedObject serializedObject, string propPath)
                : base(serializedObject, propPath, (p, v) => p.objectReferenceValue = v, p => p.objectReferenceValue)
            {

            }

            public override void Update()
            {
                _updatePostponed = false;

                _isMultipleTypes = false;
                _commonType = null;
                _commonValue = null;
                _anyValue = null;
                _hasNullValue = true;

                _isMixedValue = null;

                values.Clear();

                var commonGameObject = default(GameObject);
                var firstValueSet = false;
                var checkForCommonType = false;

                void CompleteValues()
                {
                    foreach (var target in _serializedObject.targetObjects)
                    {
                        if (values.ContainsKey(target))
                        {
                            continue;
                        }
                        using (var so = new SerializedObject(target))
                        {
                            var soTargetProp = so.FindProperty(_path);
                            var soTarget = soTargetProp.objectReferenceValue;
                            values[target] = soTarget;
                        }
                    }
                }

                foreach (var target in _serializedObject.targetObjects)
                {
                    using (var so = new SerializedObject(target))
                    {
                        var soTargetProp = so.FindProperty(_path);
                        var soTarget = soTargetProp.objectReferenceValue;

                        _hasNullValue |= soTarget == null;

                        values[target] = soTarget;

                        if (checkForCommonType)
                        {
                            if(soTarget == null)
                            {
                                continue;
                            }
                            if (soTarget.GetType().IsAssignableFrom(_commonType))
                            {
                                _commonType = soTarget.GetType();
                                continue;
                            }
                            else if ((soTarget.GetType() == typeof(GameObject) || typeof(Component).IsAssignableFrom(soTarget.GetType()))
                                && (_commonType == typeof(GameObject) || typeof(Component).IsAssignableFrom(_commonType)))
                            {
                                _commonType = typeof(GameObject);
                                continue;
                            }
                            else if (!_commonType.IsAssignableFrom(soTarget.GetType()))
                            {
                                _commonType = null;
                                CompleteValues();
                                return;
                            }
                        }

                        if(_anyValue == null && soTarget != null)
                        {
                            _anyValue = soTarget;
                        }
                        
                        if (!firstValueSet)
                        {
                            _commonValue = soTarget;
                            commonGameObject = soTarget is GameObject go ? go : soTarget is Component c ? c.gameObject : null;
                            firstValueSet = true;
                            _commonType = _commonValue?.GetType();
                            continue;
                        }

                        if ((soTarget == null && _commonValue != null)
                            || (soTarget != null && _commonValue == null))
                        {
                            _isMultipleTypes = true;
                            _commonValue = null;
                            _isMixedValue = true;
                            CompleteValues();
                            return;
                        }

                        if (_commonValue == soTarget)
                        {
                            _isMultipleTypes = false;
                            _isMixedValue = false;
                            _commonType = _commonValue?.GetType();
                            continue;
                        }

                        _isMultipleTypes |= _commonValue.GetType() != soTarget.GetType();
                        _isMixedValue = true;

                        var goTarget = soTarget is GameObject got ? got : soTarget is Component ct ? ct.gameObject : null;

                        if (goTarget != null && goTarget == commonGameObject)
                        {
                            _commonValue = commonGameObject;
                            _commonType = typeof(GameObject);
                            continue;
                        }

                        // Get the common type if there is one
                        if (soTarget.GetType().IsAssignableFrom(_commonType))
                        {
                            _commonType = soTarget.GetType();
                        }
                        else if((soTarget.GetType() == typeof(GameObject) || typeof(Component).IsAssignableFrom(soTarget.GetType()))
                            && (_commonType == typeof(GameObject) || typeof(Component).IsAssignableFrom(_commonType)))
                        {
                            _commonType = typeof(GameObject);
                        }
                        else if (!_commonType.IsAssignableFrom(soTarget.GetType()))
                        {
                            _commonType = null;
                        }

                        // We have a mistype, no need to continue
                        checkForCommonType = _commonType != null;
                        _commonValue = null;
                        continue;
                    }
                }
            }
        }

        internal partial class PropertyData
        {
            public bool initialized;
            public Action preRenderAction;
            public ConverterHandler readConverter;
            public ConverterHandler writeConverter;
            public Type prevType;
            public Type bindType;
            public string prevValue;
            public string prevPath;
            public string formattedValue;
            public string invalidPath;
            public bool hasError;
            public bool firstRun = true;
            public Properties properties;
            public Modifiers preModifiers;
            public Modifiers modifiers;
            public Parameters parameters;
            public bool isValid;
            public GUIContent typeIcon;
            public string errorMessage;
            public string errorClass;
            public bool shouldDebug;
            public DebugData debugInfo;
            public Action onChanged;
            public (bool isValid, Object value) prevTarget;
            public IDictionary<string, Accessors.AccessorPath> accessorsPaths;
            public bool shouldRefitPath;
            public BindMode? fixedBindMode;
            public GUIContent label;
            
            public RerouteData reroute;
            
            public IBindDataDebug bindDataDebug => properties.property.GetValue() as IBindDataDebug;

            public bool isPathPreview
            {
                get => BindData.BitFlags.ShowPathValuePreview.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.ShowPathValuePreview.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool isLiveDebug
            {
                get => BindData.BitFlags.LiveDebug.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.LiveDebug.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool isAutoUpdate
            {
                get => BindData.BitFlags.AutoUpdate.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.AutoUpdate.EnableFlagIn(value, properties.flags.intValue);
            }
            
            public bool updateInEditor
            {
                get => BindData.BitFlags.UpdateInEditor.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.UpdateInEditor.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool enableEvents
            {
                get => BindData.BitFlags.EnableEvents.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.EnableEvents.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool isUIInitialized
            {
                get => BindData.BitFlags.UIInitialized.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.UIInitialized.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool sourceNotNeeded
            {
                get => BindData.BitFlags.SourceNotNeeded.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.SourceNotNeeded.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool isTargetFieldCollapsed
            {
                get => BindData.BitFlags.CompactTargetView.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.CompactTargetView.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool isConverterFieldCollapsed
            {
                get => BindData.BitFlags.CompactConverterView.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.CompactConverterView.EnableFlagIn(value, properties.flags.intValue);
            }
            
            public bool isModifiersCollapsed
            {
                get => BindData.BitFlags.CompactModifiersView.IsFlagOf(properties.flags.intValue);
                set => properties.flags.intValue = BindData.BitFlags.CompactModifiersView.EnableFlagIn(value, properties.flags.intValue);
            }

            public bool canPathPreview;
            public bool pathPreviewIsEditing;
            public bool pathPreviewCanEdit;

            public readonly bool isMultipleTargets;
            
            public readonly bool hasCustomUpdates;

            public readonly MixedObjectValue commonSource;
            public readonly MixedValueProperty<BindMode> commonBindMode;
            public readonly MixedValueProperty<string> commonPath;
            public readonly MixedValueProperty<string> commonType;
            public readonly MixedValueProperty<IConverter> commonReadConverter;
            public readonly MixedValueProperty<IConverter> commonWriteConverter;
            public readonly MixedModifiersValue commonModifiers;
            public readonly MixedModifiersValue commonPreModifiers;

            private Type _sourceType;

            public SerializedObject serializedObject => properties.property.serializedObject;

            public Type sourceCurrentType => properties.target.objectReferenceValue?.GetType()
                                  ?? sourcePersistedType;
            public bool isSelfReference => string.IsNullOrEmpty(properties.path.stringValue)
                                        && ObjectIsCompatible(properties.target.objectReferenceValue, bindType); 

            public Object sourceTarget
            {
                get => properties.target.objectReferenceValue;
                set
                {
                    properties.target.objectReferenceValue = value;
                    commonSource.Update();
                }
            }

            public Type sourcePersistedType
            {
                get
                {
                    if(_sourceType == null)
                    {
                        if (!string.IsNullOrEmpty(properties.type.stringValue))
                        {
                            _sourceType = Type.GetType(properties.type.stringValue, false);
                        }
                    }
                    return _sourceType;
                }
                set
                {
                    if(_sourceType == value)
                    {
                        return;
                    }
                    _sourceType = value;
                    properties.type.stringValue = value?.AssemblyQualifiedName;
                }
            }

            public bool canShowEvents => enableEvents && properties.valueChangedEvent != null;

            public PropertyData(SerializedProperty property)
            {
                if (properties.property == null
                    || (property != null && properties.property.propertyPath != property.propertyPath))
                {
                    properties = new Properties(property);
                }
                isValid = true;
                debugInfo = new DebugData(property.serializedObject.targetObject);
                initialized = false;


                isMultipleTargets = property.serializedObject.isEditingMultipleObjects;

                hasCustomUpdates = property.GetFieldInfo()?.GetCustomAttribute<MultiUpdateAttribute>() != null;

                prevPath = properties.path.stringValue;

                commonSource = new MixedObjectValue(serializedObject, properties.target.propertyPath);
                commonPath = new MixedValueProperty<string>(serializedObject, properties.path.propertyPath, (p, v) => p.stringValue = v, p => p.stringValue);
                commonType = new MixedValueProperty<string>(serializedObject, properties.type.propertyPath, (p, v) => p.stringValue = v, p => p.stringValue);
                commonBindMode = new MixedValueProperty<BindMode>(serializedObject, properties.mode.propertyPath, (p, v) => p.enumValueIndex = (int)v, p => (BindMode)p.enumValueIndex);
                commonReadConverter = new MixedValueProperty<IConverter>(serializedObject, properties.readConverter.propertyPath, (p, v) => p.managedReferenceValue = v, p => p.managedReferenceValue as IConverter);
                commonWriteConverter = new MixedValueProperty<IConverter>(serializedObject, properties.writeConverter.propertyPath, (p, v) => p.managedReferenceValue = v, p => p.managedReferenceValue as IConverter);
                commonModifiers = new MixedModifiersValue(serializedObject, properties.modifiers.propertyPath);
                commonPreModifiers = new MixedModifiersValue(serializedObject, properties.preModifiers.propertyPath);

                reroute = RetrieveReroutePath(property);
            }

            private RerouteData RetrieveReroutePath(SerializedProperty property)
            {
                var bindDataIndex = property.propertyPath.LastIndexOf("._bindData", StringComparison.Ordinal);
                if (bindDataIndex < 0)
                {
                    return default;
                }
            
                var rootProperty = serializedObject.FindProperty(property.propertyPath.Substring(0, bindDataIndex));
                if (rootProperty == null)
                {
                    return default;
                }
            
                var objectProperty = rootProperty.FindPropertyRelative("_proxySource");
                var pathProperty = rootProperty.FindPropertyRelative("_proxyPath");
                if (objectProperty == null || pathProperty == null)
                {
                    return default;
                }

                if (!objectProperty.objectReferenceValue)
                {
                    Debug.LogError($"The proxy source is null for the BindData at path '{pathProperty.stringValue}'. Cannot retrieve reroute data.", serializedObject.targetObject);
                    return default;
                }
            
                using var so = new SerializedObject(objectProperty.objectReferenceValue);
                var dataProperty = so.FindProperty(pathProperty.stringValue);

                if (dataProperty == null)
                {
                    return default;
                }
                
                var fieldInfo = dataProperty.GetFieldInfo();
                
                var type = fieldInfo?.DeclaringType ?? (dataProperty.GetParent() == null ? objectProperty.objectReferenceValue.GetType() : null);
                var name = fieldInfo?.Name ?? dataProperty.name;
                var fieldType = fieldInfo?.FieldType ?? bindType;

                if (type == null)
                {
                    return default;
                }
                
                var hasReroute = FieldRoutes.TryGetRoute(type, name, out var reroutePath);
                
                return new RerouteData
                {
                    data = this,
                    type = type,
                    from = name,
                    fieldType = fieldType,
                    to = reroutePath,
                    toKind = hasReroute ? type?.GetMember(reroutePath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault()?.MemberType.ToString() : "[Unknown]",
                    runtimeProxyPath = rootProperty.FindPropertyRelative("_runtimeProxyPath").propertyPath
                };
            }

            internal void Invalidate() => isValid = false;

            internal void Refresh()
            {
                properties.property?.serializedObject.SetIsDifferentCacheDirty();
            }

            public void UpdateCommonValues()
            {
                commonSource.Update();
                commonPath.Update();
                commonType.Update();
                commonBindMode.Update();
                commonReadConverter.Update();
                commonWriteConverter.Update();
                commonModifiers.Update();
                commonPreModifiers.Update();
            }

            private static bool ObjectIsCompatible(Object obj, Type type)
            {
                if (!obj) { return false; }

                if (type.IsAssignableFrom(obj.GetType()))
                {
                    return true;
                }

                return ConvertersFactory.HasConversion(obj.GetType(), type, out _);
            }
            
            public struct RerouteData
            {
                public PropertyData data;
                public Type type;
                public Type fieldType;
                public string from;
                public string to;
                public string toKind;
                public string runtimeProxyPath;

                public bool isValid => type != null && !string.IsNullOrEmpty(from);
                public bool isEnabled => isValid && !string.IsNullOrEmpty(to);

                public void To(string toValue)
                {
                    to = toValue;
                    toKind = !string.IsNullOrEmpty(toValue) ? type?.GetMember(toValue, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault()?.MemberType.ToString() : "[Unknown]";
                    MarkDirty();
                }

                public void MarkDirty()
                {
                    var runtimeProp = data.serializedObject.FindProperty(runtimeProxyPath);
                    if (runtimeProp == null)
                    {
                        return;
                    }

                    runtimeProp.stringValue = "///";
                    runtimeProp.stringValue = null;
                }
            }
        }
    }
}