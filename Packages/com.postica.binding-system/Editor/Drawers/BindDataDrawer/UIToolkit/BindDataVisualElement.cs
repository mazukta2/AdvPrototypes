using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using PopupWindow = Postica.Common.PopupWindow;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        private event Action<string> PropertyChanged;

        private void NotifyPropertyChanged(string path) => PropertyChanged?.Invoke(path);

        private void ApplyChanges(PropertyData data)
        {
            if (data.serializedObject.ApplyModifiedProperties())
            {
                NotifyPropertyChanged(data.properties.property.propertyPath);
                //data.Invalidate();
            }
        }

        private bool ApplyChangesSilently(PropertyData data)
        {
            return data.serializedObject.ApplyModifiedProperties();
        }
        
        public static BindDataUI ShowBindView(Rect position, BindProxy proxy)
        {
            if (!proxy.BindData.HasValue)
            {
                return null;
            }
            
            if (!proxy.ActualContext || string.IsNullOrEmpty(proxy.ContextPath))
            {
                return null;
            }
            
            var serializedObject = new SerializedObject(proxy.ActualContext);
            var property = serializedObject.FindProperty(proxy.ContextPath);
            var drawer = new BindDataDrawer();
            drawer.SetFieldValue("m_FieldInfo", property.GetFieldInfo());
            drawer.SetFieldValue("m_PreferredLabel", proxy.Path.Split('.', '/').Last().NiceName());
                
            var view = drawer.CreatePropertyGUI(property).WithClass(BindDataUI.BindDataExpandedClass);
            var width = Mathf.Max(450, position.width);
            var height = 100f;
            var id = $"{proxy.Source}.{proxy.Path}";
            PopupWindow.Show(position, new Vector2(width, height), view, isDynamicTransform: true, windowId: id).OnClose(w => serializedObject.Dispose());

            return view as BindDataUI;
        }
        
        public static void ShowBindView(Rect position, SerializedProperty proxyProperty)
        {
            if (!proxyProperty.IsAlive())
            {
                return;
            }
            
            var drawer = new BindDataDrawer();
            drawer.SetFieldValue("m_FieldInfo", proxyProperty.GetFieldInfo());
            drawer.SetFieldValue("m_PreferredLabel", proxyProperty.propertyPath.Split('.', '/').Last().NiceName());
                
            var view = drawer.CreatePropertyGUI(proxyProperty).WithClass(BindDataUI.BindDataExpandedClass);
            var width = Mathf.Max(450, position.width);
            var height = 100f;
            var id = $"{proxyProperty.serializedObject.targetObject}.{proxyProperty.propertyPath}";
            PopupWindow.Show(position, new Vector2(width, height), view, isDynamicTransform: true, windowId: id).OnClose(w => proxyProperty?.serializedObject?.Dispose());
        }


        public class BindDataUI : VisualElement
        {
            private const float MinSlimWidth = 220f;
            private const float MinSlimNoLabelWith = 150f;
            private const float MinBindPathWidth = 280f;
            
            private static readonly Regex ParameterRegex = new(@"\[([\d\w]+)\]", RegexOptions.Compiled);

            internal const string BindDataExpandedClass = "bind-data--expanded";

            internal static Predicate<BindDataUI> ShouldDisposeImmediately;

            public static class Colors
            {
                public static Color Debug => BindColors.Debug;
                public static Color ConverterUnsafe => BindColors.ConverterUnsafe;
                public static Color Main => BindColors.Primary;
                public static Color Error => BindColors.Error;
            }

            private static class Labels
            {
                public static string Path_NoPath_NoTarget = "NOTHING SELECTED";

                public static string PathTooltip_NoPath_NoTarget =
                    "Drag and drop an object over this field to bind or click to select the path to bind to global values";

                public static string Path_NoPath = "SELECT PATH";
                public static string PathTooltip_NoPath = "Click to select the path to bind to";

                public static string Color_Debug => EditorGUIUtility.isProSkin ? "#eeae00" : "#b37600";
                public static string Color_ConverterUnsafe => EditorGUIUtility.isProSkin ? "#eeae00" : "#b37600";
                public static string Color_Main => EditorGUIUtility.isProSkin ? "#7fdbef" : "#007acc";
                public static string Color_Error => EditorGUIUtility.isProSkin ? "#ff6666" : "#cc0000";

                private static string WrapColor_Debug => "<color=" + Color_Debug + ">";
                private static string WrapColor_Main => "<color=" + Color_Main + ">";
                private static string WrapColor_End => "</color>";

                public static string LiveDebug => " " + WrapColor_Debug + "(Live Debug)" + WrapColor_End;
                public static string ReadConverter => "Read Converter: ";
                public static string WriteConverter => "Write Converter: ";

                public static string BindMode(BindMode mode) => mode switch
                {
                    BindingSystem.BindMode.Read => "Reads From",
                    BindingSystem.BindMode.Write => "Writes To",
                    BindingSystem.BindMode.ReadWrite => "Reads From & Writes To",
                    _ => "Unknown"
                };
            }

            private static int? _initFrame;

            private readonly BindDataDrawer _owner;
            private readonly string _propertyPath;
            private readonly SerializedObject _serializedObject;
            private readonly SerializedProperty _property;

            private bool _isRefreshing;
            private CommonTargetView _commonTargetView;
            private bool? _prevVisibility;
            private int _prevModifiersCount;
            private BindProxyDrawer.BindProxyView _bindProxyView;
            private VisualElement _lastParent;

            public Toggle bindToggle;
            public VisualElement bindRoot;
            public BindPreviewUI bindPreview;

            public Button bindMenu;
            public ObjectField targetField;
            public Label targetFieldMessage;
            public Button bindMode;
            public Image bindType;
            public Toggle debugMode;
            public Toggle showConverterFields;
            public Toggle showModifiers;
            public Toggle showPreviewToggle;
            public Toggle showTargetField;
            public PropertyField changedEventField;
            public ConverterHandler.ConverterView readConverter;
            public ConverterHandler.ConverterView writeConverter;
            public Modifiers.ModifiersView preModifiers;
            public Modifiers.ModifiersView modifiers;
            public DebugViews debugViews;
            public Image rerouteIcon;

            public EnhancedFoldout pathView;
            public DropdownButton pathButton;
            private Button expandButton;

            public BindProxyDrawer.BindProxyView BindProxyView
            {
                get
                {
                    if (_bindProxyView == null && panel != null)
                    {
                        _bindProxyView = this.QueryParent<BindProxyDrawer.BindProxyView>(levels: 3);
                    }
                    return _bindProxyView;
                }
                set
                {
                    _bindProxyView = value;
                }
            }

            public bool IsForProxy => BindProxyView != null || _propertyPath.TrimStart('_').StartsWith("bindings.Array.data[");
            public PropertyData Data => _owner.GetDataFast(_property);
            public SerializedProperty Property => _serializedObject.FindProperty(_propertyPath);

            // Constructor. Assigns the property
            public BindDataUI(BindDataDrawer owner, SerializedProperty property)
            {
                this.AddBSStyle().AddToClassList("bind-data");

                _owner = owner;
                _property = property.Copy();
                _propertyPath = property.propertyPath;
                _serializedObject = property.serializedObject;

                //

                var data = Data;
                if (data == null)
                {
                    Debug.LogError($"BindDataUI: Property {property.propertyPath} is not supported");
                    return;
                }
                
                _prevModifiersCount = data.modifiers.array?.Length ?? 0;

                RegisterCallback<AttachToPanelEvent>(AttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            private void AttachToPanel(AttachToPanelEvent evt)
            {
                UnregisterCallback<AttachToPanelEvent>(AttachToPanel);

                _owner.PropertyChanged -= Refresh;
                _owner.PropertyChanged += Refresh;

                OnFocusedInternal -= CheckIfShouldFocus;
                OnFocusedInternal += CheckIfShouldFocus;

                Undo.undoRedoPerformed -= ResetAndRefresh;
                Undo.undoRedoPerformed += ResetAndRefresh;

                bindToggle = parent.parent?.Q<Toggle>(className: "bs-bind-toggle");
                bindRoot = bindToggle?.FindCommonAncestor(this);

                _lastParent = parent;
                
                parent.UnregisterCallback<TooltipEvent>(OnParentTooltip);
                parent.RegisterCallback<TooltipEvent>(OnParentTooltip);

                _initFrame ??= Time.frameCount;
                if (Time.frameCount == _initFrame)
                {
                    schedule.Execute(Refresh).ExecuteLater(0);
                }
                else
                {
                    Refresh();
                }

                TryRegisterForReplaceTarget();
            }

            private void OnParentTooltip(TooltipEvent evt)
            {
                evt.tooltip = null;
                evt.StopPropagation();
            }

            private void TryRegisterForReplaceTarget()
            {
                if (!BindingSettings.Current.ShowTargetGroupReplacement)
                {
                    return;
                }

                if (_commonTargetView is { panel: not null })
                {
                    _commonTargetView.OnTargetReplace += OnCommonTargetViewReplace;
                    return;
                }

                var foldout = this.QueryParent<Foldout>(f =>
                        !f.parent.ClassListContains("modifier-view--one-line") 
                        && !f.ClassListContains("bind-data__path") 
                        && !f.ClassListContains("ignore-target-replacement"),
                    null, "common-target-foldout");
                
                foldout ??= this.QueryParent<Foldout>(f =>
                        !f.parent.ClassListContains("modifier-view--one-line") 
                        && !f.ClassListContains("bind-data__path") 
                        && !f.ClassListContains("ignore-target-replacement"),
                    null, "unity-foldout--depth-0");

                if (foldout?.ClassListContains("bind-converter__view") == true)
                {
                    return;
                }

                var foldoutToggle = foldout?.Q(className: Foldout.inputUssClassName);
                if (foldoutToggle == null)
                {
                    TryRegisterToHeaderForReplaceTarget();
                    return;
                }

                _commonTargetView = foldoutToggle.Q<CommonTargetView>();
                if (_commonTargetView == null)
                {
                    _commonTargetView = new CommonTargetView(foldout);
                    foldoutToggle.Add(_commonTargetView);
                }

                _commonTargetView.OnTargetReplace += OnCommonTargetViewReplace;
            }

            private void TryRegisterToHeaderForReplaceTarget()
            {
                var propertyField = this.QueryParent<PropertyField>(classname: "bind-property");
                if (propertyField == null)
                {
                    return;
                }

                var headersContainer = this.QueryParent<VisualElement>(v => !v.ClassListContains("ignore-target-replacement"), null, "properties-with-header");
                if (headersContainer == null)
                {
                    return;
                }

                var index = headersContainer.IndexOf(propertyField);
                if (index < 0)
                {
                    return;
                }

                while (index >= 0)
                {
                    var sibling = headersContainer[index--];
                    if (!sibling.ClassListContains("property-with-header"))
                    {
                        continue;
                    }

                    var headerLabel = sibling.HFind<Label>(null, "unity-header-drawer__label");
                    if (headerLabel == null)
                    {
                        continue;
                    }

                    _commonTargetView = headerLabel.Q<CommonTargetView>();
                    if (_commonTargetView == null)
                    {
                        _commonTargetView = new CommonTargetView(null);
                        headerLabel.Add(_commonTargetView);
                    }

                    _commonTargetView.OnTargetReplace += OnCommonTargetViewReplace;
                    return;
                }
            }

            private bool OnCommonTargetViewReplace(Object target, bool forced)
            {
                var data = Data;

                SetTargetFieldValue(target, data, out var isValid, false, true);
                if (!isValid && !forced)
                {
                    data.serializedObject.Update();
                    data.commonSource.Update();
                    data.prevTarget = (true, data.properties.target.objectReferenceValue);
                    return false;
                }

                _owner.ApplyChanges(data);

                showTargetField.AddToClassList("target-changed");
                showTargetField.schedule.Execute(() => showTargetField.RemoveFromClassList("target-changed"))
                    .ExecuteLater(1000);

                return true;
            }

            private void ApplyChanges(Action<PropertyData> action)
            {
                var data = Data;
                action(data);
                _owner.ApplyChanges(data);
            }

            private void DetachFromPanel(DetachFromPanelEvent evt)
            {
                if (ShouldDisposeImmediately == null || ShouldDisposeImmediately(this))
                {
                    DisposeView();
                }
                
                (_lastParent ?? parent)?.UnregisterCallback<TooltipEvent>(OnParentTooltip);
            }

            private void DisposeView()
            {
                _owner.PropertyChanged -= Refresh;
                OnFocusedInternal -= CheckIfShouldFocus;
                Undo.undoRedoPerformed -= ResetAndRefresh;

                if (_commonTargetView != null)
                {
                    _commonTargetView.OnTargetReplace -= OnCommonTargetViewReplace;
                }
            }

            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                var isVisible = evt.newRect.width > 0 && evt.newRect.height > 0;
                if (isVisible != _prevVisibility)
                {
                    OnVisibilityChanged(isVisible);
                    _prevVisibility = isVisible;
                }

                UpdateIfSlimVersion();

                // Refresh Connecting Lines
                bool showWarnings = false;
#if BS_DEBUG
                showWarnings = true;
#endif
                this.Query<ConnectingLine>().ForEach(c => c.Refresh(showWarnings));
            }

            private void UpdateIfSlimVersion()
            {
                EnableInClassList("slim", layout.width < (ClassListContains("bind-data--no-label") ? MinSlimNoLabelWith : MinSlimWidth));
            }

            private void OnVisibilityChanged(bool isVisible)
            {
                if (isVisible)
                {
                    TryRegisterForReplaceTarget();
                }
                else
                {
                    if (_commonTargetView != null)
                    {
                        _commonTargetView.OnTargetReplace -= OnCommonTargetViewReplace;
                    }
                }
            }

            public bool Rebuild(bool forced = false)
            {
                if (childCount > 0 && !forced)
                {
                    // The UI is already built
                    return false;
                }

                Clear();
                Build(Data);
                return true;
            }

            private void ResetAndRefresh()
            {
                try
                {
                    var data = Data;
                    data.readConverter = default;
                    data.writeConverter = default;
                    data.modifiers = default;

                    if (!_serializedObject.IsAlive()
                        || !_serializedObject.targetObject
                        || !data.properties.property.IsAlive())
                    {
                        return;
                    }

                    Refresh();
                }
#if BS_DEBUG
                catch (Exception ex)
                {
                    // Nothing for now...
                    Debug.LogException(ex);
                }
#else
                catch (Exception)
                {
                    return;
                }
#endif
            }

            private void Refresh(string propertyPath)
            {
                // Check if the property path is the same as the one we are monitoring
                if (!propertyPath.Equals(_propertyPath, StringComparison.Ordinal))
                {
                    return;
                }

                Refresh();
            }

            public void Refresh()
            {
                if (_isRefreshing)
                {
                    return;
                }

                if (panel == null)
                {
                    DisposeView();
                    return;
                }

                _isRefreshing = true;

                try
                {
                    if (Rebuild())
                    {
                        //return;
                    }

                    if (!_serializedObject.IsAlive())
                    {
                        return;
                    }

                    PreprocessData();

                    CheckForErrors();

                    var data = Data;

                    if (Application.isPlaying)
                    {
                        var target = bindRoot ?? this;
                        var isRuntimeFail = !data.sourceNotNeeded && !data.properties.target.objectReferenceValue
                                             || (string.IsNullOrEmpty(data.properties.path.stringValue) &&
                                                 !data.isSelfReference);
                        target.EnableInClassList("runtime-fail", isRuntimeFail);
                        BindProxyView?.EnableInClassList("runtime-fail", isRuntimeFail);;
                    }

                    SetTargetValuePassive(data);

                    SetPathString(pathButton, data);
                    if (data.commonPath.isMixedValue == true)
                    {
                        pathButton.value = _owner.contents.mixedValue.text +
                                           "    Multiple Paths".RT().Size(10).Color(Colors.Main);
                    }

                    debugMode.value = BindData.BitFlags.LiveDebug.IsFlagOf(data.properties.flags.intValue);
                    if (data.commonBindMode.isMixedValue == true)
                    {
                        bindMode.EnableInClassList("multiple-modes", true);
                        bindMode.EnableInClassList("bind-mode--read", false);
                        bindMode.EnableInClassList("bind-mode--write", false);
                    }
                    else
                    {
                        bindMode.EnableInClassList("multiple-modes", false);
                        bindMode.EnableInClassList("bind-mode--read", data.properties.BindMode.CanRead());
                        bindMode.EnableInClassList("bind-mode--write", data.properties.BindMode.CanWrite());
                    }

                    bindMode.tooltip = Labels.BindMode(data.properties.BindMode);
                    
                    RefreshRerouting(data);

                    RefreshPreview(data);

                    RefreshChangedEvent(data);

                    RefreshMinimalUI(data);

                    RefreshParameters(data);
                    
                    RefreshPreModifiers(data);

                    RefreshConverters(data);

                    RefreshModifiers(data);

                    RefreshDebugViews(data);

                    _isRefreshing = false;

                    if (data.firstRun)
                    {
                        data.firstRun = false;
                        //_owner.ApplyChanges(data);
                    }
                }
                finally
                {
                    _isRefreshing = false;
                }
            }

            private void RefreshRerouting(PropertyData data)
            {
                EnableInClassList("bind-data--is-rerouted", data.reroute.isEnabled);
                rerouteIcon.EnableInClassList("hidden", !data.reroute.isEnabled);
                if (!data.reroute.isEnabled)
                {
                    return;
                }
                
                rerouteIcon.tooltip = @$"{"BIND IS BEING REROUTED".RT().Color(BindColors.Debug)}
<b>Field</b> {data.reroute.from.RT().Bold().Color(BindColors.Primary)} → {data.reroute.toKind.RT().Bold()} {data.reroute.to.RT().Bold().Color(BindColors.Primary)}

When setting or getting the value to this field, it will be rerouted to the {data.reroute.toKind} instead. For example this helps when a property has logic in its accessors which a field is lacking.";
            }

            private void RefreshPreview(PropertyData data)
            {
                var showPreview = data.isPathPreview && data.canPathPreview && data.previewView != null;
                showPreviewToggle.EnableInClassList("hidden", !data.canPathPreview);
                
                bindPreview?.EnableInClassList("hidden", !showPreview);
                if (showPreview && bindPreview == null)
                {
                    Action<bool> onApplyChanged = data.hasCustomUpdates 
                                                ? v =>
                                                    {
                                                        data.updateInEditor = v;
                                                        _owner.ApplyChanges(data);
                                                    } 
                                                : null;
                    bindPreview = new BindPreviewUI(onApplyChanged){CanApply = data.updateInEditor};
                    
                    // Insert it after Target View
                    Insert(2, bindPreview);

                    if (!ClassListContains(BindDataExpandedClass))
                    {
                        ConnectLineTo(bindPreview, offset: new Vector2(-2f, 0), fromOffset: new Vector2(-2f, 0));
                    }
                }

                if (showPreview && data.previewView.parent != bindPreview)
                {
                    bindPreview.Preview = data.previewView;
                    bindPreview.CanEdit = data.pathPreviewCanEdit;
                }
                
                if(showPreview && bindPreview != null)
                {
                    bindPreview.CanApply = data.updateInEditor;
                }
            }

            private void RefreshChangedEvent(PropertyData data)
            {
                changedEventField?.EnableInClassList("hidden", !data.canShowEvents);
            }

            private void SetTargetValuePassive(PropertyData data)
            {
                var value = data.isMultipleTargets && data.commonSource.commonValue
                    ? data.commonSource.commonValue
                    : data.properties.target.objectReferenceValue;

                if (!data.firstRun && targetField.value == value)
                {
                    return;
                }

                targetField.SetValueWithoutNotify(value);

                ClearTargetFieldMessage(data);

                SetCommonTargetValue(data, data.properties.target.objectReferenceValue);
            }
            
            private void RefreshPreModifiers(PropertyData data)
            {
                preModifiers.Refresh(data, _owner.ApplyChanges);
                preModifiers.EnableInClassList("hidden", !showModifiers.value || !(data.preModifiers.array?.Length > 0));
            }
            
            private void RefreshModifiers(PropertyData data)
            {
                modifiers.Refresh(data, _owner.ApplyChanges);
                modifiers.EnableInClassList("hidden", !showModifiers.value || !(data.modifiers.array?.Length > 0));
            }
            
            private void RefreshConverters(PropertyData data)
            {
                var showReadConverter = data.properties.BindMode.CanRead();
                showReadConverter &= showConverterFields.value && data.readConverter.isInteractive;
                var showWriteConverter = data.properties.BindMode.CanWrite();
                showWriteConverter &= showConverterFields.value && data.writeConverter.isInteractive;
                readConverter?.EnableInClassList("hidden", !showReadConverter);
                readConverter?.Refresh(data.properties.readConverter, data.readConverter);
                writeConverter?.EnableInClassList("hidden", !showWriteConverter);
                writeConverter?.Refresh(data.properties.writeConverter, data.writeConverter);
            }

            private void RefreshDebugViews(PropertyData data)
            {
                if (data.shouldDebug)
                {
                    schedule.Execute(() => debugViews.Rebuild(data, this)).ExecuteLater(0);
                }
                else
                {
                    debugViews.Clear();
                }
            }

            private void RefreshParameters(PropertyData data)
            {
                if (!(data.properties.parameters?.arraySize > 0) || data.commonPath.isMixedValue == true)
                {
                    pathView.Clear();
                    return;
                }

                if (!data.parameters.HaveChanged() && pathView.childCount > 0)
                {
                    return;
                }

                if (!data.parameters.IsValid())
                {
                    return;
                }

                var parametersProperty = data.properties.parameters;
                for (int i = 0; i < parametersProperty.arraySize; i++)
                {
                    ref var paramData = ref data.parameters.array[i];
                    var property_i = parametersProperty.GetArrayElementAtIndex(i);
                    var wrapper = new ParameterWrapper()
                    {
                        owner = this,
                        fieldName = paramData.name.NiceName(),
                        propertyPath = property_i.propertyPath
                    };
                    var field = wrapper.CreateField(property_i);
                    field.RegisterValueChangeCallback(evt =>
                    {
                        _owner.UpdatePreviewUIToolkit(data);
                        RefreshPreview(data);
                    });
                    pathView.Add(field);
                }
            }
            
            private void PreprocessData()
            {
                _serializedObject?.Update();
                var data = Data;

                if (data.fixedBindMode.HasValue)
                {
                    data.properties.mode.enumValueIndex = (int)data.fixedBindMode.Value;
                }

                if (data.firstRun)
                {
                    data.UpdateCommonValues();
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
                    catch (Exception)
                    {
                        // if (!data.hasError)
                        // {
                        //     Debug.LogException(ex);
                        // }
                        data.parameters = default;
                    }
                }
                else if (!data.parameters.IsValid())
                {
                    data.parameters.Reset(true);
                    data.parameters = default;
                }

                var (fromType, toType) = _owner.GetTypeMapping(data, false);

                if (data.preModifiers.HaveChanged())
                {
                    data.preModifiers = new Modifiers(data, fromType, true, true);
                }

                if (data.modifiers.HaveChanged())
                {
                    data.modifiers = new Modifiers(data, toType, true, false);
                }

                _owner.UpdateWriteConverter(data, false);
                _owner.UpdateReadConverter(data, false);

                data.shouldDebug = CanShowDebugValues(data);

                if (!data.firstRun) return;

                data.isTargetFieldCollapsed |=
                    !data.sourceTarget && string.IsNullOrEmpty(data.properties.path.stringValue);
                SetCommonTargetValue(data, data.properties.target.objectReferenceValue);
            }

            private void RefreshMinimalUI(PropertyData data)
            {
                EnableInClassList("live-debug", debugMode.value);
                pathView.EnableInClassList("live-debug", debugMode.value);
                bindMode.tooltip = bindMode.tooltip.Replace(Labels.LiveDebug, "");
                if (debugMode.value)
                {
                    bindMode.tooltip += Labels.LiveDebug;
                }

                string TooltipConverter(in ConverterHandler handler)
                {
                    if (handler.instance == null)
                    {
                        return $"No {(handler.isRead ? "Read" : "Write")} Converter Required";
                    }

                    var isSafe = handler.instance.IsSafe
                        ? "[Safe] ".RT().Color(Colors.Main)
                        : "[Unsafe] ".RT().Color(Colors.ConverterUnsafe);

                    return handler.isRead
                        ? isSafe + Labels.ReadConverter + handler.instance.Id.RT().Bold()
                        : isSafe + Labels.WriteConverter + handler.instance.Id.RT().Bold();
                }

                var (isToggleVisible, isUnsafe, tooltip) = data.properties.BindMode switch
                {
                    BindMode.ReadWrite => (data.readConverter.isInteractive || data.writeConverter.isInteractive,
                        data.readConverter.isUnsafe || data.writeConverter.isUnsafe,
                        TooltipConverter(data.readConverter) + '\n' + TooltipConverter(data.writeConverter)),
                    BindMode.Read => (data.readConverter.isInteractive, data.readConverter.isUnsafe,
                        TooltipConverter(data.readConverter)),
                    BindMode.Write => (data.writeConverter.isInteractive, data.writeConverter.isUnsafe,
                        TooltipConverter(data.writeConverter)),
                    _ => (false, false, "")
                };

                var isMultipleTypes = data.commonPath.isMixedValue == true || data.commonSource.isMixedValue == true;

                showConverterFields.value = !data.isConverterFieldCollapsed && !isMultipleTypes /* && isToggleVisible*/;
                showConverterFields.EnableInClassList("hidden", (!isUnsafe && !isToggleVisible) || isMultipleTypes);
                showConverterFields.EnableInClassList("unsafe", isUnsafe);
                showConverterFields.tooltip = tooltip;

                var modifiersCount = (data.modifiers.array?.Length ?? 0) + (data.preModifiers.array?.Length ?? 0);
                var hasNewModifiers = modifiersCount > _prevModifiersCount;
                var hasModifiers = modifiersCount > 0;
                _prevModifiersCount = modifiersCount;
                
                showModifiers.value = ((!data.isModifiersCollapsed && hasModifiers) || hasNewModifiers) && !isMultipleTypes;
                showModifiers.EnableInClassList("hidden", !hasModifiers || isMultipleTypes);
                showModifiers.tooltip = hasModifiers ? "Show Modifiers" : "No Modifiers";
            }

            private void UpdateShowTargetField(PropertyData data, string text, string iconClass, string targetTooltip = null)
            {
                showTargetField.value = !data.isTargetFieldCollapsed;
                showTargetField.EnableInClassList("hidden", false); // !data.sourceTarget && !data.prevTarget.value);

                if (string.IsNullOrEmpty(targetTooltip))
                {
                    showTargetField.tooltip = targetField.Q<Label>(className: "unity-object-field-display__label").text;
                }
                else
                {
                    showTargetField.tooltip = targetTooltip;
                }

                var icon = showTargetField.Q<Image>();
                icon.ClearClassList();
                icon.WithClass("unity-image", "bind-data__path__show-toggle__icon");

                if (string.IsNullOrEmpty(iconClass))
                {
                    icon.image = targetField.Q<Image>(className: "unity-object-field-display__icon").image;
                }
                else
                {
                    icon.image = null;
                    icon.AddToClassList(iconClass);
                }

                var label = showTargetField.Q<Label>();
                label.text = text;
                label.EnableInClassList("hidden", string.IsNullOrEmpty(text));

                if (data.hasError && data.errorClass == Errors.Classes.MissingComponent)
                {
                    showTargetField.tooltip +=
                        "\nRequires " + data.sourcePersistedType?.Name.RT().Color(Colors.Main).Bold();
                }
            }

            private void CheckForErrors()
            {
                var data = Data;

                data.hasError = false;
                data.invalidPath = string.Empty;

                // Clear errors
                this.Query(className: "has-error").ForEach(v =>
                {
                    v.RemoveFromClassList("has-error");
                    var label = v.Q<Label>(classes: BaseField<int>.labelUssClassName);
                    if (label != null)
                    {
                        label.tooltip = null;
                    }
                });

                SetPathTooltip(null);

                if ((data.sourceTarget || data.prevTarget.value) &&
                    !_owner.ValidatePath(data.sourceTarget, data, data.properties.path.stringValue))
                {
                    data.hasError = true;
                    switch (data.errorClass)
                    {
                        case Errors.Classes.BindMode:
                            SetErrorMessage(pathView, data.errorMessage);
                            bindMode.AddToClassList("has-error");
                            SetPathTooltip(data.errorMessage.RT().Color(Colors.Error));
                            break;
                        case Errors.Classes.Path:
                            SetErrorMessage(pathView, data.errorMessage);
                            SetPathTooltip(data.errorMessage.RT().Color(Colors.Error));
                            break;
                        case Errors.Classes.MissingComponent:
                            if (data.isTargetFieldCollapsed)
                            {
                                showTargetField?.AddToClassList("has-error");
                                SetErrorMessage(pathView, data.errorMessage);
                                SetPathTooltip(data.errorMessage.RT().Color(Colors.Error));
                            }
                            else
                            {
                                SetErrorMessage(targetField, data.errorMessage);
                                targetField.Q<Label>(classes: BaseField<int>.labelUssClassName).tooltip =
                                    data.errorMessage;
                                ClearTargetFieldMessage(data, true);
                                targetFieldMessage.text = "missing " + data.sourcePersistedType?.Name;
                            }

                            break;
                        case null:
                            data.hasError = false;
                            break;
                    }
                }

                if (!data.hasError && data.sourceTarget == null &&
                    !string.IsNullOrEmpty(data.properties.path.stringValue) && !data.sourceNotNeeded)
                {
                    data.hasError = true;
                    data.errorClass = Errors.Classes.MissingTarget;
                    data.errorMessage = "No source target set";
                    if (data.sourcePersistedType != null)
                    {
                        data.errorMessage +=
                            "\nRequires " + data.sourcePersistedType.Name.RT().Color(Colors.Main).Bold();
                        data.errorClass = Errors.Classes.MissingComponent;
                        ClearTargetFieldMessage(data, true);
                        targetFieldMessage.text = data.sourcePersistedType.Name;
                        targetFieldMessage.tooltip = data.sourcePersistedType.FullName;
                        targetFieldMessage.AddToClassList("as-error");
                    }

                    SetErrorMessage(pathView, data.errorMessage);
                    SetPathTooltip("The selected path cannot be bound because there is no target object set".RT()
                        .Color(Colors.Error));
                }

                EnableInClassList("has-errors", data.hasError);
                bindRoot?.EnableInClassList("has-bind-errors", data.hasError);
            }

            private void Build(PropertyData data)
            {
                EnableInClassList("minimal", true);

                if (data == null)
                {
                    return;
                }

                BuildTargetView(data);
                BuildPathView(data);
                BuildPreModifiers(data);
                BuildConverters(data);
                BuildModifiers(data);
                BuildDebugViews(data);
                BuildChangedValueEventView(data);

                Add(pathView);
                Add(targetField);

                Add(preModifiers);
                Add(readConverter);
                Add(writeConverter);
                Add(modifiers);

                if (changedEventField != null)
                {
                    Add(changedEventField);
                }

                AddConnectingLines();
                
                CheckIfShouldFocus();
            }

            private void CheckIfShouldFocus()
            {
                if (!FocusRequested())
                {
                    return;
                }
                
                if (!IsForProxy && _owner.MustBeFocused())
                {
                    FocusAnimation();
                    return;
                }
                
                var sourcePropPath = Data.properties.property.propertyPath.ReplaceAtEnd("_bindData", "_proxySource");
                var sourceProp = _serializedObject.FindProperty(sourcePropPath);
                if (sourceProp == null)
                {
                    return;
                }
                
                var pathPropPath = Data.properties.property.propertyPath.ReplaceAtEnd("_bindData", "_proxyPath");
                var pathProp = _serializedObject.FindProperty(pathPropPath);
                if (pathProp == null)
                {
                    return;
                }
                
                if(MustBeFocused(sourceProp.objectReferenceValue, pathProp.stringValue, clearIfTrue: false))
                {
                    FocusAnimation();
                }
            }
            
            private void FocusAnimation()
            {
                schedule.Execute(() =>
                {
                    ClearFocus();
                    AddToClassList("must-focus");
                    schedule.Execute(() => RemoveFromClassList("must-focus")).ExecuteLater(1000);
                }).ExecuteLater(50);
            }

            private void BuildChangedValueEventView(PropertyData data)
            {
                if (data.properties.valueChangedEvent == null)
                {
                    return;
                }

                changedEventField = new PropertyField()
                    .EnsureBind(data.properties.valueChangedEvent)
                    .WithClass("bind-data__changed-event");
                changedEventField.RegisterCallback<AttachToPanelEvent>(UpdateChangedEventView);
            }

            private void UpdateChangedEventView(AttachToPanelEvent evt)
            {
                changedEventField.UnregisterCallback<AttachToPanelEvent>(UpdateChangedEventView);
                changedEventField.schedule.Execute(() =>
                {
                    var data = Data;
                    if (changedEventField.childCount == 0)
                    {
                        changedEventField.BindProperty(data.properties.valueChangedEvent);
                    }

                    VisualElement headerLabel = changedEventField.Q<Label>(null, "unity-list-view__header");
                    ConnectLineTo(headerLabel ?? changedEventField, changedEventField);
                }).ExecuteLater(0);
            }

            private void BuildDebugViews(PropertyData data)
            {
                debugViews = new DebugViews();
            }

            private void BuildTargetView(PropertyData data)
            {
                // Create target field
                var label = "From";
                targetField = new ObjectField(label).StyleAsField().WithClass("bind-data__target");
                targetField.EnableInClassList("minimal", true);
                targetField.Insert(0, CreateErrorPoint());
                targetFieldMessage = new Label("Bind Source").WithClass("bind-data__target__info");
                targetField.Q(className: ObjectField.objectUssClassName)?.Add(targetFieldMessage);
                data.prevTarget = (true, data.properties.target?.objectReferenceValue);
                targetField.RegisterValueChangedCallback(evt =>
                {
                    var data = Data;
                    SetTargetFieldValue(evt.newValue, data, out _);

                    _owner.ApplyChanges(data);
                });
            }

            private void SetTargetFieldValue(Object newValue, PropertyData data, out bool isValid,
                bool logErrorsToConsole = true, bool silent = false)
            {
                ClearTargetFieldMessage(data);

                if (data.commonSource.isMultipleTypes)
                {
                    _owner.SetMultiTypeTarget(newValue, data, out isValid, logErrorsToConsole);
                }
                else
                {
                    _owner.SetTargetValue(newValue, data, out isValid, logErrorsToConsole, silent);
                    data.commonSource.Update();
                }

                SetCommonTargetValue(data, data.properties.target.objectReferenceValue);

                _owner.UpdatePathPreview(data);

                data.prevTarget = (true, data.properties.target.objectReferenceValue);
            }

            private void SetCommonTargetValue(PropertyData data, Object newTarget)
            {
                var contents = _owner.contents;
                if (data.commonSource.isMultipleTypes && data.commonSource.commonValue)
                {
                    targetField.showMixedValue = false;
                    targetFieldMessage.SetContent(contents.multipleTargetComponentTypes);
                    targetFieldMessage.AddToClassList("as-warning");
                    UpdateShowTargetField(data, data.commonSource.values?.Count.ToString(), null);
                }
                else if (data.commonSource.isMultipleTypes && data.commonSource.commonType == null)
                {
                    targetField.showMixedValue = true;
                    targetFieldMessage.SetContent(contents.multipleTargetIncompatibleTypes);
                    targetFieldMessage.AddToClassList("as-error");
                    var classes = data.commonSource.values;
                    var count = classes?.Count().ToString();
                    var targetTooltip = ToFormattedTooltip("INCOMPATIBLE TYPES".RT().Bold().Color(Colors.Error), classes);
                    UpdateShowTargetField(data, count, "incompatible-types", targetTooltip);
                }
                else if (data.commonSource.isMultipleTypes)
                {
                    targetField.showMixedValue = true;
                    targetFieldMessage.SetContent(contents.multipleTargetTypes);
                    targetFieldMessage.AddToClassList("as-info");
                    var classes = data.commonSource.values;
                    var count = classes?.Count().ToString();
                    var targetTooltip = ToFormattedTooltip("MULTIPLE TYPES".RT().Bold().Color(Colors.Debug), classes);
                    UpdateShowTargetField(data, count, "multiple-types", targetTooltip);
                }
                else if (data.commonSource.isMixedValue == true)
                {
                    targetField.showMixedValue = true;
                    targetFieldMessage.SetContent(contents.multipleTargetObjects);
                    targetFieldMessage.AddToClassList("as-info");
                    UpdateShowTargetField(data, data.serializedObject.targetObjects.Length.ToString(),
                        "multiple-objects");
                }
                else if (newTarget == null)
                {
                    targetField.showMixedValue = false;
                    targetFieldMessage.SetContent(contents.target);
                    targetFieldMessage.AddToClassList("as-info");
                    UpdateShowTargetField(data, null, "null-object", "Show Bind Source Field");
                }
                else
                {
                    targetField.showMixedValue = false;
                    UpdateShowTargetField(data, null, null);
                }
            }

            private string ToFormattedTooltip(string title, IEnumerable<KeyValuePair<Object, Object>> classes)
            {
                if (classes == null || !classes.Any())
                {
                    return null;
                }

                var sb = new StringBuilder();
                sb.AppendLine(title);
                foreach (var pair in classes)
                {
                    sb.Append(pair.Key?.name.RT().Color(Colors.Main))
                        .Append(": ");
                    if (pair.Value)
                    {
                        sb.Append(pair.Value.name.RT().Bold())
                            .Append(" -> ")
                            .AppendLine(pair.Value.GetType().FullName.RT().Bold());
                    }
                    else
                    {
                        sb.AppendLine("NULL".RT().Bold().Color(Colors.Error));
                    }
                }

                sb.Length--;

                return sb.ToString();
            }

            private void ClearTargetFieldMessage(PropertyData data, bool forced = false)
            {
                if (!data.hasError || forced)
                {
                    targetFieldMessage.RemoveFromClassList("as-error");
                    targetFieldMessage.RemoveFromClassList("as-warning");
                    targetFieldMessage.RemoveFromClassList("as-info");
                    targetFieldMessage.text = string.Empty;
                    targetFieldMessage.tooltip = string.Empty;
                }
            }
            
            private void ExpandView()
            {
                var thisClone = new BindDataUI(_owner, _property).WithClass(BindDataExpandedClass);
                thisClone.Rebuild();
                thisClone.pathView.label.text = pathView.label.text;
                var width = Mathf.Max(450, layout.width);
                var height = 100f;
                PopupWindow.Show(this, new Vector2(width, height), thisClone, true);
            }
            
            private void BuildPathView(PropertyData data)
            {
                AddToClassList("bind-data--no-label");
                var label = data.label?.text;
                
                pathButton = new DropdownButton().StyleAsInspectorField()
                    .WithClass("bind-data__path-dropdown")
                    .WithoutClass(BaseField<int>.ussClassName);
                pathButton.buttonElement.WithClass(
                    "bind-data__path__button"); // Maybe should bind the value to the button text
                pathButton.buttonElement.enableRichText = true;
                
                pathView = new EnhancedFoldout().WithClass("bind-data__path").MakeAsField();
                pathView.text = label;
                pathView.viewDataKey = _property.propertyPath + "_pathView";
                pathView.label.WithClass("bind-field__label");
                pathView.label.RegisterValueChangedCallback(evt =>
                    EnableInClassList("bind-data--no-label", string.IsNullOrEmpty(evt.newValue)));
                pathView.label.schedule.Execute(() =>
                {
                    UpdateIfSlimVersion();
                    EnableInClassList("bind-data--no-label", string.IsNullOrEmpty(pathView.label?.text));
                }).ExecuteLater(20);
                pathView.toggle.Q<Label>(null, Foldout.textUssClassName).WithClass("bind-field__label");
                pathView.EnableInClassList("minimal", true);

                pathView.AlignField(() => !ClassListContains("slim"));

                pathButton.buttonElement.clickable = new MultiClickable(
                    new Clickable(ShowPathPopupMenu),
                    CreateCtrlClickable(ExpandView, "expand-view")
                );

                // Bind mode button
                bindMode = new Button() { focusable = false }
                    .WithClass("bind-data__path__mode")
                    .WithChildren(new Image().WithClass("bind-data__path__mode__icon"));

                bindMode.EnableInClassList("can-change-value", _owner._canChangeMode);

                var changeBindModeClick = new Clickable(() => // Single Click
                {
                    if (!_owner._canChangeMode)
                    {
                        return;
                    }

                    data.properties.mode.enumValueIndex = (int)data.properties.BindMode.NextMode();
                    data.commonBindMode.Update();
                    _owner.ApplyChanges(data);
                });
                var toggleLiveDebugClick = CreateCtrlClickable(() => // CTRL + Click
                {
                    EnableLiveDebug(!debugMode.value);
                }, "live-debug-reveal");

                bindMode.clickable = new MultiClickable(changeBindModeClick, toggleLiveDebugClick);

                // Debug mode toggle
                debugMode = new Toggle().WithClass("bind-data__path__debug");
                debugMode.tooltip = "Live Debug is Inactive\nClick to enable it";
                debugMode.RegisterValueChangedCallback(evt => EnableLiveDebug(evt.newValue));

                pathButton.Insert(0, bindMode);

                pathButton.AddManipulator(new ObjectDNDManipulator(v =>
                {
                    if (v == targetField.value)
                    {
                        return;
                    }

                    var prevValue = targetField.value;
                    targetField.SetValueWithoutNotify(v);
                    var data = Data;
                    if (v is Component c)
                    {
                        v = c.gameObject;
                    }
                    SetTargetFieldValue(v, data, out var isValid);
                    var path = data.properties.path.stringValue;
                    if (string.IsNullOrEmpty(path) || !isValid)
                    {
                        _owner.ApplyChangesSilently(data);
                        ShowPathPopupMenuOnDrop(prevValue);
                    }
                    else
                    {
                        pathButton.AddToClassList("dnd-succeeded");
                        _owner.ApplyChanges(data);
                        pathButton.schedule.Execute(() => pathButton.RemoveFromClassList("dnd-succeeded"))
                            .ExecuteLater(1000);
                    }
                }, true, null));

                pathButton.Add(new Label("CHANGE SOURCE").WithClass("bind-data__path__drag-label", "valid"));
                pathButton.Add(new Label("INVALID SOURCE").WithClass("bind-data__path__drag-label", "invalid"));
                pathButton.Add(new Label("CHANGED!").WithClass("bind-data__path__drag-label", "changed"));
                pathButton.Add(new Label("CANCELLED!").WithClass("bind-data__path__drag-label", "cancel"));

                var typeLabel = new Label(data.bindType?.UserFriendlyName())
                    { tooltip = data.bindType.UserFriendlyFullName() }.WithClass("bind-data__path__type-label");
                pathButton.buttonElement.Add(typeLabel);

                showModifiers = new Toggle() { focusable = false }
                    .WithClass(Button.ussClassName, "bind-data__path__show-toggle",
                        "bind-data__path__show-toggle--modifiers")
                    .WithChildren(new Image().WithClass("bind-data__path__show-toggle__icon"));
                showModifiers.RegisterValueChangedCallback(evt =>
                {
                    // Show modifiers
                    data.isModifiersCollapsed = !evt.newValue;
                    _owner.ApplyChanges(data);
                    //RefreshConvertersVisibility(data);
                });
                pathButton.Insert(1, showModifiers);
                
                showConverterFields = new Toggle() { focusable = false }
                    .WithClass(Button.ussClassName, "bind-data__path__show-toggle",
                        "bind-data__path__show-toggle--converters")
                    .WithChildren(new Image().WithClass("bind-data__path__show-toggle__icon"));
                showConverterFields.RegisterValueChangedCallback(evt =>
                {
                    // Show converter fields
                    data.isConverterFieldCollapsed = !evt.newValue;
                    _owner.ApplyChanges(data);
                    //RefreshConvertersVisibility(data);
                });
                pathButton.Insert(2, showConverterFields);
                
                showPreviewToggle = new Toggle()
                    {
                        focusable = false, 
                        value = data.isPathPreview,
                        tooltip = "Preview data pointed by the path"
                    }
                    .WithClass(Button.ussClassName, "bind-data__path__show-toggle",
                        "bind-data__path__show-toggle--preview")
                    .WithChildren(new Image().WithClass("bind-data__path__show-toggle__icon"));
                showPreviewToggle.RegisterValueChangedCallback(evt =>
                {
                    // Show converter fields
                    data.isPathPreview = evt.newValue && data.canPathPreview;
                    _owner.ApplyChanges(data);
                    //RefreshConvertersVisibility(data);
                });
                pathButton.Insert(3, showPreviewToggle);

                showTargetField = new Toggle()
                    {
                        focusable = false, 
                        value = !data.isTargetFieldCollapsed,
                        tooltip = "Show Bind Source Field"
                    }
                    .WithClass(Button.ussClassName, "bind-data__path__show-toggle",
                        "bind-data__path__show-toggle--target")
                    .WithChildren(new Image().WithClass("bind-data__path__show-toggle__icon", "null-object"),
                        new Label().WithClass("bind-data__path__show-toggle__label"));
                showTargetField.RegisterValueChangedCallback(evt =>
                {
                    targetField.EnableInClassList("hidden", !evt.newValue);
                    data.isTargetFieldCollapsed = !evt.newValue;
                    _owner.ApplyChanges(data);
                });
                var pingObjectClick = CreateCtrlClickable(() => // CTRL + Click
                {
                    if (targetField.value)
                    {
                        EditorGUIUtility.PingObject(targetField.value);
                    }
                }, "ping-object");
                showTargetField.AddManipulator(pingObjectClick);

                targetField.EnableInClassList("hidden", data.isTargetFieldCollapsed);
                pathButton.Insert(4, showTargetField);

                bindMenu = CreateBindMenuButton();
                pathButton.Add(bindMenu);

                rerouteIcon = new Image().WithClass("bind-data__reroute-icon");
                
                pathView.startOfHeader.Insert(0, CreateErrorPoint());
                pathView.startOfHeader.Insert(1, rerouteIcon);
                pathView.restOfHeader.Add(pathButton);
                
                expandButton = new Button(ExpandView)
                    {
                        tooltip = "Expand Field",
                        focusable = false,
                    }.WithClass("bind-data__path__expand")
                        .WithChildren(new Image().WithClass("bind-data__path__expand__icon"), 
                            new Label("BIND").WithClass("bind-data__path__expand__label"));
                pathView.restOfHeader.Add(expandButton);

                return;

                Clickable CreateCtrlClickable(Action action, string previewClass)
                {
                    var clickable = new PreviewClickable(action, previewClass);
                    clickable.activators.Clear();
                    clickable.activators.Add(new ManipulatorActivationFilter()
                    {
                        modifiers = Application.platform == RuntimePlatform.OSXEditor
                            ? EventModifiers.Command
                            : EventModifiers.Control
                    });
                    return clickable;
                }
            }

            private void ShowPathPopupMenu()
            {
                // Add a stopwatch to measure time
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                Profiler.BeginSample("BindDataDrawer.ShowPathPopupMenu");

                Profiler.BeginSample("BindDataDrawer.PreparePathPopupMenu");
                var (menu, rect) = PreparePathPopupMenu();
                Profiler.EndSample();

                var timeToPrepare = stopwatch.ElapsedMilliseconds;
                Profiler.BeginSample("BindDataDrawer.ShowPathPopupMenuOnScreen");
                menu.Show(rect, isScreenPosition: true);
                Profiler.EndSample();

                Profiler.EndSample();
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 2000)
                {
                    Debug.LogWarning($"Showing Path Popup Menu for {menu.ItemsCount} elements took: \n" +
                                     $"\t- Values Preparation: {timeToPrepare}ms\n" +
                                     $"\t- UI Loading: {stopwatch.ElapsedMilliseconds}ms\n" +
                                     $"If you're seeing this message, contact the developer with the details of the operation.");
                }
            }

            private (SmartDropdown menu, Rect rect) PreparePathPopupMenu()
            {
                var rect = pathButton.buttonElement.worldBound;
                rect.xMin = bindMode.worldBound.xMin;
                rect = GUIUtility.GUIToScreenRect(rect);
                rect.width = Mathf.Max(rect.width, MinBindPathWidth);
                var data = Data;
                var menu = _owner.HandleShowPopup(data, data.properties.path.stringValue, data.properties.property);

                return (menu, rect);
            }

            private void ShowPathPopupMenuOnDrop(Object prevValue)
            {
                var (menu, rect) = PreparePathPopupMenu();
                rect.width = Mathf.Max(rect.width, MinBindPathWidth);
                schedule.Execute(() => menu.Show(rect, isScreenPosition: true, onClose: r =>
                {
                    var data = Data;
                    if (data.serializedObject.isEditingMultipleObjects ||
                        r == SmartDropdown.CloseReason.ItemWasSelected)
                    {
                        pathButton.AddToClassList("dnd-succeeded");
                        pathButton.schedule.Execute(() => pathButton.RemoveFromClassList("dnd-succeeded"))
                            .ExecuteLater(1000);
                    }
                    else if (!string.IsNullOrEmpty(data.properties.path.stringValue))
                    {
                        targetField.value = prevValue;
                        pathButton.AddToClassList("dnd-failed");
                        pathButton.schedule.Execute(() => pathButton.RemoveFromClassList("dnd-failed"))
                            .ExecuteLater(1000);
                        Refresh();
                    }
                    else
                    {
                        Refresh();
                    }
                })).ExecuteLater(0);
            }

            private void BuildConverters(PropertyData data)
            {
                readConverter = new ConverterHandler.ConverterView(data.properties.readConverter,
                    _owner.styles,
                    "Read Convert",
                    c => ApplyChanges(d => d.properties.readConverter.managedReferenceValue = c)).WithClass("read");
                writeConverter = new ConverterHandler.ConverterView(data.properties.writeConverter, _owner.styles,
                    "Write Convert",
                    c => ApplyChanges(d => d.properties.writeConverter.managedReferenceValue = c)).WithClass("write");
            }
            
            private void BuildPreModifiers(PropertyData data)
            {
                preModifiers = new Modifiers.ModifiersView(data, _owner.contents, usePreModifiers: true);
                preModifiers.AddToClassList("pre-modifiers");
            }
            
            private void BuildModifiers(PropertyData data)
            {
                modifiers = new Modifiers.ModifiersView(data, _owner.contents);
            }

            private void EnableLiveDebug(bool enable)
            {
                debugMode.tooltip = enable ? "Live Debug is Active" : "Live Debug is Inactive\nClick to enable it";

                var data = Data;
                data.properties.flags.intValue =
                    BindData.BitFlags.LiveDebug.EnableFlagIn(enable, data.properties.flags.intValue);
                _owner.ApplyChanges(data);
            }

            private Button CreateBindMenuButton()
            {
                return new Button(ShowBindMenu) { focusable = false }.WithClass("bind-data__menu")
                    .WithChildren(new Image().WithClass("bind-data__menu__icon"));
            }

            private string SetPathString(DropdownButton pathButton, PropertyData data)
            {
                // TODO: Beware of property values as those can be from providers (thus the id instead of the path)
                var value = data.properties.path.stringValue;
                var contents = _owner.contents;

                pathView.RemoveFromClassList("self-reference");
                pathView.RemoveFromClassList("path-not-set");
                pathView.RemoveFromClassList("target-set");

                if (BindProxyView != null)
                {
                    BindProxyView.RemoveFromClassList("path-not-set");
                    BindProxyView.RemoveFromClassList("target-set");
                }

                if (string.IsNullOrEmpty(value) /* && string.IsNullOrEmpty(data.formattedValue)*/)
                {
                    if (!data.isSelfReference)
                    {
                        pathView.AddToClassList("path-not-set");
                        BindProxyView?.AddToClassList("path-not-set");
                        data.formattedValue = data.properties.target.objectReferenceValue
                            ? $" <color={Styles.helpColor}><i>Select path...</i></color>"
                            : $" <color={Styles.helpColor}><i>Drag an object or select path...</i></color>";
                        if (data.properties.target.objectReferenceValue)
                        {
                            pathButton.value = Labels.Path_NoPath;
                            pathButton.tooltip = Labels.PathTooltip_NoPath;
                            pathView.AddToClassList("target-set");
                            BindProxyView?.AddToClassList("target-set");
                        }
                        else
                        {
                            pathButton.value = Labels.Path_NoPath_NoTarget;
                            pathButton.tooltip = Labels.PathTooltip_NoPath_NoTarget;
                        }

                        contents.formattedPath.text = data.formattedValue;
                        contents.formattedPath.tooltip = string.Empty;
                    }
                    else
                    {
                        data.formattedValue = "SELF";
                        pathView.AddToClassList("self-reference");
                        pathButton.value = "SELF";
                        pathButton.tooltip = "The object reference will be used as value";
                        contents.formattedPath.text = data.formattedValue;
                        contents.formattedPath.tooltip = "The object reference will be used as value";
                    }

                    data.prevValue = value;

                    return contents.formattedPath.text;
                }

                contents.formattedPath.tooltip = string.Empty;

                data.prevValue = value;
                if (!data.firstRun)
                {
                    data.hasError = false;
                }

                char separator = '/';

                // Check if it comes from a provider
                if (!string.IsNullOrEmpty(value)
                    && AccessorPath.TryGetProviderId(value, out var providerId, out var cleanId))
                {
                    var providers = BindTypesCache.GetAllAccessorProviders();
                    var separatorString = new string(separator, 1);
                    if (providers.TryGetValue(providerId, out var provider) &&
                        provider.TryConvertIdToPath(cleanId, separatorString, out var nicePath))
                    {
                        value = providerId + '/' + nicePath;
                    }
                }

                if (ReflectionFactory.CurrentOptions.UseNiceNames)
                {
                    value = _owner.NicifyPath(value);
                    // Switch to arrows
                    separator = '→';
                }

                try
                {
                    if (data.parameters.IsValid() && data.parameters.parameters.Length > 0)
                    {
                        // Replace path with parameters values.
                        // E.g. "MyList[string, int]/Value" => "MyList[Hello, 5]/Value"
                        // Use regexp to find all occurrences of [index] and replace them
                        var matches = ParameterRegex.Matches(value);
                        var parametersLength = data.parameters.parameters.Length;
                        for (int i = 0; i < matches.Count && i < parametersLength; i++)
                        {
                            var match = matches[i];
                            var param = data.parameters.parameters[i];
                            var paramValue = param?.Value?.ToString();
                            if (!string.IsNullOrEmpty(paramValue))
                            {
                                value = value.Replace(match.Value, $"[{paramValue}]");
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore
                }

                var lastSeparatorIndex = value.LastIndexOf(separator) + 1;
                if (lastSeparatorIndex > 0)
                {
                    var lessImportantPart = value[..lastSeparatorIndex];
                    var moreImportantPart = value[lastSeparatorIndex..];
                    var lessImportantSize = Mathf.Max(11, pathButton.resolvedStyle.fontSize - 1);
                    var formattedValue = lessImportantPart.RT()
                                             .Color(EditorStyles.label.normal.textColor.WithAlpha(0.75f))
                                             .Size(lessImportantSize)
                                         + moreImportantPart.RT().Bold();

                    data.formattedValue = formattedValue;
                    contents.formattedPath.tooltip = value.Replace("#%", "", StringComparison.Ordinal)
                        .Replace("%#", "", StringComparison.Ordinal);
                }
                else
                {
                    data.formattedValue = value;
                    contents.formattedPath.tooltip = string.Empty;
                }

                if (!string.IsNullOrEmpty(data.invalidPath))
                {
                    var niceInvalidPath = _owner.NicifyPath(data.invalidPath);
                    data.formattedValue =
                        data.formattedValue.Replace(niceInvalidPath, niceInvalidPath.RT().Color(Colors.Error));
                }

                pathButton.value = data.formattedValue;
                pathButton.tooltip = contents.formattedPath.tooltip;

                contents.formattedPath.text = data.formattedValue;
                return contents.formattedPath.text;
            }

            private VisualElement CreateErrorPoint()
            {
                var errorElement = new Button() { text = "!", focusable = false }.WithClass("error-point");
                return errorElement;
            }

            private void SetPathTooltip(string pathTooltip)
            {
                var parentTooltip = parent?.tooltip;
                if (string.IsNullOrEmpty(pathTooltip))
                {
                    pathView.tooltip = parentTooltip;
                    pathView.label.tooltip = parentTooltip;
                }
                else if (string.IsNullOrEmpty(parentTooltip))
                {
                    pathView.tooltip = pathTooltip;
                    pathView.label.tooltip = pathTooltip;
                }
                else
                {
                    pathView.tooltip = parentTooltip + "\n\n" + pathTooltip;
                    pathView.label.tooltip = parentTooltip + "\n\n" + pathTooltip;
                }
            }

            private bool SetErrorMessage(VisualElement target, string errorMessage, Action onClick = null)
            {
                target.AddToClassList("has-error");

                var errorElement = target.Q<Button>(className: "error-point");
                if (errorElement == null)
                {
                    return false;
                }

                errorElement.tooltip = errorMessage;
                errorElement.RemoveFromClassList("has-action");

                if (onClick == null)
                {
                    return true;
                }

                errorElement.AddToClassList("has-action");

                void OnClicked()
                {
                    onClick();
                    errorElement.clicked -= OnClicked;
                }

                errorElement.clicked -= OnClicked;
                errorElement.clicked += OnClicked;
                return true;
            }

            private void ShowBindMenu()
            {
                var menu = _owner.BuildOptionsMenu(Data, 
                    () => _owner.ApplyChanges(Data),
                    () =>
                    {
                        _owner.ApplyChangesSilently(Data);
                        ResetAndRefresh();
                    });

                menu.Show(bindMenu.worldBound.WithWidth(296), startFromSelected: false);
            }

            private void AddConnectingLines()
            {
                if (bindToggle == null)
                {
                    return;
                }

                if (ClassListContains(BindDataExpandedClass))
                {
                    return;
                }

                ConnectLineTo(bindPreview, offset: new Vector2(-2f, 0), fromOffset: new Vector2(-2f, 0));
                ConnectLineTo(targetField);
                
                preModifiers.AddConnectingLines(ConnectLineTo);
                
                ConnectLineTo(readConverter.iconElement, readConverter);
                ConnectLineTo(writeConverter.iconElement, writeConverter);

                modifiers.AddConnectingLines(ConnectLineTo);
            }

            private void ConnectLineTo(VisualElement to, VisualElement owner = null, Vector2 offset = default, Vector2 fromOffset = default)
            {
                if (to == null)
                {
                    return;
                }

                var connectorLine = new ConnectingLine();
                fromOffset += new Vector2(-0.5f, 3);
                connectorLine.SetFrom(bindToggle, ConnectingLine.Position.Center, ConnectingLine.Position.Center,
                    fromOffset);
                connectorLine.SetTo(to, ConnectingLine.Position.Min, ConnectingLine.Position.Center, offset);

                owner ??= to;
                owner.hierarchy.Add(connectorLine);
                owner.WithClass("connecting-line__owner");
            }

            public void Reset()
            {
                _owner._propertyData.Clear();
                _owner._tempLabels.Clear();
                _owner._bindTypes.Clear();
            }

            public void Cleanup()
            {
                var data = _owner.GetDataRaw(_property);
                if (data == null)
                {
                    return;
                }

                if (!data.properties.property.IsAlive())
                {
                    return;
                }
                
                var key = data.properties.property.propertyPath;
                _owner._propertyData.Remove(key);
                _owner._tempLabels.Remove(key);
                _owner._bindTypes.Remove(key);
                data.initialized = false;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!_initialized)
            {
                _initialized = true;
                Initialize(property);
            }

            RegisterLabel(property, new GUIContent(preferredLabel));
            var view = new BindDataUI(this, property);
            return view;
        }

        private class ParameterWrapper
        {
            public BindDataUI owner;
            public string fieldName;
            public string propertyPath;
            public PropertyField field;

            public PropertyField CreateField(SerializedProperty property)
            {
                field = new PropertyField().EnsureBind(property).WithClass("bind-parameters__property")
                    .OnBind(f => UpdateLabels());
                field.RegisterCallback<SerializedPropertyChangeEvent>(UpdateField);
                return field;
            }

            private void UpdateField(SerializedPropertyChangeEvent evt)
            {
                field.UnregisterCallback<SerializedPropertyChangeEvent>(UpdateField);

                if (field.childCount == 0)
                {
                    var property = owner.Data.serializedObject.FindProperty(propertyPath);
                    if (property != null)
                    {
                        field.BindProperty(property);
                    }
                }

                field.schedule.Execute(UpdateLabels).ExecuteLater(20);
            }

            private void UpdateLabels()
            {
                field.Query<Label>(null, "bind-field__label").ForEach(SetFieldName);
            }

            private void SetFieldName(VisualElement v)
            {
                if (v == null)
                {
                    return;
                }

                if (v.QueryParent(null, "bs-bind") != field[0])
                {
                    return;
                }

                switch (v)
                {
                    case Label label:
                        label.text = fieldName;
                        break;
                    case Toggle t:
                        t.text = fieldName;
                        break;
                    case TextElement te:
                        te.text = fieldName;
                        break;
                }
            }
        }

        private class CommonTargetView : VisualElement
        {
            public delegate bool ReplaceTargetDelegate(Object target, bool forced);

            public Foldout foldout;
            public Label description;
            public Label smartZone;
            public Label forcedZone;

            private List<ReplaceTargetDelegate> _onReplaceCallbacks = new();

            public event ReplaceTargetDelegate OnTargetReplace
            {
                add
                {
                    if (value == null)
                    {
                        return;
                    }

                    if (_onReplaceCallbacks.Contains(value)) return;

                    _onReplaceCallbacks.Add(value);
                    UpdateState();
                }
                remove
                {
                    if (_onReplaceCallbacks.Remove(value))
                    {
                        UpdateState();
                    }
                }
            }

            public CommonTargetView(Foldout foldout)
            {
                this.AddBSStyle();
                this.foldout = foldout;

                this.foldout?.RegisterValueChangedCallback(evt => UpdateState());

                AddToClassList("common-target");
                // this.AddManipulator(new ObjectDNDManipulator(v => Drop(v, smartZone, false), true, null));
                description = new Label("Rebind Fields: ")
                    {
                        tooltip =
                            "Drag and drop an object over the zones to replace bind source in all reachable active bound fields.\n\n" +
                            "SMART".RT().Bold() +
                            " zone will try to replace the bind source only if the type is compatible.\n" +
                            "FORCED".RT().Bold() +
                            " zone will replace the bind source regardless of the type compatibility."
                    }
                    .WithClass("common-target__description");
                smartZone = new Label("SMART")
                {
                    tooltip =
                        "Drag and drop an object over this zone to replace it with bind sources in all reachable active bound fields.\n" +
                        "The bind source will be replaced only <b>if the type is compatible</b>."
                }.WithClass("common-target__zone", "smart");
                smartZone.AddManipulator(new ObjectDNDManipulator(v => Drop(v, smartZone, false), true, null));

                forcedZone = new Label("FORCED")
                {
                    tooltip =
                        "Drag and drop an object over this zone to replace it with bind sources in all reachable active bound fields.\n" +
                        "The bind source will be replaced <b>regardless of the type compatibility</b>."
                }.WithClass("common-target__zone", "forced");
                forcedZone.AddManipulator(new ObjectDNDManipulator(v => Drop(v, forcedZone, true), true, null));

                Add(description);
                Add(smartZone);
                Add(forcedZone);
                Add(new VisualElement(){pickingMode = PickingMode.Position}.WithClass("tooltip-blocker"));
                
                AddToClassList("inactive");
                schedule.Execute(() =>
                    {
                        var isActivated = DragAndDrop.objectReferences?.Length > 0;
                        EnableInClassList("inactive", !isActivated);
                    })
                    .Every(200);
            }

            private void Drop(Object v, Label label, bool forcedReplace)
            {
                if (!v)
                {
                    return;
                }

                if (_onReplaceCallbacks.Count == 0)
                {
                    return;
                }

                var count = 0;
                foreach (var callback in _onReplaceCallbacks)
                {
                    try
                    {
                        if (callback(v, forcedReplace))
                        {
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("BindData: Error while replacing bind source: " + ex.Message);
                    }
                }

                var classToAdd = count == 0 ? "dnd-failed" : "dnd-succeeded";

                label.AddToClassList(classToAdd);
                var prevText = label.text;
                label.text = count + " Rebound";
                label.schedule.Execute(() =>
                    {
                        label.text = prevText;
                        label.RemoveFromClassList(classToAdd);
                    })
                    .ExecuteLater(2000);
            }

            private void UpdateState()
            {
                this.visible = _onReplaceCallbacks.Count > 0 && (foldout == null || foldout.value);
                description.text = _onReplaceCallbacks.Count > 1
                    ? $"Rebind {_onReplaceCallbacks.Count} Fields:"
                    : "Rebind 1 Field:";

                EnableInClassList("invisible", !visible);
            }
        }
    }
}