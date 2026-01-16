using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    [CustomPropertyDrawer(typeof(SerializedType))]
    public class SerializedTypeDrawer : PropertyDrawer
    {
        private GUIStyle _foldoutStyle;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _foldoutStyle ??= new GUIStyle(EditorStyles.foldout) { richText = true };
            var rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;
            var actualLabel = label.text;
            if (property.propertyPath.EndsWith(']'))
            {
                int commaIndex = actualLabel.IndexOf(',');
                if (commaIndex >= 0)
                {
                    actualLabel = actualLabel.Substring(0, commaIndex);
                }

                int dotIndex = actualLabel.LastIndexOf('.');
                actualLabel = actualLabel.Substring(0, dotIndex + 1) + "<b>" + actualLabel.Substring(dotIndex + 1) + "</b>";
            }
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, actualLabel, _foldoutStyle);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var typeProperty = property.FindPropertyRelative("_type");
                EditorGUI.TextField(rect, typeProperty.stringValue);
                EditorGUI.indentLevel--;
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded 
                ? EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing 
                : EditorGUIUtility.singleLineHeight;
        }
    }
}