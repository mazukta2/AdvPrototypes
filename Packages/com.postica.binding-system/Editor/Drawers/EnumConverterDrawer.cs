using System;
using System.Linq;
using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem.Converters
{
    [CustomPropertyDrawer(typeof(EnumUnityObjectConverter.Choices), true)]
    [CustomPropertyDrawer(typeof(EnumConverter<>.Choices), true)]
    [CustomPropertyDrawer(typeof(EnumConverter<,>.Choices), true)]
    class EnumConverterDrawer : StackedPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var (enumType, toType) = GetEnumType(property);
            if (enumType == null)
            {
                Debug.LogError("EnumConverterDrawer: Enum type is null for " + property.propertyPath);
                return new Label("Enum type not found.");
            }
            
            var listProperty = property.FindPropertyRelative(nameof(EnumConverter<int>.Choices.values));
            var fallbackProperty = property.FindPropertyRelative(nameof(EnumConverter<int>.Choices.fallback));
            
            var foldout = new Foldout
            {
                text = $"Enum Mapping ({enumType.Name.RT().Bold()} \u2192 {toType.Name.RT().Bold()})",
                value = true,
                viewDataKey = "EnumConverterDrawer_" + property.propertyPath
            }.WithClass("enum-converter__foldout", "common-target-foldout");
            
            var enums = GetEnums(enumType);
            
            while(listProperty.arraySize > enums.Length)
            {
                listProperty.DeleteArrayElementAtIndex(listProperty.arraySize - 1);
            }
            
            while(listProperty.arraySize < enums.Length)
            {
                listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
            }

            // if (fallbackProperty != null)
            // {
            //     var fallbackField = GetEnumPropertyField(fallbackProperty, "Fallback", 0, toType).WithClass("enum-converter__fallback-field");
            //     foldout.Add(fallbackField);
            // }

            for (int i = 0; i < enums.Length; i++)
            {
                var prop = listProperty.GetArrayElementAtIndex(i);
                var propertyField = GetEnumPropertyField(prop, enums[i].name, enums[i].value, toType);
                propertyField.EnableInClassList("first", i == 0);
                propertyField.EnableInClassList("last", i == enums.Length - 1);
                foldout.Add(propertyField);
            }
            
            return foldout;
        }

        private PropertyField GetEnumPropertyField(SerializedProperty property, string name, int value, Type toType)
        {
            var propertyField = new PropertyField(property, name)
            {
                tooltip = $"Value: {value}, Type: {toType.Name}"
            }.WithClass("enum-converter__property-field");
            propertyField.BindProperty(property);
            return propertyField;
        }

        private (string name, int value)[] GetEnums(Type enumType)
        {
            var enumNames = Enum.GetNames(enumType);
            var enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
            var enums = new (string name, int value)[enumNames.Length];
            for (int i = 0; i < enumNames.Length; i++)
            {
                enums[i] = (enumNames[i], enumValues[i]);
            }
            Array.Sort(enums, (a, b) => a.value.CompareTo(b.value));
            return enums;
        }

        private (Type enumType, Type toType) GetEnumType(SerializedProperty property)
        {
            var parent = property.GetParent();
            if (parent == null)
            {
                Debug.LogError("EnumConverterDrawer: Unable to find parent property for " + property.propertyPath);
                return (null, null);
            }

            var converter = parent.GetValue();
            if (converter is EnumConverters.IEnumConverter enumConverter)
            {
                var toType = enumConverter.ToType;
                if (enumConverter is IContravariantConverter contravariantConverter)
                {
                    toType = contravariantConverter.ActualTargetType;
                }
                return (enumConverter.EnumType, toType);
            }
            
            Debug.LogError("EnumConverterDrawer: No EnumConverter found for " + property.propertyPath);
            return (null, null);
        }
    }
}