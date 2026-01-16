using System;
using System.Collections.Generic;
using System.Reflection;
using Postica.Common.Reflection;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public partial class DrawerSystem
    {
        public readonly static EditorGUIProxy EditorGUI = new();
        public readonly static EditorGUIUtilityProxy EditorGUIUtility = new();
        public readonly static GUILayoutUtilityProxy GUILayoutUtility = new();

        public class EditorGUIProxy : ClassProxy<EditorGUI>
        {
            private static readonly FieldWrapper s_PropertyStack = new(nameof(s_PropertyStack));
            private static readonly FieldWrapper<GUIContent> s_MixedValueContent = new(nameof(s_MixedValueContent));
            private static readonly FieldWrapper<GUIContent> s_PrefixLabel = new(nameof(s_PrefixLabel));
            private static readonly FieldWrapper<Rect> s_PrefixTotalRect = new(nameof(s_PrefixTotalRect));
            private static readonly FieldWrapper<Rect> s_PrefixRect = new(nameof(s_PrefixRect));
            private static readonly FieldWrapper<bool> s_HasPrefixLabel = new(nameof(s_HasPrefixLabel));
            private static readonly FieldWrapper<GUIContent> s_PropertyFieldTempContent = new(nameof(s_PropertyFieldTempContent));
            private static readonly MethodWrapper<Rect, SerializedProperty, GUIContent, bool> _DefaultPropertyField = new(nameof(DefaultPropertyField));
            
            private IObservableStack<PropertyGUIData> _propertyStack;
            
            public GUIContent MixedValueContent => Static(s_MixedValueContent);
            
            public object PropertyStack
            {
                get => Static(s_PropertyStack).Value;
                set => Static(s_PropertyStack).Value = value;
            }
            
            public GUIContent PrefixLabel
            {
                get => Static(s_PrefixLabel).Value;
                set => Static(s_PrefixLabel).Value = value;
            }
            
            public Rect PrefixTotalRect
            {
                get => Static(s_PrefixTotalRect).Value;
                set => Static(s_PrefixTotalRect).Value = value;
            }
            
            public Rect PrefixRect
            {
                get => Static(s_PrefixRect).Value;
                set => Static(s_PrefixRect).Value = value;
            }
            
            public bool HasPrefixLabel
            {
                get => Static(s_HasPrefixLabel);
                set => Static(s_HasPrefixLabel).Value = value;
            }
            
            public GUIContent PropertyFieldTempContent
            {
                get => Static(s_PropertyFieldTempContent).Value;
                set => Static(s_PropertyFieldTempContent).Value = value;
            }
            
            public bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
            {
                return Static(_DefaultPropertyField).Call(position, property, label);
            }
            
            public void RegisterPropertyStackCallbacks(Action<PropertyGUIData> onPush, Action<PropertyGUIData> onPop)
            {
                if (_propertyStack == null)
                {
                    var innerType = typeof(PropertyGUIData).GetCustomAttribute<ForAttribute>().Type;
                    var stackType = typeof(ObservableStack<,>).MakeGenericType(typeof(PropertyGUIData), innerType);
                    var stack = Activator.CreateInstance(stackType);
                    
                    PropertyStack = stack;
                    _propertyStack = (IObservableStack<PropertyGUIData>) stack;
                }

                if (onPush != null)
                {
                    _propertyStack.OnPush -= onPush;
                    _propertyStack.OnPush += onPush;
                }

                if (onPop != null)
                {
                    _propertyStack.OnPop -= onPop;
                    _propertyStack.OnPop += onPop;
                }
            }
            
            public void UnregisterPropertyStackCallbacks(Action<PropertyGUIData> onPush, Action<PropertyGUIData> onPop)
            {
                if (_propertyStack == null) return;
                
                if (onPush != null)
                {
                    _propertyStack.OnPush -= onPush;
                }

                if (onPop != null)
                {
                    _propertyStack.OnPop -= onPop;
                }
            }
            
            private interface IObservableStack<T>
            {
                event Action<T> OnPush;
                event Action<T> OnPop;
            }
            
            private class ObservableStack<S, T> : Stack<T>, IObservableStack<S> where S : ClassProxy, ISafeProxy, new()
            {
                public event Action<S> OnPush;
                public event Action<S> OnPop;

                private S ClassProxy { get; } = new S();
                
                public new void Push(T item)
                {
                    base.Push(item);
                    if(OnPush == null) return;
                    
                    ClassProxy.SetInstance(item);
                    OnPush(ClassProxy);
                }

                public new T Pop()
                {
                    var item = base.Pop();
                    if(OnPop == null) return item;
                    
                    ClassProxy.SetInstance(item);
                    OnPop.Invoke(ClassProxy);
                    return item;
                }
            }
        }
        
        public class EditorGUIUtilityProxy : ClassProxy<EditorGUIUtility>
        {
            private static readonly FieldWrapper<int> s_LastControlID = new(nameof(s_LastControlID));
            
            public int LastControlID
            {
                get => Static(s_LastControlID);
                set => Static(s_LastControlID).Value = value;
            }
        }
        
        public class GUILayoutUtilityProxy : ClassProxy<GUILayoutUtility>
        {
            private static readonly FieldWrapper<GUIStyle> s_SpaceStyle = new(nameof(s_SpaceStyle));
            private static readonly FieldWrapper<Rect> kDummyRect = new(nameof(kDummyRect));
            private static readonly FieldWrapper<LayoutCache> _current = new(nameof(current));
            
            public GUIStyle SpaceStyle
            {
                get => Static(s_SpaceStyle);
                set => Static(s_SpaceStyle).Value = value;
            }
            
            public Rect DummyRect
            {
                get => Static(kDummyRect);
                set => Static(kDummyRect).Value = value;
            }
            
            public LayoutCache current
            {
                get => Static(_current);
                set => Static(_current).Value = value;
            }

            [For(typeof(GUILayout), "UnityEngine.GUILayoutUtility+LayoutCache")]
            public class LayoutCache : ClassProxy
            {
                private static readonly FieldWrapper<GUILayoutGroup> _topLevel = new(nameof(topLevel));
                
                public GUILayoutGroup topLevel
                {
                    get => This(_topLevel);
                    set => This(_topLevel).Value = value;
                }
            }
            
            public void SetLastLayoutRect(Rect rect)
            {
                current.topLevel.entries[^1].rect = rect;
            }
            
            public GUILayoutEntry GetLastLayoutEntry()
            {
                var currentCache = current;
                var currentGroup = currentCache.topLevel;
                var currentEntries = currentGroup.entries;
                var currentEntry = currentEntries[^1];
                return currentEntry;
            }
        }

        [For(typeof(GUILayout), "UnityEngine.GUILayoutEntry")]
        public class GUILayoutEntry : ClassProxy
        {
            private static readonly FieldWrapper<float> _minWidth = new(nameof(minWidth));
            private static readonly FieldWrapper<float> _maxWidth = new(nameof(maxWidth));
            private static readonly FieldWrapper<float> _minHeight = new(nameof(minHeight));
            private static readonly FieldWrapper<float> _maxHeight = new(nameof(maxHeight));
            private static readonly FieldWrapper<Rect> _rect = new(nameof(rect));
            
            public float minWidth
            {
                get => This(_minWidth);
                set => This(_minWidth).Value = value;
            }
            
            public float maxWidth
            {
                get => This(_maxWidth);
                set => This(_maxWidth).Value = value;
            }
            
            public float minHeight
            {
                get => This(_minHeight);
                set => This(_minHeight).Value = value;
            }
            
            public float maxHeight
            {
                get => This(_maxHeight);
                set => This(_maxHeight).Value = value;
            }
            
            public Rect rect
            {
                get => This(_rect);
                set => This(_rect).Value = value;
            }
        }

        [For(typeof(GUILayout), "UnityEngine.GUILayoutGroup")]
        public class GUILayoutGroup : ClassProxy
        {
            private static readonly FieldWrapper<ListProxy<GUILayoutEntry>> _entries = new(nameof(entries));
            
            public ListProxy<GUILayoutEntry> entries
            {
                get => This(_entries);
                set => This(_entries).Value = value;
            }
        }
        
        [For(typeof(Editor), "UnityEditor.PropertyGUIData")]
        public class PropertyGUIData : ClassProxy, ClassProxy.ISafeProxy
        {
            private static readonly FieldWrapper<SerializedProperty> _property = new(nameof(property));
            private static readonly FieldWrapper<Rect> _totalPosition = new(nameof(totalPosition));
            private static readonly FieldWrapper<bool> _wasBoldDefaultFont = new(nameof(wasBoldDefaultFont));
            private static readonly FieldWrapper<bool> _wasEnabled = new(nameof(wasEnabled));
            private static readonly FieldWrapper<Color> _color = new(nameof(color));
            
            public void SetInstance(object instance)
            {
                Instance = instance;
            }
            
            public SerializedProperty property
            {
                get => This(_property);
                set => This(_property).Value = value;
            }
            
            public Rect totalPosition
            {
                get => This(_totalPosition);
                set => This(_totalPosition).Value = value;
            }
            
            public bool wasBoldDefaultFont
            {
                get => This(_wasBoldDefaultFont);
                set => This(_wasBoldDefaultFont).Value = value;
            }
            
            public bool wasEnabled
            {
                get => This(_wasEnabled);
                set => This(_wasEnabled).Value = value;
            }
            
            public Color color
            {
                get => This(_color);
                set => This(_color).Value = value;
            }
        }
    }
}