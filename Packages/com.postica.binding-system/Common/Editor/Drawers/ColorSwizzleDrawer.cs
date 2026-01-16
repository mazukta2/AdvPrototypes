using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    [CustomPropertyDrawer(typeof(SwizzleColor), true)]
    class ColorSwizzleDrawer : StackedPropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement().WithStyle(s =>
            {
                s.flexDirection = FlexDirection.Row;
            });
            
            var swizzleRProperty = property.FindPropertyRelative("swizzleR");
            var swizzleGProperty = property.FindPropertyRelative("swizzleG");
            var swizzleBProperty = property.FindPropertyRelative("swizzleB");
            var swizzleAProperty = property.FindPropertyRelative("swizzleA");

            var label = new Label(preferredLabel ?? property.displayName).WithClass(BaseField<int>.labelUssClassName);
            var input = new VisualElement().WithClass(BaseField<int>.inputUssClassName);
            container.Add(label);
            container.Add(input);
            
            var fieldR = new PropertyField(swizzleRProperty, "").WithClass("swizzle-color__field", "swizzle-color__field--r");
            var fieldG = new PropertyField(swizzleGProperty, "").WithClass("swizzle-color__field", "swizzle-color__field--g");
            var fieldB = new PropertyField(swizzleBProperty, "").WithClass("swizzle-color__field", "swizzle-color__field--b");
            var fieldA = new PropertyField(swizzleAProperty, "").WithClass("swizzle-color__field", "swizzle-color__field--a");
            
            input.WithStyle(s => s.flexDirection = FlexDirection.Row)
                .WithChildren(fieldR, fieldG, fieldB, fieldA);
            
            container.AddPosticaStyles();
            
            return container.WithClass("swizzle-color", BaseField<int>.ussClassName).AlignField();
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