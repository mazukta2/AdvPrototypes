using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Postica.Common;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial struct ModifierData
        {
            private Action<object, object> _modeSetter;
            private Func<object, object> _modeGetter;

            private Func<object, object> _readFunc;
            private Func<object, object> _writeFunc;

            private GUIContent _collapsedContent;

            public string type;
            public IModifier modifier;
            public bool isOneLine;
            public bool canChangeMode;
            public bool isHotChange;
            public GUIContent expandedContent;
            public ComplexReferenceProperty properties;

            private BindMode? _mode;
            private Type _typeToModify;
            private bool _debugMethodsReady;

            public VisualElement view;
            public VisualElement containerView;
            public string viewId;
            public Button bindModeButton;
            public Button removeButton;

            public float height;

            public Func<object, object> readFunc
            {
                get
                {
                    if (!_debugMethodsReady)
                    {
                        BuildDebugMethods();
                    }
                    return _readFunc;
                }
            }
            public Func<object, object> writeFunc
            {
                get
                {
                    if (!_debugMethodsReady)
                    {
                        BuildDebugMethods();
                    }
                    return _writeFunc;
                }
            }

            public GUIContent collapsedContent
            {
                get
                {
                    if (modifier != null)
                    {
                        _collapsedContent.text = modifier.Id + "  " + modifier.ShortDataDescription;
                    }

                    return _collapsedContent;
                }
                set { _collapsedContent = value; }
            }

            public BindMode BindMode
            {
                get
                {
                    if (_mode.HasValue) return _mode.Value;
                    try { _mode = (BindMode)_modeGetter(modifier); }
                    catch { _mode = BindMode.Read; }
                    return _mode.Value;
                }
            }

            public void SetBindMode(BindMode mode)
            {
                try
                {
                    if (_modeSetter == null || !canChangeMode) return;
                    _modeSetter.Invoke(modifier, mode);
                    _debugMethodsReady = false;
                    isHotChange = true;
                }
                finally
                {
                    _mode = mode;
                }
            }

            internal void Init(Type typeToModify)
            {
                if (modifier == null) { return; }
                
                isHotChange = false;
                
                isOneLine = modifier.GetType().GetCustomAttribute<OneLineModifierAttribute>() != null;

                var isReadWrite = modifier.GetType()
                                .GetInterfaces()
                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadWriteModifier<>));
                if (!isReadWrite)
                {
                    var modifierOptions = modifier.GetType().GetCustomAttribute<ModifierOptionsAttribute>();
                    canChangeMode = modifierOptions?.ModifierMode.HasValue == false;
                    if (modifierOptions?.ModifierMode != null)
                    {
                        _mode = modifierOptions.ModifierMode.Value;
                    }
                    else
                    {
                        var implementsWrite = modifier.GetType()
                            .GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWriteModifier<>));
                        var implementsRead = modifier.GetType()
                            .GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadModifier<>));

                        _mode = implementsRead && implementsWrite ? BindMode.ReadWrite
                            : implementsWrite ? BindMode.Write : BindMode.Read;
                    }
                }
                else
                {
                    try
                    {
                        canChangeMode = modifier.GetType().GetCustomAttribute<ModifierOptionsAttribute>()?.ModifierMode
                                            .HasValue == false
                                        || modifier.GetType().GetProperty(nameof(IReadWriteModifier<bool>.ModifyMode))
                                            .GetSetMethod(true) != null;
                        _modeGetter = modifier.GetType().GetProperty(nameof(IReadWriteModifier<bool>.ModifyMode))
                            .GetValue;
                        _modeSetter = modifier.GetType().GetProperty(nameof(IReadWriteModifier<bool>.ModifyMode))
                            .SetValue;
                    }
                    catch (NullReferenceException)
                    {
                        canChangeMode = false;
                    }
                }

                _typeToModify = typeToModify;
                _debugMethodsReady = false;
                
                // if(modifier is IObjectModifier objModifier)
                // {
                //     objModifier.TargetType = typeToModify;
                // }
            }

            private void BuildDebugMethods()
            {
                if (_typeToModify != null)
                {
                    var typeToModify = _typeToModify;
                    var readMethod = modifier.GetType()
                        .GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType 
                                          && i.GetGenericTypeDefinition() == typeof(IReadModifier<>)
                                          && i.GenericTypeArguments[0] == typeToModify)
                        ?.GetMethod(nameof(IReadModifier<bool>.ModifyRead));
                    var writeMethod = modifier.GetType()
                        .GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType
                                          && i.GetGenericTypeDefinition() == typeof(IWriteModifier<>)
                                          && i.GenericTypeArguments[0] == typeToModify)
                        ?.GetMethod(nameof(IWriteModifier<bool>.ModifyWrite));
                    _readFunc = readMethod?.GetAsFunc(modifier);
                    _writeFunc = writeMethod?.GetAsFunc(modifier);
                }
                else
                {
                    var readMethod = modifier.GetType().GetInterface("IReadModifier`1")?.GetMethod(nameof(IReadModifier<bool>.ModifyRead));
                    var writeMethod = modifier.GetType().GetInterface("IWriteModifier`1")?.GetMethod(nameof(IWriteModifier<bool>.ModifyWrite));
                    _readFunc = readMethod?.GetAsFunc(modifier);
                    _writeFunc = writeMethod?.GetAsFunc(modifier);
                }
                _debugMethodsReady = true;
            }
        }

        [HideMember]
        internal class MissingModifier : IModifier
        {
            private readonly Func<bool> _clearCallback;
            public Object Context { get; set; }
            public string Id { get; set; }
            public string ShortDataDescription { get; set; }
            public BindMode ModifyMode { get; set; } = BindMode.ReadWrite;
            
            public MissingModifier(Func<bool> clearCallback = null)
            {
                _clearCallback = clearCallback;
            }
            
            public object Modify(BindMode mode, object value)
            {
                Debug.LogWarning(BindSystem.DebugPrefix + "Corrupt modifier detected: " + Id, Context);
                return value;
            }


            public bool Clear() => _clearCallback?.Invoke() ?? false;
        }

        internal readonly partial struct Modifiers
        {
            public readonly ModifierData[] array;
            public readonly bool isPreConversion;

            private readonly IModifier[] _modifiers;
            private readonly List<MissingModifier> brokenModifiers;
            private readonly PropertyData _data;

            public Modifiers(PropertyData data, Type dataType, bool isUIToolkit, bool isPreConversion)
            {
                var modifiersProperty = isPreConversion ? data.properties.preModifiers : data.properties.modifiers;

                this.isPreConversion = isPreConversion;
                _data = data;
                // Cleanup the modifiers array first
                for (int i = 0; i < modifiersProperty.arraySize; i++)
                {
                    var property_i = modifiersProperty.GetArrayElementAtIndex(i);
                    if(property_i.managedReferenceValue == null)
                    {
                        modifiersProperty.DeleteArrayElementAtIndex(i);
                        i--;
                    }
                }
                
                array = new ModifierData[modifiersProperty.arraySize];

                _modifiers = modifiersProperty.GetValue() as IModifier[];
                for (int i = 0; i < array.Length; i++)
                {
                    var property_i = modifiersProperty.GetArrayElementAtIndex(i);
                    var modifier_i = _modifiers[i];
                    if (modifier_i == null)
                    {
                        Debug.LogError(BindSystem.DebugPrefix + $"Modifier of type {property_i.managedReferenceFullTypename} is null at index {i}", data.properties.context.objectReferenceValue);
                        array[i].type = property_i.managedReferenceFullTypename;
                        array[i].modifier = null;
                        array[i].properties = new ComplexReferenceProperty(property_i, "_mode;", isUIToolkit);
                        array[i].expandedContent = new GUIContent("Error: Unknown Modifier".RT().Color(BindColors.Error));
                        array[i].collapsedContent = new GUIContent("Error: Unknown Modifier".RT().Color(BindColors.Error));
                        array[i].viewId = property_i.GetViewDataKey() + '.' + i;
                        
                        continue;
                    }
                    
                    var icon = ObjectIcon.GetFor(modifier_i.GetType());
                    array[i].type = property_i.managedReferenceFullTypename;
                    array[i].modifier = modifier_i;
                    array[i].properties = new ComplexReferenceProperty(property_i, "_mode;", isUIToolkit);
                    array[i].properties.onChanged += modifier_i.OnValidate;
                    array[i].collapsedContent = new GUIContent(modifier_i.Id + "  " + modifier_i.ShortDataDescription, icon);
                    array[i].expandedContent = new GUIContent(modifier_i.Id, icon);
                    array[i].viewId = property_i.GetViewDataKey() + '.' + modifier_i.Id;
                    array[i].Init(dataType);
                    
                    if(modifier_i is IRequiresAutoUpdate { ShouldAutoUpdate: true })
                    {
                        data.isAutoUpdate = true;
                    }
                }

                brokenModifiers = new();
                if (modifiersProperty.serializedObject.targetObjects.Length > 1)
                {
                    // If we have multiple targets, we cannot validate the modifiers
                    return;
                }
                
                var context = data.properties.context.objectReferenceValue;
                var missingRefs = SerializationUtility.GetManagedReferencesWithMissingTypes(context);
                foreach (var missingRef in missingRefs)
                {
                    if (missingRef.className.Contains("modifier", StringComparison.OrdinalIgnoreCase))
                    {
                        var missingModifier = new MissingModifier(() => SerializationUtility.ClearManagedReferenceWithMissingType(context, missingRef.referenceId))
                        {
                            Context = context,
                            Id = missingRef.className,
                            ShortDataDescription = $"{missingRef.namespaceName}.{missingRef.className} ({missingRef.assemblyName})",
                            ModifyMode = BindMode.ReadWrite
                        }; 
                        
                        brokenModifiers.Add(missingModifier);
                    }
                }
            }

            public bool HaveChanged()
            {
                if (_data?.isValid != true) { return true; }

                var modifiersProperty = isPreConversion ? _data.properties.preModifiers : _data.properties.modifiers;
                var size = modifiersProperty.arraySize;
                if (size != array.Length)
                {
                    return true;
                }
                for (int i = 0; i < size; i++)
                {
                    var modifier = modifiersProperty.GetArrayElementAtIndex(i);
                    if (modifier.managedReferenceFullTypename != array[i].type)
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool CanDraw(int index)
            {
                var mode = (BindMode)_data.properties.mode.enumValueIndex;
                return array[index].BindMode.IsCompatibleWith(mode);;
            }

            public bool IsHotChange(int index)
            {
                return array[index].isHotChange;
            }
        }
    }
}