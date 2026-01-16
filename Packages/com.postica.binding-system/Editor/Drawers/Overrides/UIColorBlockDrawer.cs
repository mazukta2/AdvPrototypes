using Postica.Common;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(ColorBlock), true)]
    [DrawerSystem.ReplacesPropertyDrawer(typeof(ColorBlockDrawer))]
    class UIColorBlockDrawer : StackedPropertyDrawer
    {
        const string kNormalColor = "m_NormalColor";
        const string kHighlightedColor = "m_HighlightedColor";
        const string kPressedColor = "m_PressedColor";
        const string kSelectedColor = "m_SelectedColor";
        const string kDisabledColor = "m_DisabledColor";
        const string kColorMultiplier = "m_ColorMultiplier";
        const string kFadeDuration = "m_FadeDuration";
        
        private readonly float[] _heights = new float[7];

        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            Rect drawRect = rect;

            SerializedProperty normalColor = prop.FindPropertyRelative(kNormalColor);
            SerializedProperty highlighted = prop.FindPropertyRelative(kHighlightedColor);
            SerializedProperty pressedColor = prop.FindPropertyRelative(kPressedColor);
            SerializedProperty selectedColor = prop.FindPropertyRelative(kSelectedColor);
            SerializedProperty disabledColor = prop.FindPropertyRelative(kDisabledColor);
            SerializedProperty colorMultiplier = prop.FindPropertyRelative(kColorMultiplier);
            SerializedProperty fadeDuration = prop.FindPropertyRelative(kFadeDuration);

            drawRect.height = _heights[0];
            EditorGUI.PropertyField(drawRect, normalColor);
            drawRect.y += _heights[0] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[1];
            EditorGUI.PropertyField(drawRect, highlighted);
            drawRect.y += _heights[1] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[2];
            EditorGUI.PropertyField(drawRect, pressedColor);
            drawRect.y += _heights[2] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[3];
            EditorGUI.PropertyField(drawRect, selectedColor);
            drawRect.y += _heights[3] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[4];
            EditorGUI.PropertyField(drawRect, disabledColor);
            drawRect.y += _heights[4] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[5];
            EditorGUI.PropertyField(drawRect, colorMultiplier);
            drawRect.y += _heights[5] + EditorGUIUtility.standardVerticalSpacing;
            drawRect.height = _heights[6];
            EditorGUI.PropertyField(drawRect, fadeDuration);
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            SerializedProperty normalColor = prop.FindPropertyRelative(kNormalColor);
            SerializedProperty highlighted = prop.FindPropertyRelative(kHighlightedColor);
            SerializedProperty pressedColor = prop.FindPropertyRelative(kPressedColor);
            SerializedProperty selectedColor = prop.FindPropertyRelative(kSelectedColor);
            SerializedProperty disabledColor = prop.FindPropertyRelative(kDisabledColor);
            SerializedProperty colorMultiplier = prop.FindPropertyRelative(kColorMultiplier);
            SerializedProperty fadeDuration = prop.FindPropertyRelative(kFadeDuration);
            
            float totalHeight = 0;
            totalHeight += _heights[0] = EditorGUI.GetPropertyHeight(normalColor);
            totalHeight += _heights[1] = EditorGUI.GetPropertyHeight(highlighted);
            totalHeight += _heights[2] = EditorGUI.GetPropertyHeight(pressedColor);
            totalHeight += _heights[3] = EditorGUI.GetPropertyHeight(selectedColor);
            totalHeight += _heights[4] = EditorGUI.GetPropertyHeight(disabledColor);
            totalHeight += _heights[5] = EditorGUI.GetPropertyHeight(colorMultiplier);
            totalHeight += _heights[6] = EditorGUI.GetPropertyHeight(fadeDuration);
            return totalHeight;
        }
    }
}