using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public class DropdownLazyItem : SmartDropdown.IBuildingBlock
    {
        protected string _name;
        protected string _path;
        protected Func<SmartDropdown.IBuildingBlock> _factory;
        protected SmartDropdown.IBuildingBlock _instance;

        protected SearchTags _searchTags;

        public string Name => _name;
        public SearchTags SearchTags => _searchTags;
        
        public DropdownLazyItem(string name, Func<SmartDropdown.IBuildingBlock> factory)
        {
            _name = name;
            _factory = factory;
            _searchTags = name;
        }

        public virtual VisualElement GetDrawer(Action closeWindow, bool darkMode)
        {
            _instance ??= _factory();
            return _instance.GetDrawer(closeWindow, darkMode);
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
            _instance ??= _factory();
            return _instance.GetSearchDrawer(searchValue, closeWindow, darkMode);
        }
    }
}