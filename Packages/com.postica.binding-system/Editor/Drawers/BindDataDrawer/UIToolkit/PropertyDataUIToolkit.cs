using System;
using System.Linq;
using Postica.BindingSystem.Accessors;
using UnityEditor;
using UnityEngine;
using Postica.Common;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial class PropertyData
        {
            public VisualElement previewView;
        }

        partial void UpdatePreviewUIToolkit(PropertyData data)
        {
            var source = data.properties.target.objectReferenceValue;
            data.previewDraw = null;
            data.getPreviewHeight = null;
            data.canPathPreview = false;
            data.pathPreviewIsEditing = false;
            data.previewView?.RemoveFromHierarchy();

            if (!source)
            {
                return;
            }
            
            var path = data.properties.path.stringValue;
            // Prepare path, convert / to . and if the path ends with ] then remove the last part
            path = path.Replace('/', '.');
            if (path.EndsWith("]"))
            {
                var lastDot = path.LastIndexOf('.');
                if (lastDot != -1)
                {
                    path = path.Substring(0, lastDot);
                }
            }

            if(OnTryPrepareDataPreview != null)
            {
                try
                {
                    if(OnTryPrepareDataPreview(data, source, path))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            var serObject = new SerializedObject(source);
#if BS_DEBUG
            Debug.Log($"{BindSystem.DebugPrefix}PREVIEW: Trying to find property {path} in {source}");
#endif
            if(AccessorsFactory.IsRegisteredAccessor(source.GetType(), path) || !serObject.TryFindLastProperty(path, out var prop))
            {
                serObject.Dispose();
                var parameters = data.parameters.parameters?.Select(p => p.Value).ToArray();
                if(!TryGetAccessor(source, path, parameters, data.properties.mainParameterIndex.intValue, out var accessor) || accessor == null)
                {
                    return;
                }

                bool TryGetAccessor(Object source, string path, object[] parameters, int mainParamIndex, out IAccessor accessor)
                {
                    try
                    {
                        accessor = AccessorsFactory.GetAccessor(source, path, parameters, mainParamIndex);
                        return true;
                    }
                    catch
                    {
                        accessor = null;
                        return false;
                    }
                }

                var propertyName = path.Substring(path.LastIndexOf('.') + 1).NiceName();
                data.pathPreviewCanEdit = accessor.CanWrite;
                data.pathPreviewIsEditing = false;
                try
                {
                    if(BindingSettings.Current.PerformanceMode)
                    {
                        var value = accessor.GetValue(source);
                        data.previewView = accessor.ValueType switch
                        {
                            { } t when t == typeof(bool) => UIField<Toggle, bool>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(byte) => UIField<IntegerField, int>(propertyName, value, v => accessor.SetValue(source, (byte)v)),
                            { } t when t == typeof(short) => UIField<IntegerField, int>(propertyName, value, v => accessor.SetValue(source, (short)v)),
                            { } t when t == typeof(int) => UIField<IntegerField, int>(propertyName, value, v => accessor.SetValue(source, (int)v)),
                            { } t when t == typeof(long) => UIField<LongField, long>(propertyName, value, v => accessor.SetValue(source, (long)v)),
                            { } t when t == typeof(float) => UIField<FloatField, float>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(double) => UIField<DoubleField, double>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(char) => UIField<TextField, string>(propertyName, value?.ToString(), v => accessor.SetValue(source, v)),
                            { } t when t == typeof(string) => UIField<TextField, string>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector2) => UIField<Vector2Field, Vector2>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector3) => UIField<Vector3Field, Vector3>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector4) => UIField<Vector4Field, Vector4>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector2Int) => UIField<Vector2IntField, Vector2Int>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector3Int) => UIField<Vector3IntField, Vector3Int>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Quaternion) => UIField<Vector4Field, Vector4>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Bounds) => UIField<BoundsField, Bounds>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Rect) => UIField<RectField, Rect>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(RectInt) => UIField<RectIntField, RectInt>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Color) => UIField<ColorField, Color>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Gradient) => UIField<GradientField, Gradient>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(AnimationCurve) => UIField<CurveField, AnimationCurve>(propertyName, value, v => accessor.SetValue(source, v)),
                            { } t when typeof(Object).IsAssignableFrom(t) => UIField(accessor.ValueType, propertyName, (Object)value, v => accessor.SetValue(source, v)),
                            
                            _ => null
                        };
                    }
                    else
                    {
                        Func<object> valueGetter = () => accessor.GetValue(source);
                        data.previewView = accessor.ValueType switch
                        {
                            { } t when t == typeof(bool) => UIField<Toggle, bool>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(byte) => UIField<IntegerField, int>(propertyName, valueGetter, v => accessor.SetValue(source, (byte)v)),
                            { } t when t == typeof(short) => UIField<IntegerField, int>(propertyName, valueGetter, v => accessor.SetValue(source, (short)v)),
                            { } t when t == typeof(int) => UIField<IntegerField, int>(propertyName, valueGetter, v => accessor.SetValue(source, (int)v)),
                            { } t when t == typeof(long) => UIField<LongField, long>(propertyName, valueGetter, v => accessor.SetValue(source, (long)v)),
                            { } t when t == typeof(float) => UIField<FloatField, float>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(double) => UIField<DoubleField, double>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(char) => UIField<TextField, string>(propertyName, valueGetter?.ToString(), v => accessor.SetValue(source, v)),
                            { } t when t == typeof(string) => UIField<TextField, string>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector2) => UIField<Vector2Field, Vector2>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector3) => UIField<Vector3Field, Vector3>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector4) => UIField<Vector4Field, Vector4>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector2Int) => UIField<Vector2IntField, Vector2Int>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Vector3Int) => UIField<Vector3IntField, Vector3Int>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Quaternion) => UIField<Vector4Field, Vector4>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Bounds) => UIField<BoundsField, Bounds>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Rect) => UIField<RectField, Rect>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(RectInt) => UIField<RectIntField, RectInt>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Color) => UIField<ColorField, Color>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(Gradient) => UIField<GradientField, Gradient>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when t == typeof(AnimationCurve) => UIField<CurveField, AnimationCurve>(propertyName, valueGetter, v => accessor.SetValue(source, v)),
                            { } t when typeof(Object).IsAssignableFrom(t) => UIField(accessor.ValueType, propertyName, () => (Object)accessor.GetValue(source), v => accessor.SetValue(source, v)),
                            
                            _ => null
                        };
                    }

                    data.canPathPreview = data.previewView != null;
                }
                catch (Exception e)
                {
                    data.previewView = new Label($"Preview Error: {e.Message}").WithClass("preview-ui__error");
                }

                return;
            }
            
            prop.isExpanded = true;

            var propField = new PropertyField(prop).EnsureBind(serObject);
            data.previewView = propField;
            data.pathPreviewCanEdit = true;
            data.pathPreviewIsEditing = false;

            data.canPathPreview = true;
        }
        
        private static T UIField<T, V>(string label, object firstValue, Action<V> action) where T : BaseField<V>, new()
        {
            var field = new T
            {
                label = label,
                value = (V)firstValue
            };
            field.RegisterValueChangedCallback(evt => action(evt.newValue));
            return field;
        }
        
        private static ObjectField UIField(Type type, string label, Object firstValue, Action<Object> action)
        {
            var field = new ObjectField(label)
            {
                objectType = type,
                value = firstValue
            };
            field.RegisterValueChangedCallback(evt => action(evt.newValue));
            return field;
        }
        
        private static T UIField<T, V>(string label, Func<object> valueGetter, Action<V> action) where T : BaseField<V>, new()
        {
            var field = new T
            {
                label = label,
                value = (V)valueGetter()
            };
            field.schedule.Execute(() => field.SetValueWithoutNotify((V)valueGetter())).Every(100);
            field.RegisterValueChangedCallback(evt => action(evt.newValue));
            return field;
        }
        
        private static ObjectField UIField(Type type, string label, Func<Object> valueGetter, Action<Object> action)
        {
            var field = new ObjectField(label)
            {
                objectType = type,
                value = valueGetter()
            };
            field.schedule.Execute(() => field.SetValueWithoutNotify(valueGetter())).Every(100);
            field.RegisterValueChangedCallback(evt => action(evt.newValue));
            return field;
        }

        public class BindPreviewUI : VisualElement
        {
            private VisualElement _preview;
            private Toggle _editToggle;
            private Toggle _applyToggle;
            private Action<bool> _onApplyChanged;

            public VisualElement Preview
            {
                get => _preview;
                set
                {
                    if (_preview == value)
                    {
                        return;
                    }
                    
                    _editToggle.value = false;
                    _preview?.RemoveFromHierarchy();
                    _preview?.RemoveFromClassList("preview-ui__preview");
                    
                    _preview = value;
                    _preview?.AddToClassList("preview-ui__preview");
                    _preview?.SetEnabled(false);
                    
                    Add(_preview);
                    
                    EnableInClassList("hidden", _preview == null);
                }
            }

            public bool CanEdit
            {
                get => _editToggle.IsDisplayed();
                set => _editToggle.EnableInClassList("hidden", !value);
            }
            
            public bool CanApply
            {
                get => _applyToggle.value;
                set => _applyToggle.value = value;
            }
            
            public BindPreviewUI(Action<bool> onApplyChanged)
            {
                _onApplyChanged = onApplyChanged;
                
                AddToClassList("preview-ui");
                var label = new Label("VALUE PREVIEW").WithClass("preview-ui__label");
                
                _editToggle = new Toggle("EDIT")
                {
                    focusable = false,
                    value = false,
                    tooltip = "Enable editing of the preview"
                }.WithClass("preview-ui__toggle", "preview-ui__toggle--edit");
                
                _editToggle.RegisterValueChangedCallback(evt =>
                {
                    Preview?.SetEnabled(evt.newValue);
                    EnableInClassList("editing", evt.newValue);
                });
                
                _applyToggle = new Toggle("APPLY NOW")
                {
                    focusable = false,
                    value = false,
                    tooltip = "Apply the changes to the field immediately"
                }.WithClass("preview-ui__toggle", "preview-ui__toggle--apply");

                if (_onApplyChanged != null)
                {
                    _applyToggle.RegisterValueChangedCallback(evt => { _onApplyChanged?.Invoke(evt.newValue); });
                }
                else
                {
                    _applyToggle.AddToClassList("hidden");
                }

                Add(new VisualElement().WithClass("preview-ui__header").WithChildren(label, _applyToggle, _editToggle));
                EnableInClassList("hidden", true);
            }
        }
    }
}