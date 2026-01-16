using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal readonly partial struct ConverterHandler
        {
            public readonly IConverter instance;
            public readonly Type fromType;
            public readonly Type toType;
            public readonly bool isImplicit;
            public readonly bool isRead;

            private readonly string _typeName;
            private readonly PropertyData _data;
            private readonly SerializedProperty _property;
            private readonly ComplexReferenceProperty _properties;
            private readonly IConverterTemplate[] _templates;
            private readonly GUIContent _content;
            private readonly IConverter _implicitConverter;
            private readonly bool _canSelect;

            public ConverterHandler(Type from, Type to, PropertyData data, SerializedProperty property, bool isRead, bool isUIToolkit = false)
            {
                _data = data;

                _property = property;

                this.isRead = isRead;

                instance = property.GetValue() as IConverter;
                fromType = from;
                toType = to;

                if (fromType == null || toType == null || fromType == toType)
                {
                    _implicitConverter = null;
                    _typeName = _property.managedReferenceFullTypename;
                    _properties = default;
                    _templates = Array.Empty<IConverterTemplate>();
                    _content = null;
                    _canSelect = false;

                    isImplicit = true;

                    return;
                }

                ConvertersFactory.TryGetConverter(from, to, out _implicitConverter);

                if (instance == null)
                {
                    _property.managedReferenceValue = null;
                    instance = _implicitConverter;
                }

                _templates = ConvertersFactory.GetTemplates(from, to).ToArray();
                
                if(instance == null && _templates.Length > 0)
                {
                    instance = _templates[0].Create();
                    if (instance is IContextConverter contextConverter)
                    {
                        contextConverter.SetContext(data.sourceTarget, data.sourcePersistedType, data.properties.path.stringValue);
                    }

                    if (instance is IContravariantConverter contravariantConverter)
                    {
                        contravariantConverter.ActualTargetType = toType;
                    }
                    _property.managedReferenceValue = instance;
                }
                else if (_templates.Length > 1)
                {
                    data.serializedObject.ApplyModifiedProperties();
                    
                    if (instance is IContextConverter contextConverter)
                    {
                        contextConverter.SetContext(data.sourceTarget, data.sourcePersistedType,
                            data.properties.path.stringValue);
                    }

                    if (instance is IContravariantConverter contravariantConverter)
                    {
                        contravariantConverter.ActualTargetType = toType;
                    }

                    data.serializedObject.Update();
                }
                else
                {
                    if (instance is IContextConverter contextConverter)
                    {
                        contextConverter.SetContext(data.sourceTarget, data.sourcePersistedType,
                            data.properties.path.stringValue);
                    }

                    if (instance is IContravariantConverter contravariantConverter)
                    {
                        contravariantConverter.ActualTargetType = toType;
                    }
                }
                    

                if (instance is IRequiresValidation requiresValidation)
                {
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    requiresValidation.Validate(out var hasChanged);
                    if (hasChanged)
                    {
                        property.serializedObject.Update();
                    }
                }

                _properties = instance == null
                            ? new ComplexReferenceProperty(0)
                            : new ComplexReferenceProperty(_property, isUIToolkit: isUIToolkit);

                _typeName = _property.managedReferenceFullTypename;
                _content = new GUIContent(instance?.Id, instance?.Description);

                _canSelect = _templates.Length > 1 || (_templates.Length == 1 && _implicitConverter != null);

                isImplicit = instance == null || (_templates.Length == 0 && _properties.Length == 0);
            }
            
            public bool IsEmpty() => _content == null;

            public bool HasChanged() => _data?.isValid != true || _typeName != _property.managedReferenceFullTypename;

            
            public bool ShouldDraw() => instance != null;
        }
    }
}