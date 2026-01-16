using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Postica.Common.SmartDropdown;

namespace Postica.Common
{
    public class SeparatorItem : IBuildingBlock
    {
        private string _name;

        private VisualElement _searchElement;
        private (VisualElement element, bool useThisStyle)[] _additionalElements;

        public SearchTags SearchTags => SearchTags.None;
        
        public SeparatorItem(string name)
        {
            _name = name;
        }
        
        public SeparatorItem(string name, params (VisualElement element, bool useSeparatorStyle)[] additionalElements)
        {
            _name = name;
            _additionalElements = additionalElements;
        }

        public VisualElement GetDrawer(Action closeWindow, bool darkMode)
        {
            var ve = new VisualElement();
            ve.AddToClassList("sd-separator");
            var line = new VisualElement();
            line.AddToClassList("sd-line");
            line.AddToClassList("sd-line-left");
            ve.Add(line);
            var label = new Label(_name)
            {
                enableRichText = true,
            };
            label.AddToClassList("sd-label");
            ve.Add(label);
            if (_additionalElements != null)
            {
                foreach (var (element, useThisStyle) in _additionalElements)
                {
                    if (useThisStyle)
                    {
                        element.AddToClassList("sd-separator-element");
                        if (element is Label)
                        {
                            element.AddToClassList("sd-label");
                        }
                    }

                    ve.Add(element);
                }
            }
            line = new VisualElement();
            line.AddToClassList("sd-line");
            line.AddToClassList("sd-line-right");
            ve.Add(line);
            return ve;
        }

        public VisualElement GetSearchDrawer(string searchValue, Action closeWindow, bool darkMode)
        {
            return _searchElement ??= GetDrawer(closeWindow, darkMode);
        }

        public void OnPathResolved(IPathNode node)
        {
            _name ??= node.Name;
        }
    }

}