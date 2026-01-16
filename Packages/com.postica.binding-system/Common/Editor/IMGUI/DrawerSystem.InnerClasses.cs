using System;
using System.Reflection;
using System.Collections.Generic;
using Postica.Common.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    public partial class DrawerSystem
    {
        public readonly static ScriptAttributeUtilityProxy ScriptAttributeUtility = new();
        public readonly static ReorderableListWrapperProxy ReorderableListWrapper = new();
        public readonly static PropertyHandlerProxy PropertyHandler = new();
        public readonly static PropertyHandlerCacheProxy PropertyHandlerCache = new();
        public readonly static MaterialPropertyHandlerProxy MaterialPropertyHandler = new();
        public readonly static MaterialEditorProxy MaterialEditor = new();
        public readonly static MaterialPropertyProxy MaterialProperty = new();

        public class EditorProxy : ClassProxy<Editor>
        {
            private static readonly PropertyWrapper<PropertyHandlerCacheProxy> _propertyHandlerCache =
                new(nameof(propertyHandlerCache));

            public PropertyHandlerCacheProxy propertyHandlerCache => This(_propertyHandlerCache);

            public EditorProxy(Editor instance) : base(instance)
            {
            }

            public EditorProxy() : base()
            {
            }

            public static implicit operator EditorProxy(Editor instance) => new EditorProxy(instance);
        }

        [For(typeof(Editor), "UnityEditor.ScriptAttributeUtility")]
        public class ScriptAttributeUtilityProxy : ClassProxy
        {
            private static readonly FieldWrapper<PropertyHandlerCacheProxy> s_GlobalCache = new(nameof(s_GlobalCache));

            private static readonly FieldWrapper<PropertyHandlerProxy> s_SharedNullHandler =
                new(nameof(s_SharedNullHandler));
            
            private static readonly FieldWrapper<DictionaryProxy<Type, Type>>
                k_DrawerStaticTypesCache = new(nameof(k_DrawerStaticTypesCache));

            private static readonly PropertyWrapper<PropertyHandlerCacheProxy> _propertyHandlerCache =
                new(nameof(propertyHandlerCache));

            private static readonly MethodWrapper<SerializedProperty, PropertyHandlerProxy> _GetHandler =
                new(nameof(GetHandler));

            public PropertyHandlerProxy GetHandler(SerializedProperty property) => Static(_GetHandler).Call(property);

            public DictionaryProxy<Type, Type> DrawerStaticTypesCache => Static(k_DrawerStaticTypesCache);
            
            public PropertyHandlerCacheProxy propertyHandlerCache
            {
                get => Static(_propertyHandlerCache);
                set => Static(_propertyHandlerCache).Value = value;
            }

            public PropertyHandlerProxy SharedNullHandler => Static(s_SharedNullHandler);

            public PropertyHandlerCacheProxy GlobalCache => Static(s_GlobalCache);
        }

        [For(typeof(Editor), "UnityEditor.PropertyHandlerCache")]
        public class PropertyHandlerCacheProxy : ClassProxy
        {
            private static readonly MethodWrapper<SerializedProperty, PropertyHandlerProxy> _GetHandler =
                new(nameof(GetHandler));

            private static readonly VoidMethodWrapper<SerializedProperty, PropertyHandlerProxy> _SetHandler =
                new(nameof(SetHandler));

            private static readonly MethodWrapper<SerializedProperty, int> _GetPropertyHash =
                new(nameof(GetPropertyHash));

            public PropertyHandlerProxy GetHandler(SerializedProperty property) => This(_GetHandler).Call(property);

            public void SetHandler(SerializedProperty property, PropertyHandlerProxy handler) =>
                This(_SetHandler).Call(property, handler);

            public int GetPropertyHash(SerializedProperty property) => Static(_GetPropertyHash).Call(property);
        }

        [For(typeof(Editor), "UnityEditor.PropertyHandler")]
        public class PropertyHandlerProxy : ClassProxy
        {
            private static readonly FieldWrapper<DictionaryProxy<string, ReorderableListWrapperProxy>>
                s_reorderableLists = new(nameof(s_reorderableLists));

            private static readonly FieldWrapper<List<DecoratorDrawer>> m_DecoratorDrawers =
                new(nameof(m_DecoratorDrawers));

            private static readonly FieldWrapper<List<PropertyDrawer>> m_PropertyDrawers =
                new(nameof(m_PropertyDrawers));

            private static readonly FieldWrapper<int> m_NestedLevel = new(nameof(m_NestedLevel));
            private static readonly PropertyWrapper<PropertyDrawer> _propertyDrawer = new(nameof(propertyDrawer));
            private static readonly PropertyWrapper<bool> _empty = new(nameof(empty));
            private static readonly PropertyWrapper<bool> _hasPropertyDrawer = new(nameof(hasPropertyDrawer));

            private static readonly MethodWrapper<int, NestingContext> _ApplyNestingContext =
                new(nameof(ApplyNestingContext));

            private static readonly MethodWrapper<SerializedProperty, bool> _UseReorderabelListControl =
                new(nameof(UseReorderabelListControl));

            public PropertyHandlerProxy EnsureInitialized()
            {
                Instance ??= Activator.CreateInstance(Type);
                // if (!empty && PropertyDrawers != null)
                // {
                //     return this;
                // }

                PropertyDrawers = PropertyDrawers == null
                    ? new List<PropertyDrawer>()
                    : new List<PropertyDrawer>(PropertyDrawers);
                DecoratorDrawers = DecoratorDrawers == null
                    ? new List<DecoratorDrawer>()
                    : new List<DecoratorDrawer>(DecoratorDrawers);

                return this;
            }

            public DictionaryProxy<string, ReorderableListWrapperProxy> ReorderableLists => Static(s_reorderableLists);

            public bool TryGetReorderableList(SerializedProperty property, out ReorderableList list)
            {
                var propertyId = ReorderableListWrapper.GetPropertyIdentifier(property);
                if (PropertyHandler.ReorderableLists.TryGetValue(propertyId, out var wrapper) && wrapper != null)
                {
                    list = wrapper.ReorderableList;
                    return true;
                }

                list = null;
                return false;
            }

            public List<DecoratorDrawer> DecoratorDrawers
            {
                get => This(m_DecoratorDrawers);
                set => This(m_DecoratorDrawers).Value = value;
            }

            public List<PropertyDrawer> PropertyDrawers
            {
                get => This(m_PropertyDrawers);
                set => This(m_PropertyDrawers).Value = value;
            }

            public int NestedLevel
            {
                get => This(m_NestedLevel);
                set => This(m_NestedLevel).Value = value;
            }

            public PropertyDrawer propertyDrawer => This(_propertyDrawer);
            public bool empty => This(_empty);
            public bool hasPropertyDrawer => This(_hasPropertyDrawer);

            public NestingContext ApplyNestingContext(int nestingLevel) =>
                This(_ApplyNestingContext).Call(nestingLevel);

            public bool UseReorderabelListControl(SerializedProperty property) =>
                Static(_UseReorderabelListControl).Call(property);
        }

        [For(typeof(Editor), "UnityEditor.MaterialPropertyHandler")]
        public class MaterialPropertyHandlerProxy : ClassProxy
        {
            private static readonly FieldWrapper<MaterialPropertyDrawer> m_PropertyDrawer =
                new(nameof(m_PropertyDrawer));

            private static readonly FieldWrapper<DictionaryProxy<string, MaterialPropertyHandlerProxy>>
                s_PropertyHandlers = new(nameof(s_PropertyHandlers));

            private static readonly MethodWrapper<Shader, string, string> _GetPropertyString =
                new(nameof(GetPropertyString));

            public MaterialPropertyDrawer PropertyDrawer
            {
                get => This(m_PropertyDrawer);
                set => This(m_PropertyDrawer).Value = value;
            }

            public DictionaryProxy<string, MaterialPropertyHandlerProxy> PropertyHandlers => Static(s_PropertyHandlers);

            public string GetPropertyString(Shader shader, string name) =>
                Static(_GetPropertyString).Call(shader, name);
        }

        [For(typeof(Editor), "UnityEditor.PropertyHandler+NestingContext")]
        public class NestingContext : ClassProxy
        {
        }


        [For(typeof(Editor), "UnityEditorInternal.ReorderableListWrapper")]
        public class ReorderableListWrapperProxy : ClassProxy
        {
            private static readonly FieldWrapper<ReorderableList> m_ReorderableList = new(nameof(m_ReorderableList));

            private static readonly MethodWrapper<SerializedProperty, string> _GetPropertyIdentifier =
                new(nameof(GetPropertyIdentifier));

            public ReorderableList ReorderableList
            {
                get => This(m_ReorderableList);
                set => This(m_ReorderableList).Value = value;
            }

            public string GetPropertyIdentifier(SerializedProperty property) =>
                Static(_GetPropertyIdentifier).Call(property);
        }

        public class MaterialEditorProxy : ClassProxy<MaterialEditor>
        {
            private static readonly FieldWrapper m_contextualPropertyMenu = new(nameof(contextualPropertyMenu));
            private static readonly FieldWrapper<GUIContent> s_TilingText = new(nameof(s_TilingText));
            private static readonly FieldWrapper<GUIContent> s_OffsetText = new(nameof(s_OffsetText));

            public GUIContent TilingText => Static(s_TilingText);
            public GUIContent OffsetText => Static(s_OffsetText);

            [For(typeof(MaterialEditor), "UnityEditor.MaterialEditor+MaterialPropertyCallbackFunction")]
            public delegate void OnMaterialEditorDelegate(GenericMenu menu,
                MaterialProperty property,
                Renderer[] renderers);

            private static readonly DelegateConverter<OnMaterialEditorDelegate> _delegateConverter = new();

            public OnMaterialEditorDelegate contextualPropertyMenu
            {
                get => _delegateConverter.ToT(Static(m_contextualPropertyMenu).Value);
                set => Static(m_contextualPropertyMenu).Value = _delegateConverter.FromT(value);
            }
        }

        public class MaterialPropertyProxy : ClassProxy<MaterialProperty>
        {
            public delegate void OnPropertyDelegate(Rect position, MaterialProperty property, Object[] targets,
                float startY);

            private static readonly FieldWrapper s_PropertyStackRaw = new(nameof(s_PropertyStack));
            private static FieldWrapper<ListProxy<PropertyDataProxy>> s_PropertyStack;

            private List<OnPropertyDelegate> _onBeginPropertyCallbacks = new();
            private List<OnPropertyDelegate> _onEndPropertyCallbacks = new();

            public readonly PropertyDataProxy PropertyData = new();

            public event OnPropertyDelegate OnBeginProperty
            {
                add
                {
                    if (_onBeginPropertyCallbacks.Count == 0)
                    {
                        InitializePropertyStack();
                    }

                    if (_onBeginPropertyCallbacks.Contains(value)) return;
                    _onBeginPropertyCallbacks.Add(value);
                }
                remove => _onBeginPropertyCallbacks.Remove(value);
            }

            public event OnPropertyDelegate OnEndProperty
            {
                add
                {
                    if (_onEndPropertyCallbacks.Count == 0)
                    {
                        InitializePropertyStack();
                    }

                    if (_onEndPropertyCallbacks.Contains(value)) return;
                    _onEndPropertyCallbacks.Add(value);
                }
                remove => _onEndPropertyCallbacks.Remove(value);
            }

            private void InvokeOnBeginProperty(PropertyDataProxy data)
            {
                Debug.Log("Adding property to stack");
                for (var index = 0; index < _onBeginPropertyCallbacks.Count; index++)
                {
                    var callback = _onBeginPropertyCallbacks[index];
                    try
                    {
                        callback.Invoke(data.position, data.property, data.targets, data.startY);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        _onBeginPropertyCallbacks.RemoveAt(index--);
                    }
                }
            }

            private void InvokeOnEndProperty(PropertyDataProxy data)
            {
                for (var index = 0; index < _onEndPropertyCallbacks.Count; index++)
                {
                    var callback = _onEndPropertyCallbacks[index];
                    try
                    {
                        callback.Invoke(data.position, data.property, data.targets, data.startY);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        _onEndPropertyCallbacks.RemoveAt(index--);
                    }
                }
            }

            public ListProxy<PropertyDataProxy> PropertyStack
            {
                get
                {
                    if (s_PropertyStack != null) return Static(s_PropertyStack);

                    InitializePropertyStack();
                    return Static(s_PropertyStack);
                }
            }

            private void InitializePropertyStack()
            {
                s_PropertyStack = new FieldWrapper<ListProxy<PropertyDataProxy>>(nameof(s_PropertyStack));
                // Replace the original list with a 
                var innerType = typeof(PropertyDataProxy).GetCustomAttribute<ForAttribute>().Type;
                var stackType = typeof(ObservableList<,>).MakeGenericType(typeof(PropertyDataProxy), innerType);
                var stack = Activator.CreateInstance(stackType);

                Static(s_PropertyStackRaw).Value = stack;

                ((IObservableList<PropertyDataProxy>)stack).OnAdd += InvokeOnBeginProperty;
                ((IObservableList<PropertyDataProxy>)stack).OnRemove += InvokeOnEndProperty;
            }

            private interface IObservableList<T>
            {
                event Action<T> OnAdd;
                event Action<T> OnRemove;
            }

            private class ObservableList<S, T> : List<T>, IList<T>, IObservableList<S>
                where S : ClassProxy, ISafeProxy, new()
            {
                public event Action<S> OnAdd;
                public event Action<S> OnRemove;

                private S ClassProxy { get; } = new S();

                public new void Add(T item)
                {
                    base.Add(item);
                    if (OnAdd == null) return;

                    try
                    {
                        ClassProxy.SetInstance(item);
                        OnAdd(ClassProxy);
                    }
                    catch
                    {
                        // Do nothing
                    }
                }

                public new bool Remove(T item)
                {
                    var result = base.Remove(item);
                    if (OnRemove == null) return result;

                    try
                    {
                        ClassProxy.SetInstance(item);
                        OnRemove.Invoke(ClassProxy);
                    }
                    catch
                    {
                        // Do nothing
                    }

                    return result;
                }
            }

            [For(typeof(MaterialProperty), "UnityEditor.MaterialProperty+PropertyData")]
            public class PropertyDataProxy : ClassProxy, ISafeProxy
            {
                private static readonly FieldWrapper<MaterialProperty> _property = new(nameof(property));
                private static readonly FieldWrapper<float> _startY = new(nameof(startY));
                private static readonly FieldWrapper<Rect> _position = new(nameof(position));
                private static readonly FieldWrapper<UnityEngine.Object[]> _targets = new(nameof(targets));

                private static readonly FieldWrapper<List<MaterialProperty>> _capturedProperties =
                    new(nameof(capturedProperties));

                public List<MaterialProperty> capturedProperties => Static(_capturedProperties);

                public UnityEngine.Object[] targets
                {
                    get => This(_targets);
                    set => This(_targets).Value = value;
                }

                public MaterialProperty property
                {
                    get => This(_property);
                    set => This(_property).Value = value;
                }

                public float startY
                {
                    get => This(_startY);
                    set => This(_startY).Value = value;
                }

                public Rect position
                {
                    get => This(_position);
                    set => This(_position).Value = value;
                }

                public void SetInstance(object instance)
                {
                    Instance = instance;
                }
            }
        }

        public class InspectorElementProxy : ClassProxy<InspectorElement>
        {
            private static readonly PropertyWrapper<Editor> _editor = new(nameof(editor));

            public Editor editor => This(_editor);

            public InspectorElementProxy(InspectorElement instance) : base(instance)
            {
            }

            public static implicit operator InspectorElementProxy(InspectorElement instance) =>
                new InspectorElementProxy(instance);
        }
    }
}