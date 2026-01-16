using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{

    public class DropdownGroupItem
    {
        private readonly string _type;
        private readonly SmartDropdown.IBuildingBlock _innerBlock;
        private VisualElement _groupUI;
        private VisualElement _searchGroupUI;

        public DropdownGroupItem(string type, SmartDropdown.IBuildingBlock innerBlock)
        {
            _type = type;
            _innerBlock = innerBlock;
        }

        public VisualElement CreateGroupUI(SmartDropdown.IPathGroup group, Action onClick, Action closeWindow,
            bool isDarkSkin)
        {
            _groupUI ??= CreateGroupView(onClick, closeWindow, isDarkSkin);
            
            if (_groupUI.childCount > 1)
            {
                _groupUI.RemoveAt(0);
            }

            var innerView = _innerBlock.GetDrawer(closeWindow, isDarkSkin);
            _groupUI.Insert(0, innerView);
            return _groupUI;
        }

        public VisualElement CreateSearchGroupUI(string searchValue, SmartDropdown.IPathGroup group, Action onClick, Action closeWindow, bool isDarkSkin)
        {
            if (_searchGroupUI == null)
            {
                _searchGroupUI = CreateGroupView(onClick, closeWindow, isDarkSkin);
                _searchGroupUI.AddToClassList("sd-search-group");
                _innerBlock.OnPathResolved(group);
            }
            
            if (_searchGroupUI.childCount > 1)
            {
                _searchGroupUI.RemoveAt(0);
            }

            var innerView = _innerBlock.GetSearchDrawer(searchValue, closeWindow, isDarkSkin);
            _searchGroupUI.Insert(0, innerView);
            return _searchGroupUI;
        }
        
        
        private VisualElement CreateGroupView(Action onClick, Action closeWindow, bool isDarkSkin)
        {
            var groupUI = new VisualElement();
            groupUI.AddToClassList("sd-super-group");

            var ve = new VisualElement();
            ve.AddToClassList("sd-clickable");
            ve.AddToClassList("sd-group");

            var label = new Label(_type);
            label.AddToClassList("sd-label");
            label.AddToClassList("sd-label-description");
            ve.Add(label);

            var rightArrow = new Label(@"›");
            rightArrow.AddToClassList("sd-right-arrow");
            ve.Add(rightArrow);

            ve.AddManipulator(new Clickable(onClick));

            groupUI.Add(ve);
            return groupUI;
        }
    }

}