using UnityEditor;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        protected internal class Styles
        {
            public readonly GUIStyle placeholder;
            public readonly GUIStyle bindMode;
            public readonly GUIStyle bindMenuButton;
            public readonly GUIStyle path;
            public readonly GUIStyle pathPreview;
            public readonly GUIStyle pathPreviewEdit;
            public readonly GUIStyle converterNotSafe;
            public readonly GUIStyle error;
            public readonly GUIStyle globalError;
            public readonly GUIStyle pathIcon;
            public readonly GUIStyle targetHint;
            public readonly GUIStyle targetInvalidValue;
            public readonly GUIStyle targetMultiComponentTypes;
            public readonly GUIStyle modifierHeader;
            public readonly GUIStyle modifierHeaderButton;
            public readonly GUIStyle debugTextBox;
            public readonly GUIStyle debugErrorBox;
            public readonly GUIStyle debugLabel;
            public readonly GUIStyle multiConverters;
            public readonly GUIStyle multiModifiers;
            public readonly GUIStyle previewBox;
            public readonly GUIStyle modifierFoldout;

            public readonly Texture2D converterIcon;
            public readonly Texture2D converterUnsafeIcon;

            public static string helpColor => "#6BAEB3";
            public static string errorColor => "#FF8080";
            public static string warnColor => "#FFAA20";
            public static string greyColor => "#888888";

            public Color linesColor => EditorStyles.largeLabel.normal.textColor.WithAlpha(0.5f);
            public Color previewEditableLine => Color.red.Green(0.5f);
            public Color previewReadonlyLine => linesColor.WithAlpha(0.2f);

            public Styles()
            {
                placeholder = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleRight,

                };
                placeholder.normal.textColor = Color.red;

                path = new GUIStyle(EditorStyles.popup)
                {
                    richText = true,
                };

                pathPreview = new GUIStyle("Button")
                {
                    fixedHeight = 18,
                    fixedWidth = 20,
                    padding = new RectOffset(2, 2, 1, 1),
                };

                previewBox = new GUIStyle("Box")
                {
                    fontSize = EditorStyles.centeredGreyMiniLabel.fontSize,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(5, 5, 2, 2),
                    fontStyle = FontStyle.Bold,
                };
                previewBox.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;
                previewBox.hover.textColor = EditorStyles.centeredGreyMiniLabel.hover.textColor;

                pathPreviewEdit = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 9,
                };

                converterNotSafe = new GUIStyle("Label")
                {
                    alignment = TextAnchor.UpperRight,
                };

                bindMode = new GUIStyle("Button")
                {
                    richText = true,
                    padding = new RectOffset(),
                    fontStyle = FontStyle.Normal,
                };

                bindMenuButton = new GUIStyle("Button")
                {
                    padding = new RectOffset(),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fixedWidth = 24,
                    margin = new RectOffset(0, 2, 0, 0),
                };

                error = new GUIStyle()
                {
                    padding = new RectOffset(),
                };

                globalError = new GUIStyle("Box")
                {

                };

                pathIcon = new GUIStyle()
                {
                    padding = new RectOffset(),
                };

                targetHint = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(0, 24, 0, 0),
                };

                targetInvalidValue = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(0, 8, 0, 0),
                };

                targetMultiComponentTypes = new GUIStyle(targetHint)
                {
                    //fixedWidth = 16,
                    //fixedHeight = 16,
                };

                modifierHeader = new GUIStyle("Box")
                {

                };
                
                modifierFoldout = new GUIStyle(EditorStyles.foldout)
                {
                    richText = true,
                };

                debugTextBox = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(4, 4, 2, 2),
                };

                debugErrorBox = new GUIStyle(debugTextBox);
                debugErrorBox.normal.textColor = Color.red;

                debugLabel = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
                debugLabel.normal.textColor = EditorGUIUtility.isProSkin 
                                            ? new Color(238f / 255, 174f / 255, 0, 1)
                                            : new Color(0.2f, 0.2f, 0.2f, 1);

                modifierHeaderButton = new GUIStyle(EditorStyles.miniButton)
                {
                    fixedWidth = 20,
                    margin = new RectOffset(1, 1, 0, 0),
                    padding = new RectOffset(1, 1, 1, 1),
                    fontSize = 10,
                    fixedHeight = EditorGUIUtility.singleLineHeight,
                    contentOffset = Vector2.zero,
                    alignment = TextAnchor.MiddleCenter,
                };
                modifierHeaderButton.normal.background = null;
                modifierHeaderButton.normal.scaledBackgrounds = null;
                modifierHeaderButton.hover.textColor = Color.yellow.Green(0.5f).WithAlpha(0.75f);

                multiConverters = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleLeft
                };

                multiModifiers = new GUIStyle(multiConverters)
                {

                };

                if (EditorGUIUtility.isProSkin)
                {
                    converterIcon = Icons.ConvertIcon.MultiplyBy(Color.white.WithAlpha(0.5f));
                    converterUnsafeIcon = Icons.ConvertUnsafeIcon.MultiplyBy(_unsafeColor.WithAlpha(0.5f));
                }
                else
                {
                    converterIcon = Icons.ConvertIcon.MultiplyBy(Color.grey.WithAlpha(0.5f));
                    converterUnsafeIcon = Icons.ConvertUnsafeIcon.MultiplyBy(Color.grey.WithAlpha(0.5f));
                }
            }
        }
    }
}