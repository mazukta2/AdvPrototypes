using System;
using System.Linq;
using System.Reflection;
using Postica.Common;
using Postica.Common.Reflection;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UIElements.Image;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(UnityEventBase), true)]
    // [DrawerSystem.ReplacesPropertyDrawer(typeof(UnityEventDrawer))]
    class UnityEventDrawerForBindings : UnityEventDrawer
    {
        private static Func<UnityEventDrawer, SerializedProperty> _listGetter;
        private static Func<UnityEventDrawer, UnityEventBase> _dummyEventGetter;
        private static Func<UnityEventDrawer, Rect, Rect[]> _rowRectsGetter;
        private static Func<Object, UnityEventBase, SerializedProperty, GenericMenu> _buildPopupList;

        private static bool _initialized;
        private static bool _isValid;
        private static readonly GUIContent _tempContent = new();
        private static GUIContent _bindContent = new();
        private static GUIStyle _labelStyle;
        private static GUIStyle _unboundFieldStyle;

        private static bool Initialize()
        {
            if (_initialized)
            {
                return _isValid;
            }

            _initialized = true;
            _isValid = true;

            _labelStyle = new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                padding = new RectOffset(4, 0, 0, 0),
            };
            
            _unboundFieldStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true,
                padding = new RectOffset(0, 8, 0, 0),
            };
            
            _bindContent = new GUIContent(EditorGUIUtility.isProSkin 
                                            ? Icons.BindIcon_Dark_On.Resize(14, 14) 
                                            : Icons.BindIcon_Lite_On.Resize(14, 14), 
                "Click to highlight the bound field");

            try
            {
                var type = typeof(UnityEventDrawer);
                _listGetter = type.FieldGetter<UnityEventDrawer, SerializedProperty>("m_ListenersArray");
                _dummyEventGetter = type.FieldGetter<UnityEventDrawer, UnityEventBase>("m_DummyEvent");
                var getRectsMethod = type.GetMethod("GetRowRects",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _rowRectsGetter = getRectsMethod.CreateDelegate(typeof(Func<UnityEventDrawer, Rect, Rect[]>)) as
                    Func<UnityEventDrawer, Rect, Rect[]>;

                var buildPopupListMethod = type.GetMethod("BuildPopupList",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                _buildPopupList =
                    buildPopupListMethod.CreateDelegate(
                            typeof(Func<Object, UnityEventBase, SerializedProperty, GenericMenu>)) as
                        Func<Object, UnityEventBase, SerializedProperty, GenericMenu>;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                _isValid = false;
            }

            return _isValid;
        }

        private Rect[] GetRowRects(Rect rect)
        {
            return _rowRectsGetter(this, rect);
        }

        private SerializedProperty GetListenersArray()
        {
            return _listGetter(this);
        }

        private UnityEventBase GetDummyEvent()
        {
            return _dummyEventGetter(this);
        }

        private GenericMenu BuildPopupList(Object target, UnityEventBase dummyEvent,
            SerializedProperty arrayElementAtIndex)
        {
            return _buildPopupList(target, dummyEvent, arrayElementAtIndex);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = base.CreatePropertyGUI(property);
            container.AddBSStyle();

            // Find the list first
            var list = container.Query<ListView>().First();
            var propertyRelative = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            list.bindItem += BindEventView;

            return container;

            void BindEventView(VisualElement view, int i)
            {
                if (i >= propertyRelative.arraySize)
                {
                    return;
                }

                var listener = propertyRelative.GetArrayElementAtIndex(i);
                var eventData = new EventData(listener);
                
                if (!eventData.TryGetProxy(out var proxy))
                {
                    view.RemoveFromClassList("unity-event--proxy");
                    var tester = view.Q<Toggle>("unity-event__proxy-test");
                    if (tester?.value == true)
                    {
                        tester.value = false;
                        view.schedule.Execute(() =>
                        {
                            eventData.argument = null;
                            eventData.listener.serializedObject.ApplyModifiedProperties();
                        }).ExecuteLater(0);
                    }
                    return;
                }

                view.AddToClassList("unity-event--proxy");
                
                var leftColumn = view.Q(null, "unity-event__left-column");
                var rightColumn = view.Q(null, "unity-event__right-column");
                var functionDropdown = rightColumn.Q<DropdownField>("unity-event__function-dropdown");
                var parameterProperty = rightColumn.Q<PropertyField>("unity-event__parameter-property");
                var objectParameter = rightColumn.Q<ObjectField>("unity-event__object-parameter");
                
                functionDropdown.Q<TextElement>(null, DropdownField.textUssClassName).text = $"{proxy.Source.GetType().Name}.UpdateBinding";
                
                // Hide the parameter fields
                parameterProperty.style.display = DisplayStyle.None;
                objectParameter.style.display = DisplayStyle.None;
                
                var bindTargetField = leftColumn.Q<ObjectField>("unity-event__proxy-target");
                if(bindTargetField == null)
                {
                    bindTargetField = new ObjectField
                    {
                        name = "unity-event__proxy-target",
                        allowSceneObjects = true,
                    };
                    bindTargetField.RegisterValueChangedCallback(e =>
                    {
                        eventData.target = e.newValue;
                        eventData.methodName = null;
                        eventData.listener.serializedObject.ApplyModifiedProperties();
                    });
                    leftColumn.Add(bindTargetField);
                    
                    // Hook to events
                    var listenerTarget = leftColumn.Q<PropertyField>("unity-event__listener-target");
                    listenerTarget.OnBind(p => BindEventView(view, i));
                    functionDropdown.RegisterValueChangedCallback(evt =>
                    {
                        // eventData.argument = null;
                        BindEventView(view, i);
                    });
                }
                
                bindTargetField.SetValueWithoutNotify(proxy.Source);
                
                var bindPathButton = rightColumn.Q<Button>("unity-event__proxy-path");
                bindPathButton?.RemoveFromHierarchy();
                bindPathButton = new Button()
                {
                    name = "unity-event__proxy-path",
                    tooltip = _bindContent.tooltip,
                    enableRichText = true,
                };
                
                bindPathButton.clickable = new MultiClickable(
                    new Clickable(() => ProxyClicked(false, bindPathButton.worldBound, proxy)),
                    CreateCtrlClickable(() => ProxyClicked(true, bindPathButton.worldBound, proxy)));
                
                rightColumn.Add(bindPathButton.WithChildren(
                    new Image()
                        {
                            image = EditorGUIUtility.isProSkin
                                ? Icons.BindIcon_Dark_On
                                : Icons.BindIcon_Lite_On,
                        }.WithClass("unity-event__proxy-path__icon"),
                    new Label
                        {
                            text = "! Field is Unbound",
                        }.WithClass("unity-event__proxy-path__unbound")));
                bindPathButton.schedule.Execute(() =>
                {
                    bindPathButton.EnableInClassList("unity-event__proxy-path--unbound", !proxy.IsBound);
                    bindPathButton.SetEnabled(proxy.IsBound);
                }).Every(50);
                
                bindPathButton.EnableInClassList("unity-event__proxy-path--unbound", !proxy.IsBound);
                
                var pieces = proxy.Path.Split('/', '.');
                pieces[^1] = pieces[^1].NiceName().RT().Bold();
                bindPathButton.text = string.Join('\u2192', pieces.Select(p => p.NiceName()));
                
                var bindTester = rightColumn.Q<Toggle>("unity-event__proxy-test");
                if(bindTester == null)
                {
                    bindTester = new Toggle
                    {
                        name = "unity-event__proxy-test",
                        style = { display = DisplayStyle.None },
                    };
                    rightColumn.Add(bindTester);
                }

                bindTester.value = true;
            }
            
            Clickable CreateCtrlClickable(Action action)
            {
                var clickable = new PreviewClickable(action, "focus_proxy");
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

        protected override void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!Initialize())
            {
                base.DrawEvent(rect, index, isActive, isFocused);
                return;
            }

            var eventData = new EventData(GetListenersArray().GetArrayElementAtIndex(index));

            if (!eventData.TryGetProxy(out var proxy))
            {
                if (eventData.IsUpdateProxyEvent())
                {
                    eventData.argument = null;
                    eventData.target = null;
                }
                base.DrawEvent(rect, index, isActive, isFocused);
                return;
            }

            ++rect.y;
            Rect[] rowRects = GetRowRects(rect);
            Rect position1 = rowRects[0];
            Rect position2 = rowRects[1];
            Rect rect1 = rowRects[2];
            Rect position3 = rowRects[3];
            SerializedProperty propertyRelative1 = eventData.listener.FindPropertyRelative("m_CallState");
            SerializedProperty propertyRelative4 = eventData.listener.FindPropertyRelative("m_Target");
            SerializedProperty propertyRelative5 = eventData.listener.FindPropertyRelative("m_MethodName");
            Color backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            EditorGUI.PropertyField(position1, propertyRelative1, GUIContent.none);
            EditorGUI.BeginChangeCheck();
            GUI.Box(position2, GUIContent.none);
            var newTarget = EditorGUI.ObjectField(position2, proxy.Source, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                eventData.target = newTarget;
                propertyRelative5.stringValue = null;
            }

            var pieces = proxy.Path.Split('/', '.');
            pieces[^1] = pieces[^1].NiceName().RT().Bold();
            _bindContent.text = string.Join('\u2192', pieces.Select(p => p.NiceName()));

            using (new EditorGUI.DisabledScope(!proxy.IsBound))
            {
                if (GUI.Button(position3, _bindContent, _labelStyle))
                {
                    ProxyClicked(Event.current.command || Event.current.control, position3, proxy);
                }
            }

            if (!proxy.IsBound)
            {
                GUI.Label(position3, "! Field is Unbound".RT().Color(BindColors.Error), _unboundFieldStyle);
            }

            using (new EditorGUI.DisabledScope(propertyRelative4.objectReferenceValue == null))
            {
                EditorGUI.BeginProperty(rect1, GUIContent.none, propertyRelative5);
                GUIContent content = !EditorGUI.showMixedValue
                    ? TempContent(eventData, proxy)
                    : DrawerSystem.EditorGUI.MixedValueContent;
                if (EditorGUI.DropdownButton(rect1, content, FocusType.Passive, EditorStyles.popup))
                {
                    BuildPopupList(propertyRelative4.objectReferenceValue, GetDummyEvent(), eventData.listener)
                        .DropDown(rect1);
                }

                EditorGUI.EndProperty();
            }

            GUI.backgroundColor = backgroundColor;
        }

        private static void ProxyClicked(bool isFocus, Rect position, BindProxy proxy)
        {
            if(isFocus)
            {
                BindDataDrawer.Focus(proxy.Source, proxy.Path);
                return;
            }
            BindDataDrawer.ShowBindView(/*GUIUtility.ScreenToGUIRect*/(position), proxy);
        }

        private static GUIContent TempContent(EventData eventData, BindProxy proxy)
        {
            _tempContent.text = eventData.target && proxy.Source
                ? $"{proxy.Source.GetType().Name}.UpdateBinding"
                : "<Missing UnknownComponent.UpdateBinding>".RT().Color(BindColors.Error);
            return _tempContent;
        }

        private static GUIContent TempContent(string text)
        {
            _tempContent.text = text;
            return _tempContent;
        }

        private struct EventData
        {
            public readonly SerializedProperty listener;

            public SerializedProperty targetProperty => listener.FindPropertyRelative("m_Target");

            public Object target
            {
                get => targetProperty.objectReferenceValue;
                set => targetProperty.objectReferenceValue = value;
            }

            public SerializedProperty targetAssemblyTypeNameProperty =>
                listener.FindPropertyRelative("m_TargetAssemblyTypeName");

            public string targetAssemblyTypeName
            {
                get => targetAssemblyTypeNameProperty.stringValue;
                set => targetAssemblyTypeNameProperty.stringValue = value;
            }

            public SerializedProperty methodNameProperty => listener.FindPropertyRelative("m_MethodName");

            public string methodName
            {
                get => methodNameProperty.stringValue;
                set => methodNameProperty.stringValue = value;
            }

            public SerializedProperty modeProperty => listener.FindPropertyRelative("m_Mode");

            public PersistentListenerMode mode
            {
                get => (PersistentListenerMode)modeProperty.enumValueIndex;
                set => modeProperty.enumValueIndex = (int)value;
            }

            public SerializedProperty argumentProperty => listener.FindPropertyRelative("m_Arguments.m_StringArgument");

            public string argument
            {
                get => argumentProperty.stringValue;
                set => argumentProperty.stringValue = value;
            }

            public EventData(SerializedProperty listener)
            {
                this.listener = listener;
            }
            
            public bool IsUpdateProxyEvent() => target is IBindProxyProvider bindings && methodName == "UpdateProxy";

            public bool TryGetProxy(out BindProxy proxy)
            {
                proxy = null;

                if (target is not IBindProxyProvider bindings)
                {
                    return false;
                }

                if (methodName != "UpdateProxy")
                {
                    return false;
                }

                return bindings.TryGetProxy(argument, out proxy, out _);
            }
        }
    }
}