using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    static class VisualElementExtensions
    {
        private static readonly Dictionary<Type, SerializedPropertyGetter> _serializedPropertyGetters = new();
        
        public abstract class SerializedPropertyGetter
        {
            public abstract bool TryGetSerializedProperty(object source, out SerializedProperty property);
            
            public static SerializedPropertyGetter Get(Type type)
            {
                if (_serializedPropertyGetters.TryGetValue(type, out var getter))
                {
                    return getter;
                }

                var property = type.GetProperty("boundProperty", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (property == null)
                {
                    _serializedPropertyGetters[type] = null;
                    return null;
                }

                var getterMethod = property.GetGetMethod(true);
                var getterMethodType = typeof(Func<,>).MakeGenericType(type, typeof(SerializedProperty));
                var getterFunc = Delegate.CreateDelegate(getterMethodType, getterMethod);
                var getterType = typeof(SerializedPropertyGetter<>).MakeGenericType(type);
                var instance = (SerializedPropertyGetter) Activator.CreateInstance(getterType, getterFunc);
                _serializedPropertyGetters[type] = instance;
                return instance;
            }
        }
        
        public class SerializedPropertyGetter<T> : SerializedPropertyGetter
        {
            private readonly Func<T, SerializedProperty> _getter;

            public SerializedPropertyGetter(Func<T, SerializedProperty> getter)
            {
                _getter = getter;
            }

            public override bool TryGetSerializedProperty(object source, out SerializedProperty property)
            {
                if (source is T t)
                {
                    property = _getter(t);
                    return property != null;
                }

                property = null;
                return false;
            }
        }

        public class EventCallbackWrapper<T>
        {
            private TrickleDown _trickleDown;
            private EventCallback<T> _callback;
            private Action<EventCallback<T>, TrickleDown> _unregisterCallback;
            
            public Func<bool> callbackCondition;
            
            public EventCallbackWrapper(EventCallback<T> callback, Action<EventCallback<T>, TrickleDown> unregisterCallback, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            {
                _trickleDown = trickleDown;
                _callback = callback;
                _unregisterCallback = unregisterCallback;
            }
            
            public void Invoke(T evt)
            {
                if (callbackCondition != null && !callbackCondition())
                {
                    return;
                }
                _unregisterCallback?.Invoke(Invoke, _trickleDown);
                _callback?.Invoke(evt);
            }
        }
        
        public readonly struct LayoutState : IDisposable
        {
            public readonly VisualElement element;
            public readonly StyleLength width;
            public readonly StyleLength height;
            public readonly StyleLength left;
            public readonly StyleLength top;
            public readonly StyleLength right;
            public readonly StyleLength bottom;
            public readonly StyleEnum<Position> position;

            internal LayoutState(VisualElement elem)
            {
                element = elem;
                width = elem.style.width;
                height = elem.style.height;
                left = elem.style.left;
                top = elem.style.top;
                right = elem.style.right;
                bottom = elem.style.bottom;
                position = elem.style.position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Restore() => Dispose();

            public void Dispose()
            {
                element.style.width = width;
                element.style.height = height;
                element.style.left = left;
                element.style.top = top;
                element.style.right = right;
                element.style.bottom = bottom;
                element.style.position = position;
            }
        }

        public static LayoutState PushLayout(this VisualElement element) => new LayoutState(element);

        internal static void AddPosticaStyles(this VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.styleSheets.Add(Resources.Load<StyleSheet>("__pstyle"));
            if (!EditorGUIUtility.isProSkin)
            {
                element.styleSheets.Add(Resources.Load<StyleSheet>("__pstyle_lite"));
            }
        }

        public static void SetContent(this TextElement te, GUIContent content)
        {
            te.text = content.text;
            te.tooltip = content.tooltip;
        }

        public static UQueryBuilder<T> Displayed<T>(this UQueryBuilder<T> builder) where T : VisualElement
        {
            return builder.Where(v => v.resolvedStyle.display != DisplayStyle.None);
        }

        public static VisualElement QueryParent(this VisualElement v, string name, string classname)
            => QueryParent<VisualElement>(v, name, classname);

        public static T QueryParent<T>(this VisualElement v, string name = null, string classname = null, int levels = 999) where T : VisualElement
        {
            var parent = v.parent;
            while (parent != null && levels-- > 0)
            {
                if (parent is T t && (string.IsNullOrEmpty(name) || parent.name == name) && (string.IsNullOrEmpty(classname) || parent.ClassListContains(classname)))
                {
                    return t;
                }
                parent = parent.parent;
            }
            return null;
        }
        
        public static T QueryParent<T>(this VisualElement v, Predicate<T> predicate, string name = null, string classname = null) where T : VisualElement
        {
            var parent = v.parent;
            while (parent != null)
            {
                if (parent is T t && predicate(t) && (string.IsNullOrEmpty(name) || parent.name == name) && (string.IsNullOrEmpty(classname) || parent.ClassListContains(classname)))
                {
                    return t;
                }
                parent = parent.parent;
            }
            return null;
        }

        public static T HFind<T>(this VisualElement elem, string name = null, string classname = null) where T : VisualElement
        {
            // Search for an element in hierarchy
            var searchName = !string.IsNullOrEmpty(name);
            var searchClass = !string.IsNullOrEmpty(classname);
            var searchBoth = searchName && searchClass;
            searchName &= !searchBoth;
            searchClass &= !searchBoth;
            foreach(var child in elem.hierarchy.Children())
            {
                if(child is not T tchild)
                {
                    var found = child.HFind<T>(name, classname);
                    if(found != null)
                    {
                        return found;
                    }
                    continue;
                }
                if(searchBoth && child.name == name && child.ClassListContains(classname))
                {
                    return tchild;
                }
                if(searchName && child.name == name)
                {
                    return tchild;
                }
                if(searchClass && child.ClassListContains(classname))
                {
                    return tchild;
                }

                // Recurse into children
                var foundChild = child.HFind<T>(name, classname);
                if(foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }

        public static T MakeAbsolute<T>(this T elem) where T : VisualElement
        {
            if (elem == null)
            {
                return null;
            }

            elem.style.top = elem.resolvedStyle.top;
            elem.style.left = elem.resolvedStyle.left;
            elem.style.width = elem.resolvedStyle.width;
            elem.style.height = elem.resolvedStyle.height;

            elem.style.position = Position.Absolute;

            return elem;
        }

        public static void CopyLayoutFrom(this VisualElement elem, VisualElement other)
        {
            if (elem == null || other == null)
            {
                return;
            }

            elem.style.top = other.resolvedStyle.top;
            elem.style.left = other.resolvedStyle.left;
            elem.style.right = other.resolvedStyle.right;
            elem.style.bottom = other.resolvedStyle.bottom;
            elem.style.width = other.resolvedStyle.width;
            elem.style.height = other.resolvedStyle.height;
        }
        
        public static T MakeReadonly<T, TVal>(this T elem) where T : VisualElement, INotifyValueChanged<TVal>
        {
            if (elem == null)
            {
                return null;
            }
            
            elem.RegisterValueChangedCallback(e =>
            {
                elem.SetValueWithoutNotify(e.previousValue);
                e.StopPropagation();
            });
            
            return elem;
        }

        public static void MoveAbsolutePosition(this VisualElement elem, Vector2 position)
        {
            elem.style.translate = new Translate(position.x, position.y, 0);
        }

        public static void ResetLayoutStyle(this VisualElement elem)
        {
            if (elem == null)
            {
                return;
            }

            elem.style.top = StyleKeyword.Null;
            elem.style.left = StyleKeyword.Null;
            elem.style.right = StyleKeyword.Null;
            elem.style.bottom = StyleKeyword.Null;
            elem.style.width = StyleKeyword.Null;
            elem.style.height = StyleKeyword.Null;
        }

        public static PropertyField EnsureBind(this PropertyField field, SerializedProperty property)
        {
            field.BindProperty(property);
            return field;
        }
        
        public static PropertyField EnsureBind(this PropertyField field, SerializedProperty property, Action<PropertyField> onBind)
        {
            field.BindProperty(property);
            var wrapper =
                new EventCallbackWrapper<SerializedPropertyChangeEvent>(evt => onBind(field), field.UnregisterCallback);
            field.RegisterCallback<SerializedPropertyChangeEvent>(wrapper.Invoke);
            return field;
        }
        
        public static PropertyField EnsureBind(this PropertyField field, SerializedObject obj)
        {
            field.Bind(obj);
            return field;
        }
        
        public static PropertyField OnBind(this PropertyField field, Action<PropertyField> onBind)
        {
            if (field.childCount > 0)
            {
                onBind(field);
                return field;
            }
            var wrapper =
                new EventCallbackWrapper<SerializedPropertyChangeEvent>(evt => onBind(field), field.UnregisterCallback);
            field.RegisterCallback<SerializedPropertyChangeEvent>(wrapper.Invoke);
            return field;
        }
        
        public static PropertyField OnBind(this PropertyField field, Action<PropertyField, SerializedProperty> onBind)
        {
            if(field.TryGetSerializedProperty(out var property))
            {
                onBind(field, property);
                return field;
            }
            
            var wrapper =
                new EventCallbackWrapper<SerializedPropertyChangeEvent>(EventCallback, field.UnregisterCallback);
            field.RegisterCallback<SerializedPropertyChangeEvent>(wrapper.Invoke);
            return field;

            void EventCallback(SerializedPropertyChangeEvent evt)
            {
                if (field.TryGetSerializedProperty(out var property))
                {
                    onBind(field, property);
                }
            }
        }
        
        public static bool TryGetSerializedProperty(this IBindable elem, out SerializedProperty property)
        {
            if (elem.binding == null)
            {
                if (elem is VisualElement visualElement)
                {
                    var inputField = visualElement.Q(null, BaseField<int>.ussClassName);
                    if (inputField is IBindable inputFieldBindable && inputFieldBindable != elem && inputFieldBindable.bindingPath == elem.bindingPath)
                    {
                        return inputFieldBindable.TryGetSerializedProperty(out property);
                    }
                    var foldout = visualElement.Q<Foldout>(null, "unity-foldout");
                    if (foldout != null && foldout != elem && foldout.bindingPath == elem.bindingPath)
                    {
                        return foldout.TryGetSerializedProperty(out property);
                    }
                }

                property = null;
                return false;
            }

            var getter = SerializedPropertyGetter.Get(elem.binding.GetType());
            if (getter == null)
            {
                property = null;
                return false;
            }
            
            return getter.TryGetSerializedProperty(elem.binding, out property);
        }
 
        public static T BindTo<T>(this T elem, UnityEngine.Object target, string path) where T : BindableElement
        {
            if(elem == null || target == null)
            {
                return null;
            }
            elem.bindingPath = path;
            elem.Bind(new SerializedObject(target));
            return elem;
        }

        public static BaseField<TProperty> BindTo<TSource, TProperty>(this BaseField<TProperty> field, TSource source, Expression<Func<TSource, TProperty>> property, bool updateLabel = true)
        {
            // Get the property using Expression
            var propertyInfo = ReflectionEditorExtensions.GetPropertyInfo<TSource, TProperty>(property);
            if (propertyInfo == null)
            {
                return field;
            }
            
            field.RegisterValueChangedCallback(evt => propertyInfo.SetValue(source, evt.newValue));
            field.value = (TProperty) propertyInfo.GetValue(source);
            field.schedule.Execute(() => field.value = (TProperty) propertyInfo.GetValue(source)).Every(100);
            if (updateLabel)
            {
                field.label = propertyInfo.Name.NiceName();
            }

            var tooltipAttribute = propertyInfo.GetCustomAttribute<TooltipAttribute>();
            if (tooltipAttribute != null)
            {
                field.tooltip = tooltipAttribute.tooltip;
            }
            
            return field;
        }
        
        public static T Horizontal<T>(this T elem) where T : VisualElement
        {
            elem.style.flexDirection = FlexDirection.Row;
            return elem;
        }
        
        public static T WithStyle<T>(this T elem, Action<IStyle> style) where T : VisualElement
        {
            style(elem.style);
            return elem;
        }
        
        public static T WhenClicked<T>(this T elem, Action action) where T : VisualElement
        {
            elem.RegisterCallback<ClickEvent>(e =>
            {
                action();
            });
            return elem;
        }

        public static T WithClass<T>(this T elem, string @class) where T : VisualElement
        {
            elem?.AddToClassList(@class);
            return elem;
        }
        
        public static T WithClassDelayed<T>(this T elem, string @class) where T : VisualElement
        {
            elem?.schedule.Execute(() => elem.AddToClassList(@class));
            return elem;
        }
        
        public static T WithClassDelayed<T>(this T elem, string class1, string class2) where T : VisualElement
        {
            elem?.schedule.Execute(() => elem.WithClass(class1, class2));
            return elem;
        }

        public static T WithClass<T>(this T elem, string class1, string class2) where T : VisualElement
        {
            elem?.AddToClassList(class1);
            elem?.AddToClassList(class2);
            return elem;
        }

        public static T WithClass<T>(this T elem, string class1, string class2, string class3) where T : VisualElement
        {
            elem?.AddToClassList(class1);
            elem?.AddToClassList(class2);
            elem?.AddToClassList(class3);
            return elem;
        }

        public static T WithClass<T>(this T elem, string class1, string class2, string class3, string class4) where T : VisualElement
        {
            elem?.AddToClassList(class1);
            elem?.AddToClassList(class2);
            elem?.AddToClassList(class3);
            elem?.AddToClassList(class4);
            return elem;
        }
        
        public static T WithClassEnabled<T>(this T elem, string classname, bool enabled) where T : VisualElement
        {
            if(elem == null) { return null; }
            elem.EnableInClassList(classname, enabled);
            return elem;
        } 

        public static T WithClass<T>(this T elem, params string[] classes) where T : VisualElement
        {
            if(elem == null) { return null; }
            foreach (var @class in classes)
            {
                elem.AddToClassList(@class);
            }
            return elem;
        }
        
        public static T WithoutClassDelayed<T>(this T element, string className) where T : VisualElement
        {
            if (element == null) { return null; }

            element.schedule.Execute(() => element.RemoveFromClassList(className));
            return element;
        }
        
        public static T WithoutClass<T>(this T element, params string[] classNames) where T : VisualElement
        {
            if (element == null) { return null; }
            foreach (var name in classNames)
            {
                element.RemoveFromClassList(name);
            }
            return element;
        }

        public static T RecurseRemoveClass<T>(this T element, string className, bool includeSelf = true) where T : VisualElement
        {
            if (element == null) { return null; }

            if (includeSelf)
            {
                element.RemoveFromClassList(className);
            }

            foreach (var child in element.Children())
            {
                child.RecurseRemoveClass(className, true);
            }
            return element;
        }
        
        public static T RemoveClassFromAncestors<T>(this T element, string className) where T : VisualElement
        {
            if (element == null) { return null; }

            var parent = element.parent;
            while (parent != null)
            {
                parent.RemoveFromClassList(className);
                parent = parent.parent;
            }
            return element;
        }
        
        public static T WithRichText<T>(this T elem) where T : TextElement
        {
            elem.enableRichText = true;
            return elem;
        }
        
        public static int GetDepth(this VisualElement element)
        {
            if (element == null)
            {
                return 0;
            }
            
            int depth = 0;
            while (element.parent != null)
            {
                depth++;
                element = element.parent;
            }
            return depth;
        }

        public static T WithTooltip<T>(this T elem, string tooltip, bool replace = false) where T : VisualElement
        {
            if (elem == null) { return null; }
            if(replace || string.IsNullOrEmpty(elem.tooltip))
            {
                elem.tooltip = tooltip;
            }
            return elem;
        }

        public static T WithChildren<T>(this T elem, params VisualElement[] children) where T : VisualElement
        {
            if (children == null) { return elem; }
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] is IBindable bindable)
                {
                    bindable.binding?.Update();
                }

                try
                {
                    elem.Add(children[i]);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return elem;
        }

        public static void TransferChildrenTo(this VisualElement elem, VisualElement other)
        {
            foreach(var child in elem.Children().ToArray())
            {
                child.RemoveFromHierarchy();
                other.Add(child);
            }
        }

        public static void ApplyTranslation(this VisualElement elem, VisualElement other)
        {
            var tranlation = other.resolvedStyle.translate;
            elem.style.translate = new StyleTranslate(new Translate(tranlation.x, tranlation.y, tranlation.z));
        }

        public static T SetVisibility<T>(this T elem, bool visible) where T : VisualElement
        {
            if (elem != null)
            {
                elem.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            return elem;
        }
        
        public static T VisibleWhen<T>(this T elem, Func<bool> condition, int freqInMs = 100) where T : VisualElement
        {
            if (elem == null)
            {
                return null;
            }

            elem.schedule.Execute(() => elem.style.display = condition() ? DisplayStyle.Flex : DisplayStyle.None)
                .Every(freqInMs);
            
            return elem;
        }

        public static bool IsDisplayed(this VisualElement elem)
        {
            return elem.resolvedStyle.display != DisplayStyle.None && !elem.ClassListContains("hidden") && elem.layout.height > 0 && elem.layout.width > 0;
        }

        public static T DoOnValueChange<T, S>(this T elem, Action<T, S> action) where T : VisualElement, INotifyValueChanged<S>
        {
            elem.RegisterValueChangedCallback((e) => action(elem, e.newValue));
            return elem;
        }

        public static T DoOnValueChange<T, S>(this T elem, Action<S> action) where T : VisualElement, INotifyValueChanged<S>
        {
            elem.RegisterValueChangedCallback((e) => action(e.newValue));
            return elem;
        }
        
        public static PropertyField DoOnValueChange(this PropertyField elem, Action<SerializedProperty> action)
        {
            elem.RegisterValueChangeCallback(e => action(e.changedProperty));
            return elem;
        }
        
        public static T Unpickable<T>(this T element) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

        public static T OnAttachToPanel<T>(this T element, EventCallback<AttachToPanelEvent> action, bool onlyOnce = false) where T : VisualElement
        {
            if (onlyOnce)
            {
                var wrapper = new EventCallbackWrapper<AttachToPanelEvent>(action, element.UnregisterCallback);
                element.RegisterCallback<AttachToPanelEvent>(wrapper.Invoke);
                return element;
            }
            element.RegisterCallback(action);
            return element;
        }

        public static T OnAttachToPanel<T>(this T element, EventCallback<AttachToPanelEvent> action, long delayInMs) where T : VisualElement
        {
            element.RegisterCallback<AttachToPanelEvent>(evt => element.schedule.Execute(() => action(evt)).StartingIn(delayInMs));
            return element;
        }
        
        public static T OnGeometryChanged<T>(this T element, EventCallback<GeometryChangedEvent> action, bool onlyOnce = false) where T : VisualElement
        {
            if (onlyOnce)
            {
                var wrapper = new EventCallbackWrapper<GeometryChangedEvent>(action, element.UnregisterCallback);
                element.RegisterCallback<GeometryChangedEvent>(wrapper.Invoke);
                return element;
            }
            element.RegisterCallback(action);
            return element;
        }

        public static T AlignField<T>(this T field, VisualElement label) where T : VisualElement
        {
            field.AddManipulator(new AlignInInspectorManipulator(label));
            return field;
        }

        public static T AlignField<T>(this T field, Func<bool> condition = null) where T : VisualElement
        {
            field.AddManipulator(new AlignInInspectorManipulator(v => v.Q(null, "unity-base-field__label"), condition));
            return field;
        }

        public static T StyleAsField<T>(this T element, bool enableStyling = true, bool aligned = true) where T : VisualElement
        {
            if (!enableStyling)
            {
                return element.WithoutClass(BaseField<int>.ussClassName, BaseField<int>.alignedFieldUssClassName, "unity-base-field__inspector-field");
            }
            return aligned 
                ? element.WithClass(BaseField<int>.ussClassName, BaseField<int>.alignedFieldUssClassName, "unity-base-field__inspector-field")
                : element.WithClass(BaseField<int>.ussClassName, "unity-base-field__inspector-field");
        }

        public static T StyleAsFieldLabel<T>(this T element) where T : VisualElement
        {
            return element.WithClass(BaseField<int>.labelUssClassName, "unity-text-element", "unity-label");
        }

        public static T StyleAsPropertyFieldLabel<T>(this T element) where T : VisualElement
        {
            return element.WithClass(PropertyField.labelUssClassName, "unity-text-element", "unity-label");
        }

        public static T StyleAsFieldInput<T>(this T element) where T : VisualElement
        {
            return element.WithClass(BaseField<int>.inputUssClassName);
        }

        public static T StyleAsInspectorField<T>(this T element) where T : VisualElement
        {
            return element.WithClass(
#if UNITY_2022_3_OR_NEWER
                PropertyField.inspectorElementUssClassName, 
#endif
                BaseField<int>.alignedFieldUssClassName);
        }

        public static T WithLabel<T>(this T element, string label) where T : VisualElement
        {
            switch (element)
            {
                case Toggle toggle:
                    toggle.label = label;
                    break;
                case Foldout foldout:
                    foldout.text = label;
                    break;
                case Label labelElement:
                    labelElement.text = label;
                    break;
                case Button button:
                    button.text = label;
                    break;
                case TextField textField:
                    textField.label = label;
                    break;
                case IntegerField integerField:
                    integerField.label = label;
                    break;
                case FloatField floatField:
                    floatField.label = label;
                    break;
                case DoubleField doubleField:
                    doubleField.label = label;
                    break;
                case LongField longField:
                    longField.label = label;
                    break;
                case EnumField enumField:
                    enumField.label = label;
                    break;
                case ObjectField objectField:
                    objectField.label = label;
                    break;
                case PropertyField propertyField:
                    propertyField.label = label;
                    if (string.IsNullOrEmpty(label))
                    {
                        var labelView = propertyField.Q<Label>(null, BaseField<int>.labelUssClassName);
                        if (labelView != null)
                        {
                            labelView.style.display = DisplayStyle.None;
                        }
                    }

                    break;
                case Slider slider:
                    slider.label = label;
                    break;
                case MinMaxSlider minMaxSlider:
                    minMaxSlider.label = label;
                    break;
                case CurveField curveField:
                    curveField.label = label;
                    break;
                case GradientField gradientField:
                    gradientField.label = label;
                    break;
                case ColorField colorField:
                    colorField.label = label;
                    break;
            }
            return element;
        }
        
        public static string GetLabel<T>(this T element) where T : VisualElement
        {
            return element switch
            {
                Toggle toggle => toggle.label,
                Foldout foldout => foldout.text,
                Label labelElement => labelElement.text,
                Button button => button.text,
                TextField textField => textField.label,
                IntegerField integerField => integerField.label,
                FloatField floatField => floatField.label,
                DoubleField doubleField => doubleField.label,
                LongField longField => longField.label,
                EnumField enumField => enumField.label,
                ObjectField objectField => objectField.label,
                PropertyField propertyField => propertyField.label,
                Slider slider => slider.label,
                MinMaxSlider minMaxSlider => minMaxSlider.label,
                CurveField curveField => curveField.label,
                GradientField gradientField => gradientField.label,
                ColorField colorField => colorField.label,
                _ => default
            };
        }

        public static bool TryGetLabel<T>(this T element, out Label label) where T : VisualElement
        {
            label = element.Q<Label>(null, BaseField<int>.labelUssClassName);
            if (label == null)
            {
                label = element.Q<Label>(null, PropertyField.labelUssClassName);
            }
            return label != null;
        }
    }
}