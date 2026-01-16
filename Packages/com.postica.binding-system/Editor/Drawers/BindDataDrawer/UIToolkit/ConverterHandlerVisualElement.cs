using Postica.Common;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal readonly partial struct ConverterHandler
        {
            public bool isInteractive => instance != null && (_canSelect || _properties.Length > 0);
            public bool isUnsafe => instance?.IsSafe == false; 

            public class ConverterView : VisualElement
            {
                private Action<IConverter> _onSelectCallback;
                private Foldout _view;
                private VisualElement _header;
                private Toggle _foldoutToggle;
                private VisualElement _restOfHeader;
                private Button _type;
                private Label _label;
                private Label _typeLabel;
                private Image _icon;
                private Image _safeIcon;
                private ConverterHandler _currentHandler;
                private Styles _styles;
                private IConverter _currentInstance;

                public Image iconElement => _icon;

                public ConverterView(SerializedProperty property, Styles styles, string label, Action<IConverter> onConverterSelected)
                {
                    _styles = styles;
                    _onSelectCallback = onConverterSelected;

                    AddToClassList("bind-converter");

                    (_type, _typeLabel) = DropdownButton.CreateOnlyButton();
                    _type.clicked += ShowConvertersList;
                    _type.StyleAsFieldInput().WithClass("bind-converter__type");

                    _icon = new Image().WithClass("bind-converter__icon");
                    _safeIcon = new Image().WithClass("bind-converter__safe-icon");
                    _label = new Label(label).StyleAsFieldLabel().WithClass("bind-converter__label");

                    _view = new Foldout().WithClass("bind-converter__view");
                    _view.viewDataKey = property.GetViewDataKey();
                    _header = new VisualElement().WithClass("bind-converter__header");
                    _restOfHeader = new VisualElement().WithClass("bind-converter__header-rest");
                    _foldoutToggle = _view.hierarchy.ElementAt(0) as Toggle;
                    _foldoutToggle?.RemoveFromHierarchy();
                    _view.hierarchy.Insert(0, _header.WithChildren(_icon, _foldoutToggle, _restOfHeader));

                    Add(_view);
                }

                private void SetHeader(bool collapsable, string label, string tooltip, params VisualElement[] elements)
                {
                    _foldoutToggle.EnableInClassList("hidden", !collapsable);
                    _foldoutToggle.text = label;
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        _foldoutToggle.tooltip = tooltip;
                    }

                    _restOfHeader.Clear();
                    _restOfHeader.WithChildren(elements);
                }

                private void ShowConvertersList()
                {
                    if (!_currentHandler._canSelect)
                    {
                        return;
                    }

                    var implicitConverter = _currentHandler._implicitConverter;
                    var instance = _currentHandler.instance;
                    var templates = _currentHandler._templates;

                    var smartDropdown = new SmartDropdown(false, "Change Converter");
                    if (implicitConverter != null)
                    {
                        smartDropdown.Add(implicitConverter.Id,
                                          "Implicit",
                                          implicitConverter.IsSafe ? _styles.converterIcon : _styles.converterUnsafeIcon,
                                          () => _onSelectCallback(null),
                                          null,
                                          instance?.Id == implicitConverter.Id);
                    }
                    foreach (var template in templates)
                    {
                        smartDropdown.Add(template.Id,
                                        template.IsSafe ? string.Empty : "Unsafe",
                                        template.IsSafe ? _styles.converterIcon : _styles.converterUnsafeIcon,
                                        () => _onSelectCallback(template.Create()),
                                        null,
                                        instance?.Id == template.Id);
                    }

                    smartDropdown.Show(_type.worldBound);
                }

                private void ResetDefaultHeader(in ConverterHandler handler)
                {
                    SetHeader(handler._properties?.Length > 0, null, null, _label, _safeIcon, _type);
                }

                public void Refresh(SerializedProperty property, in ConverterHandler handler)
                {
                    EnableInClassList("minimal", true);
                    EnableInClassList("unsafe", handler.isUnsafe);

                    ResetDefaultHeader(handler);
                    _currentHandler = handler;

                    if (handler._properties == null)
                    {
                        return;
                    }

                    EnableInClassList("fixed-value", !handler._canSelect);
                    RemoveFromClassList("no-param");
                    RemoveFromClassList("one-param");
                    RemoveFromClassList("multi-param");

                    handler._properties.Refresh();

                    if (!handler._canSelect)
                    {
                        _view.Clear();
                        
                        if (handler._properties.Length == 0)
                        {
                            AddToClassList("no-param");
                        }
                        else if (handler._properties.Length == 1)
                        {
                            AddToClassList("one-param");

                            var view = handler._properties.properties[0].view;
                            if (!handler.IsEmpty())
                            {
                                try
                                {
                                    var propertyTooltip = handler._properties.properties[0].property.tooltip;
                                    ;
                                    view?.schedule.Execute(() =>
                                        {
                                            var label = view.Q(null, "bs-bind-value")
                                                            ?.Q<Label>(null, "unity-base-field__label")
                                                        ?? view.Q<Label>(null, "unity-base-field__label");
                                            label?.WithTooltip(propertyTooltip);
                                        })
                                        .ExecuteLater(1000);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e);
                                }
                            }

                            SetHeader(false, null, null, view);
                        }
                        else
                        {
                            AddToClassList("multi-param");

                            SetHeader(true, null, null, handler._properties.properties[0].view);

                            _view.Clear();

                            for (int i = 1; i < handler._properties.Length; i++)
                            {
                                ref var view = ref handler._properties.properties[i].view;
                                _view.Add(view);
                            }
                        }
                    }
                    else
                    {
                        _view.Clear();
                        for (int i = 0; i < handler._properties.Length; i++)
                        {
                            ref var view = ref handler._properties.properties[i].view;
                            _view.Add(view);
                        }
                    }

                    _typeLabel.text = handler._content?.text;
                    _type.WithTooltip(handler._content?.tooltip);
                    _label.WithTooltip(handler._content?.tooltip);

                    _icon.tooltip = GetFullTooltip(handler);
                }

                private static string GetFullTooltip(in ConverterHandler handler)
                {
                    string converterType = handler.isRead
                                      ? "Convert on Read: ".RT().Color(BindDataUI.Colors.Main).Bold()
                                      : "Convert on Write: ".RT().Color(BindDataUI.Colors.Main).Bold();
                    string safeKeyword = handler.isUnsafe
                                      ? "[UNSAFE - conversion may fail] ".RT().Color(BindDataUI.Colors.ConverterUnsafe).Bold()
                                      : string.Empty;
                    return $"{converterType}{handler._content?.text.RT().Bold()}\n{handler._content?.tooltip}\n{safeKeyword}";
                }
            }
        }
    }
}