using Postica.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{
    //[CustomPropertyDrawer(typeof(DefaultValueProvider<>))]
    class DefaultValueProviderDrawer : StackedPropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("_value");
            EditorGUI.PropertyField(position, value, label, value.isExpanded);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(value, label, value.isExpanded);
        }
    }
}