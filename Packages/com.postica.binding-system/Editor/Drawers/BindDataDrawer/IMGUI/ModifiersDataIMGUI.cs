using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal readonly partial struct Modifiers
        {
            public float GetHeight()
            {
                var modifiersProperty = _data.properties.modifiers;
                var mode = (BindMode)_data.properties.mode.enumValueIndex;
                var totalHeight = 0f;
                var size = modifiersProperty.arraySize;
                var settings = BindingSettings.Current;

                for (int i = 0; i < size; i++)
                {
                    if(!settings.ShowIncompatibleModifiers && !array[i].isHotChange && !array[i].BindMode.IsCompatibleWith(mode))
                    {
                        continue;
                    }

                    var propertyHeight = array[i].properties.GetHeight(modifiersProperty.GetArrayElementAtIndex(i));
                    var height = propertyHeight
                               + EditorGUIUtility.singleLineHeight
                               + EditorGUIUtility.standardVerticalSpacing;

                    if (_data.shouldDebug)
                    {
                        height += _data.debugInfo.GetHeight();
                    }

                    totalHeight += height;
                    array[i].height = height;
                }

                return totalHeight;
            }

            public bool Draw(Rect modifierRect, int index, Styles styles, Contents contents, bool drawBindMode)
            {
                var shiftRect = EditorGUI.indentLevel * kIndentUnit;

                var modifierI = _data.properties.modifiers.GetArrayElementAtIndex(index);
                var foldout = new GUITools.FoldoutState(modifierI);
                var modifierData = array[index];
                var modifierContent = modifierI.isExpanded ? modifierData.expandedContent : modifierData.collapsedContent;
                var modifierHeaderRect = new Rect(modifierRect.x + shiftRect,
                                                  modifierRect.y,
                                                  modifierRect.width - shiftRect,
                                                  EditorGUIUtility.singleLineHeight);

                if (Event.current.type == EventType.Repaint)
                {
                    styles.modifierHeader.Draw(modifierHeaderRect,
                                              modifierHeaderRect.Contains(Event.current.mousePosition),
                                              false, false, false);
                }

                using (GUITools.PushState())
                using (new EditorGUI.DisabledScope(Event.current.button == 1 && modifierHeaderRect.Contains(Event.current.mousePosition)))
                {
                    var headerButtonRectShift = styles.modifierHeaderButton.fixedWidth + styles.modifierHeaderButton.margin.horizontal;
                    var headerButtonRect = new Rect(modifierHeaderRect.xMax - headerButtonRectShift,
                                                    modifierHeaderRect.y,
                                                    styles.modifierHeaderButton.fixedWidth,
                                                    modifierHeaderRect.height);
                    modifierHeaderRect.width -= 4 * headerButtonRectShift;

                    EditorGUI.BeginProperty(modifierHeaderRect, modifierContent, modifierI);
                    var indentLevel = EditorGUI.indentLevel;
#if UNITY_2022_3_OR_NEWER
                    EditorGUI.indentLevel = DrawerSystem.IsIMGUIInspector() || ShouldIndentFoldouts?.Invoke() == true ? 1 : 0;
#else
                    EditorGUI.indentLevel = 1;
#endif
                    foldout.value = EditorGUI.Foldout(modifierHeaderRect, foldout.value, modifierContent, true, styles.modifierFoldout);
                    EditorGUI.indentLevel = indentLevel;
                    EditorGUI.EndProperty();

                    var guiEnabled = GUI.enabled;
                    GUI.enabled = true;

                    GUI.backgroundColor = Color.red;
                    if (GUI.Button(headerButtonRect, contents.modifierRemove, styles.modifierHeaderButton))
                    {
                        _data.properties.modifiers.DeleteArrayElementAtIndex(index);
                        _data.modifiers = default;
                        return true;
                    }

                    GUI.backgroundColor = Color.white.WithAlpha(0.5f);
                    using (new EditorGUI.DisabledScope(index == _data.properties.modifiers.arraySize - 1))
                    {
                        // Down
                        headerButtonRect.x -= headerButtonRectShift;
                        if (GUI.Button(headerButtonRect, contents.modifierDown, styles.modifierHeaderButton))
                        {
                            if (_data.properties.modifiers.MoveArrayElement(index, index + 1))
                            {
                                _data.modifiers = default;
                                return true;
                            }
                        }
                    }
                    using (new EditorGUI.DisabledScope(index == 0))
                    {
                        // Up
                        headerButtonRect.x -= headerButtonRectShift;
                        if (GUI.Button(headerButtonRect, contents.modifierUp, styles.modifierHeaderButton))
                        {
                            if (_data.properties.modifiers.MoveArrayElement(index, index - 1))
                            {
                                _data.modifiers = default;
                                return true;
                            }
                        }
                    }

                    if (drawBindMode)
                    {
                        GUI.enabled = modifierData.canChangeMode;
                        {
                            headerButtonRect.x -= headerButtonRectShift;
                            if (GUI.Button(headerButtonRect, contents.bindModesForModifiers[(int)modifierData.BindMode], styles.modifierHeaderButton))
                            {
                                var modifierIndex = index;
                                var nextMode = modifierData.BindMode.NextMode();
                                var modifiersArray = array;
                                _data.preRenderAction = () => modifiersArray[modifierIndex].SetBindMode(nextMode);
                            }
                        }
                    }
                    GUI.enabled = guiEnabled;
                }

                if (foldout.originalValue && array[index].properties.Length > 0)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var bodyRect = new Rect(modifierRect.x,
                                                modifierRect.y + modifierHeaderRect.height + EditorGUIUtility.standardVerticalSpacing,
                                                modifierRect.width,
                                                modifierRect.height - modifierRect.height - 2 * EditorGUIUtility.standardVerticalSpacing);
                        var property = _data.properties.modifiers.GetArrayElementAtIndex(index);

                        if (_data.shouldDebug)
                        {
                            bodyRect.height -= _data.debugInfo.GetHeight();
                        }

                        array[index].properties.Draw(bodyRect, property);
                    }
                }

                foldout.Apply();

                if (_data.shouldDebug)
                {
                    var debugHeight = _data.debugInfo.GetHeight();
                    var indentWidth = EditorGUI.indentLevel * 15f;
                    var debugRect = new Rect(modifierRect.x + indentWidth, modifierRect.yMax - debugHeight, modifierRect.width - indentWidth, debugHeight);
                    var readFunc = _data.properties.BindMode.CanRead() ? modifierData.readFunc : null;
                    var writeFunc = _data.properties.BindMode.CanWrite() ? modifierData.writeFunc : null;
                    _data.debugInfo.StoreDraw(debugRect, readFunc, writeFunc);
                }

                return false;
            }
        }
    }
}