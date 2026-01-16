using Postica.Common;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal readonly partial struct ConverterHandler
        {
            public float GetHeight()
            {
                if(_data == null || instance == null) { return 0f; }

                float headerHeight = instance != null 
                                   ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing 
                                   : 0;

                if (_data.shouldDebug)
                {
                    headerHeight += _data.debugInfo.GetHeight();
                }

                if (!_property.isExpanded) 
                { 
                    return headerHeight; 
                }

                float height = _properties.GetHeight(_property);
                return height + headerHeight;
            }

            public void Draw(Rect rect, Action<IConverter> onSelect, Styles styles, Contents contents)
            {
                if(instance == null) { return; }

                var property = _property;
                var iconWidth = styles.bindMenuButton.fixedWidth;
                const float labelShift = 22;

                var foldout = new GUITools.FoldoutState(property);
                var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                EditorGUIUtility.labelWidth -= labelShift;
                if (_properties.Length == 0)
                {
                    headerRect = EditorGUI.PrefixLabel(headerRect, -1, isRead ? contents.readConverter : contents.writeConverter);
                }
                else
                {
                    var indentShift = DrawerSystem.IsIMGUIInspector() || ShouldIndentFoldouts?.Invoke() == true ? kIndentUnit : 0;
                    var foldoutRect = new Rect(rect.x + indentShift, rect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                    foldout.value = EditorGUI.Foldout(foldoutRect, foldout.value, isRead ? contents.readConverter : contents.writeConverter, true);
                    headerRect.x += foldoutRect.width + 2;
                    headerRect.width -= foldoutRect.width - 2;
                }
                EditorGUIUtility.labelWidth = 0;
                // Shift a bit the headerRect
                headerRect.x += labelShift;
                headerRect.width -= labelShift;
                DrawIcon(headerRect, instance.IsSafe, styles, iconWidth);

                if (!_canSelect)
                {
                    // Draw as a normal line
                    GUI.Label(headerRect, _content, EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUI.BeginProperty(headerRect, _content, property);
                    // Draw as a popup
                    using (new EditorGUI.DisabledScope(Event.current.button == 1 && headerRect.Contains(Event.current.mousePosition)))
                    {
                        if (GUI.Button(headerRect, _content, EditorStyles.popup))
                        {
                            var smartDropdown = new SmartDropdown(false, "Change Converter");
                            if (_implicitConverter != null)
                            {
                                smartDropdown.Add(_implicitConverter.Id,
                                                  "Implicit",
                                                  _implicitConverter.IsSafe ? styles.converterIcon : styles.converterUnsafeIcon,
                                                  () => onSelect(null),
                                                  null,
                                                  instance?.Id == _implicitConverter.Id);
                            }
                            foreach (var template in _templates)
                            {
                                smartDropdown.Add(template.Id,
                                                template.IsSafe ? string.Empty : "Unsafe",
                                                template.IsSafe ? styles.converterIcon : styles.converterUnsafeIcon,
                                                () => onSelect(template.Create()),
                                                null,
                                                instance?.Id == template.Id);
                            }

                            smartDropdown.Show(headerRect);
                        }
                    }
                    EditorGUI.EndProperty();
                }

                if (foldout.originalValue && _properties.Length > 0)
                {
                    //EditorGUIUtility.labelWidth = 0;
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var bodyRect = new Rect(rect.x,
                                                rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                                                rect.width,
                                                _properties.height);
                        _properties.Draw(bodyRect, property);
                    }
                }

                if (foldout.Apply())
                {
                    _data.Refresh();
                }

                if (!instance.IsSafe)
                {
                    using (GUITools.PushState())
                    {
                        GUI.contentColor = _unsafeColor;
                        GUI.Label(new Rect(headerRect.xMax - 100, headerRect.y, 80, headerRect.height), "[Not Safe]", styles.converterNotSafe);
                    }
                }

                if (_data.shouldDebug)
                {
                    var debugHeight = _data.debugInfo.GetHeight();
                    var indentWidth = EditorGUI.indentLevel * 15f;
                    var debugRect = new Rect(rect.x + indentWidth, rect.yMax - debugHeight, rect.width - indentWidth, debugHeight);
                    if (isRead)
                    {
                        _data.debugInfo.StoreDraw(debugRect, instance.Convert, null);
                    }
                    else
                    {
                        _data.debugInfo.StoreDraw(debugRect, null, instance.Convert);
                    }
                }
            }

            public void DrawIcon(Rect rect, Styles styles)
{
                var iconWidth = styles.bindMenuButton.fixedWidth;
                DrawIcon(rect, instance.IsSafe, styles, iconWidth);
            }

            private static void DrawIcon(Rect rect, bool isSafe, Styles styles, float iconWidth)
            {
                if (isSafe)
                {
                    var iconRect = new Rect(rect.x - iconWidth + 4, rect.y + 2, 14, 14);
                    GUI.DrawTexture(iconRect, styles.converterIcon);
                }
                else
                {
                    var iconRect = new Rect(rect.x - iconWidth + 2, rect.y + 2, 16, 16);
                    GUI.DrawTexture(iconRect, styles.converterUnsafeIcon);
                }
            }
        }
    }
}