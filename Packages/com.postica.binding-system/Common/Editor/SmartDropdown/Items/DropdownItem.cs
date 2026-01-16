using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public class DropdownItem : SmartDropdown.IBuildingBlock
    {
        protected string _name;
        protected string _path;
        protected string _secondLabel;
        protected VisualElement _searchElement;
        protected readonly string _description;
        protected readonly Texture2D _icon;
        protected readonly bool _alterGroup;

        protected SearchTags _searchTags;
        protected Action<VisualElement> _onPreRender;

        public Action OnSelectCallback { get; set; }
        public string Name { get => _name; internal set => _name = value; }
        public SearchTags SearchTags => _searchTags;

        public string SecondLabel
        {
            get => _secondLabel;
            set => _secondLabel = value;
        }

        public DropdownItem(string name, Action onSelectCallback, bool canAlterGroup = true) 
            : this(name, null, onSelectCallback, null, null, canAlterGroup) { }

        public DropdownItem(string name, string type, Action onSelectCallback, Texture2D icon, string description = null, bool canAlterGroup = true)
        {
            _name = name;
            _description = description;
            _icon = icon;
            OnSelectCallback = onSelectCallback;// ?? throw new ArgumentNullException(nameof(onSelectCallback));
            _searchTags = name;
            _secondLabel = type;
            _alterGroup = canAlterGroup;
        }
        
        public virtual DropdownItem OnPreRender(Action<VisualElement> onPreRender)
        {
            _onPreRender = onPreRender;
            return this;
        }

        public virtual VisualElement GetDrawer(Action closeWindow, bool darkMode)
        {
            var ve = new VisualElement();
            ve.AddToClassList("sd-dropdown-item");
            ve.AddToClassList("sd-clickable");

            var image = new Image() { image = _icon };
            image.AddToClassList("sd-icon");
            image.AddToClassList("sd-image");
            image.EnableInClassList("sd-has-value", _icon);
            var border = new VisualElement();
            border.AddToClassList("sd-border");
            image.Add(border);
            ve.Add(image);

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
                ve.Add(typeLabel);
            }

            ve.AddManipulator(new Clickable(OnSelectCallback + closeWindow));
            
            _onPreRender?.Invoke(ve);
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
            if(_alterGroup
                && node.SiblingIndex == 0 /* <-- The first one and can alter*/
                && node.Parent is SmartDropdown.IPathGroup group
                && group.UIElementFactory == null) /*<-- Alter only if there is no factory override*/
            {
                group.Description = _secondLabel;
                group.Icon = _icon;
            }
        }

        public VisualElement GetSearchDrawer(string searchValue, Action closeWindow, bool darkMode)
        {
            if(_searchElement == null)
            {
                _searchElement = GetDrawer(closeWindow, darkMode);
                var pathOnly = _path.Replace('/', '→');
                _searchElement.Add(new Label(pathOnly).WithClass(SmartDropdown.ussSearchLabelPath));
            }

            var mainLabel = _searchElement.Q<Label>(className: "sd-label-main");
            mainLabel.enableRichText = true;
            mainLabel.text = _name;

            // Get the last index range of the search value in the path
            var index = _name.LastIndexOf(searchValue, StringComparison.OrdinalIgnoreCase);
            if(index < 0)
            {
                return _searchElement;
            }

            var firstPartOfName = _name.Substring(0, index);
            var foundPartOfName = _name.Substring(index, searchValue.Length);
            var lastPartOfName = _name.Substring(index + searchValue.Length, _name.Length - index - searchValue.Length);
            
            // Build the text
            mainLabel.text = $"{firstPartOfName}<b><color=#eeae00>{foundPartOfName}</color></b>{lastPartOfName}";
            
            return _searchElement;
        }
    }
}