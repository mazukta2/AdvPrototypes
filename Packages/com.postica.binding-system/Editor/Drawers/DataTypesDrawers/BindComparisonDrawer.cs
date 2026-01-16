using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(BindComparison<>), true)]
    class BindComparisonDrawer : StackedPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement().WithStyle(s =>
            {
                s.flexDirection = FlexDirection.Row;
            });
            
            var valueProperty = property.FindPropertyRelative("value");
            var comparisonTypeProperty = property.FindPropertyRelative("comparisonType");
            
            container.WithChildren(new PropertyField(comparisonTypeProperty, property.displayName).WithStyle(s =>
            {
                // s.width = 80;
                s.flexShrink = 1;
                s.flexGrow = 0;
                s.fontSize = 12;
            }).EnsureBind(comparisonTypeProperty).WithClass("bind-comparison__type"),
                new PropertyField(valueProperty, "").WithStyle(s =>
            {
                s.flexGrow = 1;
            }).EnsureBind(valueProperty).WithClass("bind-comparison__value"));
            
            return container.WithClass("bind-comparison");
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("value")) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            var comparisonTypeProperty = property.FindPropertyRelative("comparisonType");
            
            // Compute the rects for the two properties
            EditorGUIUtility.labelWidth = 80;
            var comparisonTypeRect = new Rect(position.x, position.y, 100, position.height);
            var valueRect = new Rect(position.x + 100, position.y, position.width - 100, position.height);
            
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(comparisonTypeRect, comparisonTypeProperty, label, true);
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
            EditorGUI.EndProperty();
            
            EditorGUIUtility.labelWidth = 0;
        }
    }
}