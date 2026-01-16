using Postica.Common;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(BindDataParameter))]
    class BindParameterDrawer : StackedPropertyDrawer
    {
        private Dictionary<string, Data> _data = new Dictionary<string, Data>();

        private class Data
        {
            public SerializedProperty valueProperty;
            public PropertyDrawer drawer;
            public long managedReferenceId;
            public bool isGenericValue;
            public SerializedPropertyType propertyType;
        }

        private Data GetData(SerializedProperty property)
        {
            if(_data.TryGetValue(property.propertyPath, out Data data)
                && data.isGenericValue == string.IsNullOrEmpty(property.FindPropertyRelative("_typename").stringValue)
                && data.valueProperty.IsAlive()
                && (data.propertyType != SerializedPropertyType.ManagedReference
                    || data.valueProperty.managedReferenceId == data.managedReferenceId))
            {
                return data;
            }

            data = new Data();

            var typenameProperty = property.FindPropertyRelative("_typename");

            if (string.IsNullOrEmpty(typenameProperty.stringValue))
            {
                data.valueProperty = property.FindPropertyRelative("_value");
                data.managedReferenceId = data.valueProperty.managedReferenceId;
                data.propertyType = data.valueProperty.propertyType;
                data.isGenericValue = true;
            }
            else
            {
                data.valueProperty = property.FindPropertyRelative("_unityObject");
                data.managedReferenceId = -1;
                data.propertyType = data.valueProperty.propertyType;
                data.isGenericValue = false;
            }

            var drawType = string.IsNullOrEmpty(typenameProperty.stringValue)
                         ? data.valueProperty.GetPropertyType()
                         : typeof(ReadOnlyBindLite<Object>);

            if (drawType != null)
            {
                data.drawer = DrawerSystem.GetDrawerFor(drawType, data.valueProperty);
            }

            _data[property.propertyPath] = data;
            return data;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = GetData(property);

            if (data.drawer != null)
            {
                data.drawer.OnGUI(position, data.valueProperty, label);
            }
            else
            {
                EditorGUI.PropertyField(position, data.valueProperty, label, data.valueProperty.isExpanded);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var data = GetData(property);

            if (data.drawer != null)
            {
                return data.drawer.GetPropertyHeight(data.valueProperty, label);
            }

            return EditorGUI.GetPropertyHeight(data.valueProperty, label, data.valueProperty.isExpanded);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var data = GetData(property);
            if (data.drawer != null)
            {
                var view = data.drawer.CreatePropertyGUI(data.valueProperty);
                if (view == null)
                {
                    return null;
                }


                return view;
            }

            return new PropertyField(data.valueProperty, property.displayName);
        }
    }
}