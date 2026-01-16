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
using UnityEngine.Profiling;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        const float kIndentUnit = 15;

        internal delegate bool ShouldIndentModifiersDelegate();
        internal delegate Object DrawObjectFieldDelegate(BindDataDrawer drawer, ref Rect rect, Object currentObject, out bool invalidValue);
        internal delegate bool TryPreparePreviewDataDelegate(PropertyData data, Object source, string path);

        internal static ShouldIndentModifiersDelegate ShouldIndentFoldouts;

        internal static Func<string, Rect, Rect> UpdatePositionRect;

        internal DrawObjectFieldDelegate OnDrawObjectField;
        internal TryPreparePreviewDataDelegate OnTryPrepareDataPreview;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _property = property;
            if (!_initialized)
            {
                _initialized = true;
                Initialize(property);
            }
            RegisterLabel(property, label);

            var convertersHeight = 0f;

            var data = GetData(property);

            data.shouldDebug = CanShowDebugValues(data);

            if (data.properties.mode.enumValueIndex != (int)BindMode.Write)
            {
                UpdateWriteConverter(data);
                if (BindingSettings.Current.ShowImplicitConverters || !data.readConverter.isImplicit)
                {
                    convertersHeight += data.readConverter.GetHeight();
                }
            }

            if (data.properties.mode.enumValueIndex != (int)BindMode.Read)
            {
                UpdateReadConverter(data);
                if (BindingSettings.Current.ShowImplicitConverters || !data.writeConverter.isImplicit)
                {
                    convertersHeight += data.writeConverter.GetHeight();
                }
            }

            if (data.isMultipleTargets)
            {
                // Different height for multiple targets
                var lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                var multiTargetHeight = 2 * lineHeight;
                if (ShouldRenderMultiConverters(data))
                {
                    multiTargetHeight += lineHeight;
                }
                if (data.commonModifiers.hasElements)
                {
                    multiTargetHeight += lineHeight;
                }

                return multiTargetHeight;
            }

            if (data.preModifiers.HaveChanged())
            {
                var (fromType, _) = GetTypeMapping(data, _isUIToolkit);
                data.preModifiers = new Modifiers(data, fromType, _isUIToolkit, true);
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (data.modifiers.HaveChanged())
            {
                data.modifiers = new Modifiers(data, data.bindType, _isUIToolkit, false);
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (data.parameters.HaveChanged()
                && data.properties.parameters != null
                && data.properties.target.objectReferenceValue
                && !string.IsNullOrEmpty(data.properties.path.stringValue))
            {
                try
                {
                    data.parameters = new Parameters(data);
                }
                catch (Exception ex)
                {
                    if (!data.hasError)
                    {
                        Debug.LogException(ex);
                    }
                    data.parameters = default;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }

            var defaultHeight = 2 * (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight);
            if (data.shouldDebug)
            {
                defaultHeight += data.debugInfo.GetHeight();
            }

            var previewHeight = data.canPathPreview && data.isPathPreview && data.getPreviewHeight != null
                              ? data.getPreviewHeight() + 20
                              : 0;

            data.changeEventHeight = data.canShowEvents ? EditorGUI.GetPropertyHeight(data.properties.valueChangedEvent) : 0;

            return defaultHeight
                    + data.modifiers.GetHeight()
                    + convertersHeight
                    + data.parameters.GetHeight()
                    + previewHeight
                    + data.changeEventHeight;
        }

        
        private static bool CanShowDebugValues(PropertyData data)
        {
            return Application.isPlaying
                                        && BindData.BitFlags.LiveDebug.IsFlagOf(data.properties.flags.intValue)
                                        && data.properties.target.objectReferenceValue
                                        && !string.IsNullOrEmpty(data.properties.path.stringValue);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Event.current.type == EventType.Layout && OnTryPrepareDataPreview == null)
            {
                return;
            }

            Profiler.BeginSample("BindDataDrawer.OnGUI", property.serializedObject.targetObject);

            _property = property;

            if (!_initialized)
            {
                _initialized = true;
                Initialize(property);
            }
            RegisterLabel(property, label);

            var settings = BindingSettings.Current;
            var data = GetData(property);

            if (data.preRenderAction != null)
            {
                var preRenderAction = data.preRenderAction;
                data.preRenderAction = null;
                preRenderAction();
            }

            data.commonSource.UpdateIfNeeded();

#if !UNITY_2022_3_OR_NEWER
            if (UpdatePositionRect != null)
            {
                position = UpdatePositionRect(property.propertyPath, position);
            }
#endif

            if (Event.current.type == EventType.Layout)
            {
                if(OnTryPrepareDataPreview != null 
                    && data.canPathPreview 
                    && data.isPathPreview
                    && data.previewDraw != null)
                {
                    var prevIndentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = data.previewIndentLevel;
                    DrawPathValuePreview(data, data.lastPreviewRect);
                    EditorGUI.indentLevel = prevIndentLevel;
                }

                Profiler.EndSample();
                return;
            }

            if (Application.isPlaying 
                && Event.current.type == EventType.Repaint
                && (!data.properties.target.objectReferenceValue 
                    || (string.IsNullOrEmpty(data.properties.path.stringValue) && !data.isSelfReference)))
            {
                var borderColor = Color.red.WithAlpha(0.75f);
                var borderWidth = 2;

                EditorGUI.DrawRect(position, borderColor.WithAlpha(0.05f));

                EditorGUI.DrawRect(new Rect(position.x - borderWidth - 1, position.y, borderWidth, position.height), borderColor);
                EditorGUI.DrawRect(new Rect(position.xMax + 1, position.y, borderWidth, position.height), borderColor);
                
                EditorGUI.DrawRect(new Rect(position.x - borderWidth, position.y - borderWidth, position.width + borderWidth * 2, borderWidth), borderColor);
                EditorGUI.DrawRect(new Rect(position.x - borderWidth, position.yMax, position.width + borderWidth * 2, borderWidth), borderColor);

                //EditorGUI.DrawRect(position, _editorColor);
            }

            var styleShift = styles.bindMenuButton.fixedWidth + styles.bindMenuButton.margin.horizontal;
            var targetRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            var lastRect = targetRect;
            EditorGUI.BeginProperty(targetRect, data.label, data.properties.target);
            {
                var lastIndent = EditorGUI.indentLevel;
                if (_targetLabelShift)
                {
                    EditorGUI.indentLevel++;
                }
                targetRect = EditorGUI.PrefixLabel(targetRect, data.label);
                // In case there is no label
                if (targetRect.x - styleShift < position.x)
                {
                    targetRect.x += styleShift;
                    targetRect.width -= styleShift;
                }
                // Apparently there is a bug with ObjectField and indent
                EditorGUI.indentLevel = 0;
                DrawTargetField(targetRect, data);
                EditorGUI.indentLevel = lastIndent;
            }
            EditorGUI.EndProperty();

            var indentShift = EditorGUI.indentLevel * kIndentUnit;
            var bindMenuButtonRect = new Rect(targetRect.x - styleShift,
                                              targetRect.y,
                                              styles.bindMenuButton.fixedWidth,
                                              targetRect.height);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(data.properties.path.stringValue) && !data.isSelfReference))
            {
                if (GUI.Button(bindMenuButtonRect, contents.bindMenu, styles.bindMenuButton))
                {
                    SmartDropdown menu = BuildOptionsMenu(data);

                    menu.Show(targetRect);
                }
            }

            EditorGUI.indentLevel++;
            using (var guiState = GUITools.PushState())
            {
                GUI.enabled = (!data.isMultipleTargets && data.properties.target.objectReferenceValue)
                            || (data.isMultipleTargets && data.commonSource.commonType != null);
                //EditorGUIUtility.labelWidth = pathLabelWidth;

                var pathRect = new Rect(position.x,
                                    position.y + targetRect.height + EditorGUIUtility.standardVerticalSpacing,
                                    position.width,
                                    EditorGUIUtility.singleLineHeight);

                // Draw the rect for unordered list
                var rectColor = styles.linesColor;
                if (_targetLabelShift)
                {
                    DrawExpandLines(ref pathRect, ref targetRect, ref rectColor, false);
                }
                else
                {
                    var expandLinesRect = targetRect;
                    expandLinesRect.height *= 0.5f;
                    expandLinesRect.y += expandLinesRect.height;
                    DrawExpandLines(ref pathRect, ref expandLinesRect, ref rectColor, true);
                }

                Rect pathPopupRect = default;
                if (data.properties.path.stringValue != data.prevPath)
                {
                    // Most probably it is a prefab revert
                    data.prevPath = data.properties.path.stringValue;
                    data.commonPath.Update();
                }

                var pathRectShift = 0f;
                if (!data.hasError && data.canPathPreview)
                {
                    var pathPreviewRect = new Rect(pathRect.xMax - styles.pathPreview.fixedWidth, pathRect.y, styles.pathPreview.fixedWidth, pathRect.height);
                    data.isPathPreview = GUI.Toggle(pathPreviewRect, data.isPathPreview, contents.pathPreview, styles.pathPreview);
                    pathRectShift = pathPreviewRect.width;
                }

                EditorGUI.BeginProperty(pathRect, contents.path, data.properties.path);
                {
                    lastRect = pathRect;

                    float modeButtonWidth = bindMenuButtonRect.width;
                    var foldout = new GUITools.FoldoutState(data.properties.path);

                    if (data.properties.parameters?.arraySize > 0 && data.commonPath.isMixedValue != true)
                    {
                        indentShift = DrawerSystem.IsIMGUIInspector() || ShouldIndentFoldouts?.Invoke() == true ? kIndentUnit : 0;
                        var pathFoldoutRect = new Rect(pathRect.x + indentShift,
                                                       pathRect.y,
                                                       EditorGUIUtility.labelWidth - modeButtonWidth - 2,
                                                       pathRect.height);
                        foldout.value = EditorGUI.Foldout(pathFoldoutRect, foldout.value, contents.path, true);
                        pathRect = new Rect(pathFoldoutRect.xMax - indentShift + 3 + modeButtonWidth,
                                            pathFoldoutRect.y,
                                            pathRect.width - pathFoldoutRect.width - modeButtonWidth - 2,
                                            pathRect.height);
                    }
                    else
                    {
                        foldout.value = false;
                        pathRect = EditorGUI.PrefixLabel(pathRect, contents.path);
                    }

                    var pathIconsRect = new Rect(pathRect.x - 19 - modeButtonWidth, pathRect.y + 1, 16, 16);
                    if (!data.isMultipleTargets && data.hasError)
                    {
                        DrawShowError(pathIconsRect, data.errorMessage);
                        EditorGUI.DrawRect(new Rect(pathRect.x - 1, pathRect.y - 1, pathRect.width + 2, pathRect.height + 2), Color.red.WithAlpha(0.6f));
                        pathIconsRect.x -= pathIconsRect.width + 2;
                    }
                    if (!data.isMultipleTargets && BindData.BitFlags.LiveDebug.IsFlagOf(data.properties.flags.intValue))
                    {
                        GUI.Label(pathIconsRect, contents.debug, styles.pathIcon);
                        pathIconsRect.x -= pathIconsRect.width + 2;
                    }
                    if (data.typeIcon?.image)
                    {
                        GUI.Label(pathIconsRect, data.typeIcon, styles.pathIcon);
                        pathIconsRect.x -= pathIconsRect.width + 2;
                    }

                    pathPopupRect = pathRect.FromRight(pathRectShift);
                    var pathContent = FitString(styles.path,
                                             pathRect.width,
                                             data,
                                             data.properties.path.stringValue,
                                             _windowWidth != EditorGUIUtility.currentViewWidth);
                    if (GUI.Button(pathPopupRect,
                                   data.commonPath.isMixedValue == true ? contents.mixedValue : pathContent,
                                   styles.path))
                    {
                        var dropdown = HandleShowPopup(data,
                                                       data.commonPath.isMixedValue == true ? "" : data.properties.path.stringValue,
                                                       property);

                        dropdown.Show(pathRect);
                    }
                    if (data.commonPath.isMixedValue == true)
                    {
                        GUI.Label(pathPopupRect, contents.multiplePaths, styles.targetHint);
                    }

                    using (new EditorGUI.DisabledScope(!_canChangeMode))
                    {
                        if (data.commonBindMode.isMixedValue == true)
                        {
                            using (GUITools.PushState())
                            {
                                GUI.backgroundColor = Color.red;
                                if (GUI.Button(new Rect(pathRect.x - modeButtonWidth - 1, pathRect.y, modeButtonWidth, 18),
                                       contents.multipleBindModes,
                                       styles.bindMode))
                                {
                                    data.properties.mode.enumValueIndex = (int)BindMode.ReadWrite.NextMode();
                                    data.commonBindMode.Update();
                                }
                            }
                        }
                        else if (GUI.Button(new Rect(pathRect.x - modeButtonWidth - 1, pathRect.y, modeButtonWidth, 18),
                                       contents.bindModes[data.properties.mode.enumValueIndex],
                                       styles.bindMode))
                        {
                            data.properties.mode.enumValueIndex = (int)data.properties.BindMode.NextMode();
                            if (ValidatePath(data.properties.target.objectReferenceValue, data, data.properties.path.stringValue))
                            {
                                data.hasError = false;
                            }
                        }
                    }

                    lastRect = DrawPathValuePreview(data, lastRect);

                    // Parameters time
                    if (data.commonPath.isMixedValue != true)
                    {
                        lastRect = DrawParameters(data, lastRect, foldout);
                    }

                    foldout.Apply();
                }
                EditorGUI.EndProperty();


                if (data.shouldDebug)
                {
                    var indentWidth = (EditorGUI.indentLevel / 2 + 1) * kIndentUnit;
                    var debugRect = new Rect(lastRect.x + indentWidth, lastRect.yMax, lastRect.width - indentWidth, data.debugInfo.GetHeight());
                    data.debugInfo.StoreDraw(debugRect,
                                             data.properties.BindMode.CanRead() ? v => v : (Func<object, object>)null,
                                             data.properties.BindMode.CanWrite() ? v => v : (Func<object, object>)null);

                    lastRect.height += debugRect.height;
                }

                if (data.isMultipleTargets)
                {
                    if (ShouldRenderMultiConverters(data))
                    {
                        var convRect = new Rect(position.x,
                                        lastRect.y + lastRect.height,
                                        position.width,
                                        EditorGUIUtility.singleLineHeight);

                        DrawExpandLines(ref convRect, ref lastRect, ref rectColor, true);
                        GUI.Label(convRect.OffsetX(kIndentUnit), contents.multipleConverters, styles.multiConverters);
                        lastRect = convRect;
                    }
                }
                else
                {
                    DrawConverters(ref position, settings, data, ref lastRect, ref rectColor, pathPopupRect);
                }

                if (data.commonModifiers.isMixedValue.HasValue && data.commonModifiers.hasElements)
                {
                    var modifierRect = new Rect(position.x,
                                    lastRect.y + lastRect.height,
                                    position.width,
                                    EditorGUIUtility.singleLineHeight);

                    DrawExpandLines(ref modifierRect, ref lastRect, ref rectColor, true);
                    GUI.Label(modifierRect.OffsetX(kIndentUnit), contents.multipleModifiers, styles.multiModifiers);
                    lastRect = modifierRect;
                }
                else
                {
                    lastRect = DrawModifiers(position, property, settings, data, lastRect, guiState, rectColor);
                }

                if (!data.isMultipleTargets && data.shouldDebug)
                {
                    var bindData = (IBindDataDebug)property.GetValue();
                    data.debugInfo.CommitDraw(ref data, property.GetValue(), bindData.DebugValue, styles, contents);
                }

                // Draw events
                if (data.canShowEvents)
                {
                    var eventRect = new Rect(position.x,
                                    lastRect.y + lastRect.height,
                                    position.width,
                                    data.changeEventHeight);

                    DrawExpandLines(ref eventRect, ref lastRect, ref rectColor, true);
                    EditorGUI.PropertyField(eventRect.FromLeft(kIndentUnit), data.properties.valueChangedEvent);
                    lastRect = eventRect;
                }
            }
            EditorGUI.indentLevel--;

            data.firstRun = false;
            _windowWidth = EditorGUIUtility.currentViewWidth;

            Profiler.EndSample();
        }

        private Rect DrawPathValuePreview(PropertyData data, Rect lastRect)
        {
            if (data.hasError || data.previewDraw == null || !data.isPathPreview)
            {
                return lastRect;
            }

            if(Event.current.type == EventType.Repaint)
            {
                data.lastPreviewRect = lastRect;
                data.previewIndentLevel = EditorGUI.indentLevel;
            }

            var height = (data.getPreviewHeight?.Invoke() ?? 0) + 20;
            var previewRect = lastRect.WithHeight(height).FromLeft(EditorGUI.indentLevel * kIndentUnit + 1);
            previewRect.y += lastRect.height;
            var editRect = new Rect(previewRect.xMax - 32, previewRect.y, 32, 12);
            GUI.Box(previewRect, "Preview", styles.previewBox);
            data.pathPreviewIsEditing = GUI.Toggle(editRect, data.pathPreviewIsEditing, "Edit", styles.pathPreviewEdit);
            using (new EditorGUI.DisabledGroupScope(!data.pathPreviewIsEditing))
            {
                var indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var previewDrawRect = new Rect(previewRect.x + 4, previewRect.y + 18, previewRect.width - 6, previewRect.height - 20);
                data.previewDraw?.Invoke(previewDrawRect, data.pathPreviewIsEditing);
                EditorGUI.indentLevel = indentLevel;
            }

            if (data.pathPreviewIsEditing)
            {
                EditorGUI.DrawRect(previewRect.WithWidth(2), styles.previewEditableLine);
            }
            else
            {
                EditorGUI.DrawRect(previewRect.WithWidth(2), styles.previewReadonlyLine);
            }

            lastRect.height += previewRect.height;

            return lastRect;
        }

        private Rect DrawModifiers(Rect position, SerializedProperty property, BindingSettings settings, PropertyData data, Rect lastRect, GUITools.State guiState, Color rectColor)
        {
            if (data.properties.modifiers.arraySize > 0 && !data.modifiers.HaveChanged())
            {
                guiState.RestoreLabelWidth();

                var modifiersProp = data.properties.modifiers;
                var modifierRect = new Rect(position.x, lastRect.y, position.width, lastRect.height);
                var prevModifierRect = modifierRect;
                var arraySize = modifiersProp.arraySize;
                var guiWasEnabled = GUI.enabled;

                for (int i = 0; i < arraySize; i++)
                {
                    GUI.enabled = guiWasEnabled;

                    if (!data.modifiers.CanDraw(i))
                    {
                        if (settings.ShowIncompatibleModifiers || data.modifiers.IsHotChange(i))
                        {
                            GUI.enabled = false;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var modifierData = data.modifiers.array[i];
                    if (modifierData.modifier == null)
                    {
                        modifiersProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    modifierRect.y += modifierRect.height;
                    modifierRect.height = modifierData.height;
                    DrawExpandLines(ref modifierRect, ref prevModifierRect, ref rectColor, true);

                    if (data.modifiers.Draw(modifierRect, i, styles, contents, _canChangeMode))
                    {
                        break;
                    }
                    prevModifierRect = modifierRect;
                }

                GUI.enabled = guiWasEnabled;
                lastRect = prevModifierRect;
            }
            
            return lastRect;
        }

        private void DrawConverters(ref Rect position, BindingSettings settings, PropertyData data, ref Rect lastRect, ref Color rectColor, Rect pathPopupRect)
        {
            var converterIconDrawn = false;
            if (data.properties.mode.enumValueIndex != (int)BindMode.Write && data.readConverter.ShouldDraw())
            {
                if (!settings.ShowImplicitConverters && data.readConverter.isImplicit)
                {
                    var iconRect = new Rect(pathPopupRect.xMax - 10, pathPopupRect.y, 20, pathPopupRect.height);
                    data.readConverter.DrawIcon(iconRect, styles);
                    converterIconDrawn = true;
                }
                else
                {
                    //EditorGUIUtility.labelWidth = converterLabelWidth;

                    var convRect = new Rect(position.x,
                                    lastRect.y + lastRect.height,
                                    position.width,
                                    data.readConverter.GetHeight());

                    DrawExpandLines(ref convRect, ref lastRect, ref rectColor, true);

                    data.readConverter.Draw(convRect,
                                        c => data.preRenderAction = () => data.properties.readConverter.managedReferenceValue = c,
                                        styles,
                                        contents);

                    lastRect = convRect;
                }
            }

            if (data.properties.mode.enumValueIndex != (int)BindMode.Read && data.writeConverter.ShouldDraw())
            {
                if (!settings.ShowImplicitConverters && data.writeConverter.isImplicit)
                {
                    if (!converterIconDrawn)
                    {
                        var iconRect = new Rect(pathPopupRect.xMax - 10, pathPopupRect.y, 20, pathPopupRect.height);
                        data.writeConverter.DrawIcon(iconRect, styles);
                    }
                }
                else
                {
                    //EditorGUIUtility.labelWidth = converterLabelWidth;

                    var convRect = new Rect(position.x,
                                    lastRect.y + lastRect.height,
                                    position.width,
                                    data.writeConverter.GetHeight());

                    DrawExpandLines(ref convRect, ref lastRect, ref rectColor, true);

                    data.writeConverter.Draw(convRect,
                                        c => data.preRenderAction = () => data.properties.writeConverter.managedReferenceValue = c,
                                        styles,
                                        contents);

                    lastRect = convRect;
                }
            }
        }

        private static Rect DrawParameters(PropertyData data, Rect lastRect, GUITools.FoldoutState foldout)
        {
            // First check if it is valid
            if (!data.parameters.IsValid())
            {
                data.parameters.Reset(true);
                data.parameters = default;
            }

            // Now draw them
            if (foldout.originalValue && data.properties.parameters != null && !data.parameters.HaveChanged())
            {
                // Draw parameter
                var parameterRect = lastRect;
                parameterRect.y += lastRect.height + EditorGUIUtility.standardVerticalSpacing;
                using (new EditorGUI.IndentLevelScope())
                {
                    var parametersProperty = data.properties.parameters;
                    for (int i = 0; i < parametersProperty.arraySize; i++)
                    {
                        ref var paramData = ref data.parameters.array[i];
                        var property_i = parametersProperty.GetArrayElementAtIndex(i);
                        var height = paramData.height;
                        if (height == 0)
                        {
                            height = EditorGUI.GetPropertyHeight(property_i, paramData.content, true);
                        }
                        parameterRect.height = height;

                        EditorGUI.PropertyField(parameterRect, property_i, paramData.content, true);

                        var skipHeight = parameterRect.height + EditorGUIUtility.standardVerticalSpacing;
                        parameterRect.y += skipHeight;
                        lastRect.height += skipHeight;
                    }
                }

                //pathRect.y = parameterRect.y;
            }

            return lastRect;
        }

        private void DrawShowError(in Rect rect, string message)
        {
            contents.error.tooltip = message;
            GUI.Label(rect, contents.error, styles.error);
        }

        private static void DrawExpandLines(ref Rect rect, ref Rect prevRect, ref Color color, bool attachToPrevious)
        {
            var xOffset = rect.x + EditorGUI.indentLevel * kIndentUnit - 10;
            if (attachToPrevious)
            {
                var prevY = prevRect.y + EditorGUIUtility.singleLineHeight * 0.5f + 1;
                EditorGUI.DrawRect(new Rect(xOffset, prevY, 1, prevRect.height), color);
                EditorGUI.DrawRect(new Rect(xOffset, rect.y + EditorGUIUtility.singleLineHeight * 0.5f, 8, 1), color);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(xOffset, rect.y - EditorGUIUtility.singleLineHeight * 0.5f, 1, EditorGUIUtility.singleLineHeight), color);
                EditorGUI.DrawRect(new Rect(xOffset, rect.y + EditorGUIUtility.singleLineHeight * 0.5f, 8, 1), color);
            }
        }

        private void DrawTargetField(Rect rect, PropertyData data)
        {
            var lastTarget = data.properties.target.objectReferenceValue;
            var prevTargetState = data.prevTarget;

            EditorGUI.BeginChangeCheck();

            var showMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = data.commonSource.isMixedValue == true && data.commonSource.commonValue == null;
            var currentObject = data.isMultipleTargets && data.commonSource.commonValue ? data.commonSource.commonValue : lastTarget;
            Object newTarget = DrawObjectField(ref rect, currentObject, out var invalidValue);
            EditorGUI.showMixedValue = showMixedValue;

            if (!invalidValue)
            {
                if (data.commonSource.isMultipleTypes && data.commonSource.commonValue)
                {
                    GUI.Label(rect, contents.multipleTargetComponentTypes, styles.targetMultiComponentTypes);
                }
                else if (data.commonSource.isMultipleTypes && data.commonSource.commonType == null)
                {
                    GUI.Label(rect, contents.multipleTargetIncompatibleTypes, styles.targetMultiComponentTypes);
                }
                else if (data.commonSource.isMultipleTypes)
                {
                    GUI.Label(rect, contents.multipleTargetTypes, styles.targetHint);
                }
                else if (data.commonSource.isMixedValue == true)
                {
                    GUI.Label(rect, contents.multipleTargetObjects, styles.targetHint);
                }
                else if (newTarget == null)
                {
                    GUI.Label(rect, contents.target, styles.targetHint);
                }
            }

            if (data.firstRun && !data.isMultipleTargets)
            {
                SetTargetValue(newTarget, data, out _);
                data.prevTarget = (true, newTarget);
                return;
            }

            if (!EditorGUI.EndChangeCheck()) // No change occured
            {
                // HACK: The Prefab Revert event is not raised, so this is the workaround
                if (data.prevTarget.isValid && data.prevTarget.value != newTarget)
                {
                    if (data.isMultipleTargets)
                    {
                        // Too risky to have this logic, it may produce unwanted overwriting

                        data.commonSource.ForEach((t, p, v) =>
                        {
                            data.commonType[t] = v ? v.GetType().AssemblyQualifiedName : string.Empty;
                        });

                        data.commonSource.Update();
                        data.commonType.Update();
                        data.commonPath.Update();
                        data.shouldRefitPath = true;
                    }
                    else
                    {
                        data.sourcePersistedType = newTarget?.GetType();
                        data.shouldRefitPath = true;
                    }

                    if (ValidatePath(data.properties.target.objectReferenceValue, data, data.properties.path.stringValue))
                    {
                        data.prevValue = null;
                    }
                }

                data.prevTarget = (true, data.properties.target.objectReferenceValue);
                return;
            }


            if (lastTarget != newTarget)
            {
                if (data.commonSource.isMultipleTypes)
                {
                    SetMultiTypeTarget(newTarget, data, out _);
                }
                else
                {
                    SetTargetValue(newTarget, data, out _);
                    data.commonSource.PostponeUpdate();
                }

                UpdatePathPreview(data);
            }

            data.prevTarget = (true, data.properties.target.objectReferenceValue);
        }

        private void SetMultiTypeTarget(Object newTarget, PropertyData data, out bool hasValidValues, bool logErrorsToConsole = true)
        {
            var targets = data.serializedObject.targetObjects;
            var dataPath = data.properties.property.propertyPath;
            var invalidTargets = 0;

            foreach (var t in targets)
            {
                using (var st = new SerializedObject(t))
                {
                    var property_i = st.FindProperty(dataPath);
                    var data_i = new PropertyData(property_i)
                    {
                        firstRun = data.firstRun
                    };
                    SetTargetValue(newTarget, data_i, out var isValid, logErrorsToConsole);
                    if (!isValid)
                    {
                        invalidTargets++;
                    }
                    st.ApplyModifiedProperties();
                }
            }

            data.serializedObject.Update();

            if (invalidTargets > 0)
            {
                Debug.LogWarning($"Setting bind source {newTarget} caused {invalidTargets} invalid paths");
            }

            hasValidValues = invalidTargets < targets.Length;

            data.commonSource.Update();
        }

        private Object DrawObjectField(ref Rect rect, Object currentObject, out bool invalidValue)
        {
            if(OnDrawObjectField != null)
            {
                return OnDrawObjectField(this, ref rect, currentObject, out invalidValue);
            }

            invalidValue = false;
            return EditorGUI.ObjectField(rect, GUIContent.none, currentObject, typeof(object), true);
        }
    }
}