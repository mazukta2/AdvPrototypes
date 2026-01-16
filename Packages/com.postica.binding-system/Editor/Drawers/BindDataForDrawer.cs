using Postica.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(BindDataFor<>))]
    class BindDataForDrawer : BindDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var innerProperty = property.FindPropertyRelative("_bindData");
            EditorGUI.PropertyField(position, innerProperty, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var innerProperty = property.FindPropertyRelative("_bindData");
            return EditorGUI.GetPropertyHeight(innerProperty, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return base.CreatePropertyGUI(property)?.WithClass("bind-data-for");
        }
    }
}