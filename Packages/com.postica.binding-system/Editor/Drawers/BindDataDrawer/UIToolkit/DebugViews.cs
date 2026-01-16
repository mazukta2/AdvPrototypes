using Postica.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Postica.BindingSystem.Accessors;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using PopupWindow = Postica.Common.PopupWindow;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal class DebugViews
        {
            public const float UpdateFrequencyHz = 60f;
            public const int UpdateIntervalMs = (int)(1000 / UpdateFrequencyHz);
            
            
            private List<DebugView> _readViews = new();
            private List<DebugView> _writeViews = new();
            private List<VisualElement> _containers = new();
            private VisualElement _rootContainer;
            private PropertyData _data;
            private float _nextUpdateTime;
            
            private IVisualElementScheduledItem _updateItem;

            public PropertyData Data
            {
                get => _data;
                set
                {
                    if(_data == value)
                    {
                        return;
                    }

                    _data = value;
                }
            }

            public void Rebuild(PropertyData data, VisualElement root)
            {
                Clear();
                Data = data;
                _rootContainer = root;
                if (!_data.properties.property.IsAlive())
                {
                    return;
                }
                var bindMode = _data.properties.BindMode;
                // Add the container for pathview
                var pathContainer = CreateContainer(null, "path-debug").WithClass("first");
                _containers.Add(pathContainer);

                var containerInsertIndex = 2; // Right after path and target

                var pathView = root.Q(null, "bind-data__path");
                var pathViewIndex = pathView.parent.IndexOf(pathView);
                pathView.parent.Insert(containerInsertIndex, pathContainer);

                var accessor = _data.bindDataDebug.GetRawAccessor();
                if (accessor == null)
                {
                    var badPart = AddPart(pathContainer, "input", true);
                    var exceptionView = DebugView.GetExceptionView(new NullReferenceException("Accessor is null"), badPart);
                    badPart.Add(exceptionView);
                    return;
                }
                
                if (bindMode.CanRead())
                {
                    var readPart = AddPart(pathContainer, "input", true);
                    var pathDebugView = CreateGenericDebugView(_data.bindType, accessor);
                    pathDebugView.container = readPart;
                    pathDebugView.isValid = false;
                    _readViews.Add(pathDebugView);
                }

                var nextWriteContainer = pathContainer;
                VisualElement lastWritePart = null;
                VisualElement lastReadPart = pathContainer;
                VisualElement lastContainer = pathContainer;
                
                // Add the container for modifiers
                lastReadPart = DebugModifiers(accessor.ValueType, 
                    _data.preModifiers.array,
                    bindMode,
                    lastReadPart,
                    lastWritePart,
                    ref lastContainer,
                    ref nextWriteContainer);

                // Add the container for converter
                var converterContainer = CreateContainer(null, "converter-debug");
                containerInsertIndex += _data.preModifiers.array?.Length > 0 ? 3 : 2;

                if (data.readConverter.instance != null && bindMode.CanRead())
                {
                    _containers.Add(converterContainer);

                    root.Insert(containerInsertIndex, converterContainer);

                    var part = AddPart(converterContainer, "converted", true);
                    lastReadPart = part;
                    lastContainer = converterContainer;

                    var converterDebugView = CreateConverterDebugView(
                        accessor.ValueType,
                        data.bindType,
                        data.readConverter.instance);
                    converterDebugView.container = part;
                    converterDebugView.isValid = false;
                    _readViews.Add(converterDebugView);
                }

                // Repeat for write converter
                if (data.writeConverter.instance != null && bindMode.CanWrite())
                {
                    if (!_containers.Contains(converterContainer))
                    {
                        _containers.Add(converterContainer);
                    }

                    root.Insert(containerInsertIndex, converterContainer);

                    var part = AddPart(nextWriteContainer, "converted", false);
                    nextWriteContainer = converterContainer;
                    lastWritePart = part;
                    lastContainer = converterContainer;
                    Type toType = null;
                    try
                    {
                        toType = AccessorsFactory
                            .GetMemberAtPath(data.sourceTarget.GetType(), data.properties.path.stringValue)
                            ?.GetMemberType();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    var converterDebugView = CreateConverterDebugView(_data.bindType, accessor.ValueType,
                        _data.writeConverter.instance);
                    converterDebugView.container = part;
                    converterDebugView.isValid = false;
                    _writeViews.Insert(0, converterDebugView);
                }

                // Add the container for modifiers
                lastReadPart = DebugModifiers(_data.bindType, 
                    _data.modifiers.array,
                    bindMode,
                    lastReadPart,
                    lastWritePart,
                    ref lastContainer,
                    ref nextWriteContainer);

                if (bindMode.CanWrite())
                {
                    var writePart = AddPart(nextWriteContainer, "input", false);
                    var pathDebugView = CreateGenericDebugView(_data.bindType, v => _data.bindDataDebug?.DebugValue);
                    pathDebugView.container = writePart;
                    pathDebugView.isValid = false;
                    _writeViews.Insert(0, pathDebugView);
                }

                lastReadPart.Q<Label>(null, "debug-container__part__label").text = "output";

                lastContainer.WithClass("last");

                EditorApplication.update -= Update;

                if (BindingSettings.Current.RealtimeDebug)
                {
                    _updateItem = lastContainer.schedule.Execute(Update).Every(UpdateIntervalMs);
                }
                else
                {
                    EditorApplication.update += Update;
                }

                pathContainer.RegisterCallback<DetachFromPanelEvent>(evt =>
                {
                    EditorApplication.update -= Update;
                    _updateItem?.Pause();
                });
            }

            private VisualElement DebugModifiers(Type type, ModifierData[] modifiersArray, BindMode bindMode, VisualElement lastReadPart, VisualElement lastWritePart,
                ref VisualElement lastContainer, ref VisualElement nextWriteContainer)
            {
                if(modifiersArray == null) 
                {
                    return lastReadPart;
                }
                
                for (int i = 0; i < modifiersArray.Length; i++)
                {
                    ref var modifier = ref modifiersArray[i];
                    if (!modifier.BindMode.IsCompatibleWith(bindMode))
                    {
                        continue;
                    }

                    var view = modifier.containerView;

                    if(view == null)
                    {
                        continue;
                    }

                    // First remove any containers
                    view.Query(null, "debug-container").ForEach(v => v.RemoveFromHierarchy());

                    var modifierContainer = CreateContainer(null, "modifier-debug-" + i);
                    _containers.Add(modifierContainer);
                    view.hierarchy.Add(modifierContainer);

                    if (modifier.BindMode.CanRead() && bindMode.CanRead())
                    {
                        var readPart = AddPart(modifierContainer, "mod-" + i, true);
                        lastReadPart = readPart;
                        lastContainer = modifierContainer;
                        var debugView = CreateGenericDebugView(type, modifier.readFunc);
                        debugView.container = readPart;
                        debugView.isValid = false;
                        _readViews.Add(debugView);
                    }
                    if (modifier.BindMode.CanWrite() && bindMode.CanWrite())
                    {
                        var label = "mod-" + i;
                        if (lastWritePart != null)
                        {
                            lastReadPart.Q<Label>(null, "debug-container__part__label").text = label;
                        }
                        else
                        {
                            label = "output";
                        }
                        var writePart = AddPart(nextWriteContainer, label, false);
                        nextWriteContainer = modifierContainer;
                        lastWritePart = writePart;
                        lastContainer = modifierContainer;

                        var debugView = CreateGenericDebugView(type, modifier.writeFunc);
                        debugView.container = writePart;
                        debugView.isValid = false;
                        _writeViews.Insert(0, debugView);
                    }
                }

                return lastReadPart;
            }

            public void Clear()
            {
                if(_containers.Count == 0)
                {
                    return;
                }

                EditorApplication.update -= Update;
                _updateItem?.Pause();
                _updateItem = null;

                foreach (var view in _readViews)
                {
                    view?.Clear();
                }

                foreach(var view in _writeViews)
                {
                    view?.Clear();
                }

                foreach (var container in _containers)
                {
                    container?.RemoveFromHierarchy();
                }

                _readViews.Clear();
                _writeViews.Clear();
                _containers.Clear();
            }

            public void Update()
            {
                if (_data == null)
                {
                    return;
                }
                
                if(!_data.properties.mode.IsAlive())
                {
                    return;
                }
                
                // Get update interval
                var updateInterval = _data.properties.updateInterval?.floatValue ?? 1;
                var isTimeInterval = (_data.properties.flags.enumValueFlag & (int)BindData.BitFlags.UseTimeUpdateInterval) != 0;

                if (!isTimeInterval && updateInterval == 0)
                {
                    updateInterval = 1;
                }
                
                if (!isTimeInterval && (Time.frameCount % updateInterval) != 0)
                {
                    return;
                }
                
                if (isTimeInterval && Time.unscaledTime < _nextUpdateTime)
                {
                    return;
                }
                
                _nextUpdateTime = Time.unscaledTime + updateInterval;
                
                if (_data.properties.BindMode.CanRead())
                {
                    UpdateValues(null, _readViews);
                }

                if (_data.properties.BindMode.CanWrite())
                {
                    var bindData = _data.properties.property.GetValue() as IBindDataDebug;
                    UpdateValues(bindData?.DebugValue, _writeViews);
                }
            }

            private VisualElement CreateContainer(string label, string classname)
            {
                var container = new VisualElement().WithClass("debug-container");
                if (!string.IsNullOrEmpty(classname))
                {
                    container.AddToClassList(classname);
                }
                if (!string.IsNullOrEmpty(label))
                {
                    container.Add(new Label(label).WithClass("debug-container__label"));
                }
                return container;
            }

            private VisualElement AddPart(VisualElement container, string label, bool isRead)
            {
                var classname = isRead ? "read" : "write";
                var subContainer = new VisualElement().WithClass("debug-container__part").WithClass(classname)
                                        .WithChildren(new Image().WithClass("debug-container__part__icon").WithClass(classname));
                container.Add(subContainer);

                if (!string.IsNullOrEmpty(label))
                {
                    subContainer.Insert(0, new Label(label).WithClass("debug-container__part__label"));
                }

                subContainer.RegisterCallback<MouseEnterEvent>(evt => HoverAllViews(true, isRead));
                subContainer.RegisterCallback<MouseLeaveEvent>(evt => HoverAllViews(false, isRead));
                return subContainer;
            }

            private void HoverAllViews(bool hover, bool isRead)
            {
                var list = isRead ? _readViews : _writeViews;
                foreach(var view in list)
                {
                    view.container.EnableInClassList("highlight", hover);
                }
            }

            private void UpdateValues(object startValue, List<DebugView> views)
            {
                if (!_data.properties.mode.IsAlive())
                {
                    return;
                }
                
                // if (!_data.properties.BindMode.CanRead())
                // {
                //     return;
                // }

                object value = startValue;

                for (int i = 0; i < views.Count; i++)
                {
                    var view = views[i];
                    if(!view.TryApplyValue(value, out value))
                    {
                        for (int j = i + 1; j < views.Count; j++)
                        {
                            views[j].isValid = false;
                        }
                    }
                }
            }

            private string BuildSourceInfo(IBindDataDebug bindDebug)
            {
                return $"{bindDebug.Source}.{Accessors.AccessorPath.CleanPath(bindDebug.Path.Replace("Array.data", ""))}";
            }

            private static VisualElement CreateExceptionInfo(Exception ex, float paddingShift = 0)
            {
                var container = new VisualElement();
                container.style.flexGrow = 1;
                
                const float padding = 8;
                container.style.paddingBottom = padding;
                container.style.paddingTop = padding;
                container.style.paddingLeft = padding + paddingShift;
                container.style.paddingRight = padding;

                const float border = 1;
                container.style.borderBottomWidth = border;
                container.style.borderTopWidth = border;
                container.style.borderLeftWidth = border;
                container.style.borderRightWidth = border;

                var borderColor = Color.red;
                container.style.borderBottomColor = borderColor;
                container.style.borderTopColor = borderColor;
                container.style.borderLeftColor = borderColor;
                container.style.borderRightColor = borderColor;

                var type = new Label(ex.GetType().Name);
                type.style.fontSize = 12;
                type.style.color = Color.red;
                type.style.unityFontStyleAndWeight = FontStyle.Bold;

                if(ex.InnerException != null)
                {
                    container.Add(CreateExceptionInfo(ex.InnerException, 8));
                    return container;
                }

                var message = new Label(ex.Message);
                message.style.fontSize = 11;
                message.style.color = Color.red.Green(0.4f).Blue(0.4f);
                message.style.whiteSpace = WhiteSpace.Normal;

                var source = new Label("Source: " + ex.Source);
                source.style.fontSize = 9;
                source.style.whiteSpace = WhiteSpace.Normal;

                var sb = new StringBuilder();
                var method = ex.TargetSite;
                if(method != null)
                {
                    sb.AppendLine($"{method.ReflectedType.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                }

                sb.AppendLine().Append(ex.StackTrace);

                var targetSite = new ScrollView();
                targetSite.style.marginTop = 12;

                var stackTrace = new Label("[For Devs]: " + sb);
                stackTrace.style.fontSize = 10;
                stackTrace.style.whiteSpace = WhiteSpace.Normal;
                
                targetSite.Add(stackTrace);

                container.Add(type);
                container.Add(message); 
                container.Add(source);
                container.Add(targetSite);
                return container;
            }
            
            private DebugView CreateGenericDebugView(Type valueType, IAccessor accessor)
            {
                Func<object, object> readFunc = _ => accessor.GetValue(_data.sourceTarget);
                Type debugViewType = null;
                if (valueType != accessor.ValueType
                    && typeof(IValueProvider).IsAssignableFrom(accessor.ValueType)
                    && accessor.ValueType.TryGetGenericArguments(typeof(IValueProvider<>), out var genArgs)
                    && genArgs.Length == 1
                    && genArgs[0] == valueType)
                {
                    debugViewType = typeof(GenericDebugView<>).MakeGenericType(valueType);
                }
                else
                {
                    debugViewType = typeof(GenericDebugView<>).MakeGenericType(accessor.ValueType);
                }
                var debugView = Activator.CreateInstance(debugViewType, readFunc) as DebugView;
                return debugView;
            }
            
            private static DebugView CreateGenericDebugView(Type valueType, Func<object, object> readFunc)
            {
                var debugViewType = typeof(GenericDebugView<>).MakeGenericType(valueType);
                var debugView = Activator.CreateInstance(debugViewType, readFunc) as DebugView;
                return debugView;
            }
            
            private static DebugView CreateConverterDebugView(Type fromType, Type toType, IConverter converter)
            {
                var debugViewType = typeof(ConverterDebugView<,>).MakeGenericType(fromType, toType);
                var debugView = Activator.CreateInstance(debugViewType, converter) as DebugView;
                return debugView;
            }

            private sealed class ConverterDebugView<TFrom, TTo> : DebugView<TTo>
            {
                public IConverter converter;

                private Func<object, object> _getValueFromProviderFunc;
                
                public ConverterDebugView(IConverter converter)
                {
                    this.converter = converter;
                }

                protected override object ApplyValue(object value)
                {
                    try
                    {
                        if (value is IValueProvider<TFrom> valueProvider)
                        {
                            return valueProvider.Value;
                        }

                        return converter.Convert(value);
                    }
                    catch (InvalidCastException ex)
                    {
                        return ex;
                    }
                }
            }

            private sealed class GenericDebugView<T> : DebugView<T>
            {
                private readonly Func<object, object> _readFunc;

                protected override object ApplyValue(object value) => _readFunc(value);
                
                public GenericDebugView(Delegate readFunc)
                {
                    _readFunc = (Func<object, object>)readFunc;
                }
            }

            private abstract class DebugView
            {
                public VisualElement container;
                protected VisualElement field;

                public bool isValid
                {
                    get => field?.ClassListContains("invalid") == false;
                    set
                    {
                        if(field == null)
                        {
                            field = new Label("Undefined").WithClass("debug-view").WithClass("undefined");
                            container.Add(field);
                        }
                        container.EnableInClassList("invalid", !value);
                    }
                }

                protected abstract object ApplyValue(object value);

                public abstract bool TryApplyValue(object input, out object output);
                
                protected S GetBaseField<S, T>(T value) where S : BaseField<T>, new()
                {
                    if (field is not S)
                    {
                        field?.RemoveFromHierarchy();
                        field = new S() { focusable = true }.WithClass("debug-view");
                    }

                    var sfield = (S)field;
                    sfield.value = value;
                    return sfield;
                }

                protected EnumField GetEnumField(Enum value)
                {
                    if(field is EnumField enumField)
                    {
                        enumField.value = value;
                        return enumField;
                    }
                    
                    field?.RemoveFromHierarchy();
                    var sfield = new EnumField(value) { focusable = true }.WithClass("debug-view");

                    field = sfield;
                    return sfield;
                }
                
                public static VisualElement GetExceptionView(Exception ex, VisualElement container)
                {
                    var button = new Button().WithClass("exception-field");
                        button.clicked += () => PopupWindow.Show(GUIUtility.GUIToScreenRect(container.worldBound),
                            new Vector2(Mathf.Max(300, container.layout.width), 300),
                            CreateExceptionInfo(ex), isDynamicTransform: true);
                    button.text = ex.GetType().Name;
                    return button;
                }

                internal void Clear()
                {
                    field?.Clear();
                }
            }

            private abstract class DebugView<T> : DebugView
            {
                private T _lastValue;
                private Exception _exception;

                public override bool TryApplyValue(object input, out object output)
                {
                    try
                    {
                        output = ApplyValue(input);
                        SetValue(output);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        SetValue(ex);
                        output = null;
                        return false;
                    }
                }

                private void SetValue(object value)
                {
                    if(value is IValueProvider<T> valueProvider)
                    {
                        value = valueProvider.Value;
                    }
                    if(value is not T typedValue)
                    {
                        return;
                    }
                    if(EqualityComparer<T>.Default.Equals(this._lastValue, typedValue) || _exception == value)
                    {
                        return;
                    }

                    if(value is Exception ex)
                    {
                        _exception = ex;
                    }
                    else
                    {
                        _exception = null;
                        this._lastValue = typedValue;
                    }

                    var newField = GetFieldWithValue(typedValue);
                    isValid = true;

                    if (field != newField || field?.parent != container)
                    {
                        field?.RemoveFromHierarchy();
                        field = newField;
                        container.Add(field);
                    }
                }

                
                public VisualElement GetFieldWithValue(T value)
                {
                    if(_exception != null)
                    {
                        if(field is not Button button)
                        {
                            button = new Button().WithClass("exception-field");
                            button.clicked += () => PopupWindow.Show(GUIUtility.GUIToScreenRect(container.worldBound),
                                                 new Vector2(Mathf.Max(300, container.layout.width), 300),
                                                 CreateExceptionInfo(_exception), isDynamicTransform: true);
                        }
                        button.text = _exception.GetType().Name;
                        return button;
                    }
                    
                    return value switch
                    {
                        Color v => GetBaseField<ColorField, Color>(v),
                        Vector2Int v => GetBaseField<Vector2IntField, Vector2Int>(v),
                        Vector3Int v => GetBaseField<Vector3IntField, Vector3Int>(v),
                        Vector2 v => GetBaseField<Vector2Field, Vector2>(v),
                        Vector3 v => GetBaseField<Vector3Field, Vector3>(v),
                        Vector4 v => GetBaseField<Vector4Field, Vector4>(v),
                        Quaternion v => GetBaseField<Vector4Field, Vector4>(v.eulerAngles),
                        Rect v => GetBaseField<RectField, Rect>(v),
                        RectInt v => GetBaseField<RectIntField, RectInt>(v),
                        Bounds v => GetBaseField<BoundsField, Bounds>(v),
                        BoundsInt v => GetBaseField<BoundsIntField, BoundsInt>(v),
                        AnimationCurve v => GetBaseField<CurveField, AnimationCurve>(v),
                        Gradient v => GetBaseField<GradientField, Gradient>(v),
                        LayerMask v => GetBaseField<LayerMaskField, int>(v),
                        Enum v => GetEnumField(v),
                        Object v => GetBaseField<ObjectField, Object>(v),
                        string v => GetBaseField<TextField, string>(v),
                        bool v => GetBaseField<Toggle, bool>(v),
                        int v => GetBaseField<IntegerField, int>(v),
                        float v => GetBaseField<FloatField, float>(v),
                        double v => GetBaseField<DoubleField, double>(v),
                        long v => GetBaseField<LongField, long>(v),
                        ulong v => GetBaseField<LongField, long>((long)v),
                        byte v => GetBaseField<IntegerField, int>(v),
                        sbyte v => GetBaseField<IntegerField, int>(v),
                        short v => GetBaseField<IntegerField, int>(v),
                        ushort v => GetBaseField<IntegerField, int>(v),
                        uint v => GetBaseField<IntegerField, int>((int)v),
                        IntPtr v => GetBaseField<IntegerField, int>((int)v),
                        UIntPtr v => GetBaseField<IntegerField, int>((int)v),
                        char v => GetBaseField<TextField, string>(new string(v, 1)),
                        Color32 v => GetBaseField<ColorField, Color>(v),
                        _ => GetBaseField<TextField, string>(value?.ToString()),
                    };
                }
            }
        }
    }
}