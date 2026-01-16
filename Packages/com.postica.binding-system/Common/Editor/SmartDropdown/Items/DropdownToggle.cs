using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public class DropdownToggle : SmartDropdown.IBuildingBlock
    {
        protected string _name;
        protected string _path;
        protected VisualElement _searchElement;
        protected readonly string _description;
        protected readonly string _secondLabel;
        protected readonly Func<bool, bool> _onSelectCallback;

        protected bool _value;
        protected SearchTags _searchTags;
        protected Toggle _toggle;

        public string Name => _name;
        public SearchTags SearchTags => _searchTags;
        public bool Value
        {
            get => _value;
            set
            {
                if(_value != value)
                {
                    _value = _onSelectCallback?.Invoke(value) ?? value;
                    _toggle?.SetValueWithoutNotify(_value);
                }
            }
        }

        public DropdownToggle(string name, bool value, Func<bool, bool> onSelectCallback) 
            : this(name, value, onSelectCallback, null, null) { }

        public DropdownToggle(string name, bool value, Func<bool, bool> onSelectCallback, string type, string description = null)
        {
            _name = name;
            _description = description;
            _onSelectCallback = onSelectCallback ?? throw new ArgumentNullException(nameof(onSelectCallback));
            _searchTags = name;
            _secondLabel = type;
            _value = value;
        }

        public virtual VisualElement GetDrawer(Action closeWindow, bool darkMode)
        {
            var ve = new VisualElement();
            ve.AddToClassList("sd-dropdown-item");
            //ve.AddToClassList("sd-clickable");

            _toggle = new Toggle() { value = _value };
            _toggle.AddToClassList("sd-toggle");
            _toggle.RegisterValueChangedCallback(e => Value = e.newValue);

            var border = new VisualElement();
            border.AddToClassList("sd-border");
            _toggle.Add(border);
            ve.Add(_toggle);

            var label = new Label(_name);
            label.AddToClassList("sd-label");
            label.AddToClassList("sd-label-main");
            ve.Add(label);

            if (!string.IsNullOrEmpty(_description))
            {
                ve.tooltip = _description;
            }

            if (!string.IsNullOrEmpty(_secondLabel))
            {
                var typeLabel = new Label(_secondLabel);
                typeLabel.AddToClassList("sd-label");
                typeLabel.AddToClassList("sd-label-description");

                //var typeLabelParent = new VisualElement();
                //typeLabelParent.AddToClassList("sd-right");
                //typeLabelParent.AddToClassList("sd-row");
                //typeLabelParent.Add(typeLabel);

                ve.Add(typeLabel);
            }

            return ve;
        }

        public virtual void OnPathResolved(SmartDropdown.IPathNode node)
        {
            _path = node.Path;
            // Update search tags
            if (string.IsNullOrEmpty(_name))
            {
                _searchTags = _name = node.Name;
            }
        }

        public VisualElement GetSearchDrawer(string searchValue, Action closeWindow, bool darkMode)
        {
            return _searchElement ??= GetDrawer(closeWindow, darkMode);
        }
    }
}