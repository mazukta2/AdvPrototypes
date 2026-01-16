using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    class EnhancedFoldout : Foldout
    {
        public const string ussEF = "enhanced-foldout";
        public const string ussEFHeader = "enhanced-foldout__header";
        public const string ussEFLabel = "enhanced-foldout__header__label";
        public const string ussEFLabelFixed = "enhanced-foldout__header__label--fixed";
        public const string ussEFStartOfHeader = "enhanced-foldout__header-start";
        public const string ussEFRestOfHeader = "enhanced-foldout__header-rest";

        public readonly VisualElement header;
        public readonly VisualElement startOfHeader;
        public readonly VisualElement restOfHeader;
        public readonly Toggle toggle;
        public readonly Label label;

        public new string text
        {
            get => childCount > 0 ? base.text : label.text;
            set
            {
                base.text = value;
                label.text = value;
                EnableInClassList("no-text", string.IsNullOrEmpty(value));
            }
        }

        public new string tooltip
        {
            get => childCount > 0 ? toggle.tooltip : label.tooltip;
            set
            {
                toggle.tooltip = value;
                label.tooltip = value;
            }
        }

        public VisualElement GetCurrentLabel() => toggle.IsDisplayed() ? toggle : label;

        public EnhancedFoldout()
        {
            this.AddPosticaStyles();

            AddToClassList(ussEF);

            header = new VisualElement().WithClass(ussEFHeader);
            startOfHeader = new VisualElement().WithClass(ussEFStartOfHeader);
            restOfHeader = new VisualElement().WithClass(ussEFRestOfHeader);

            toggle = hierarchy.ElementAt(0) as Toggle;
            toggle.RemoveFromHierarchy();
            toggle.AddToClassList(ussEFLabel);

            label = new Label().WithClass(ussEFLabel, ussEFLabelFixed);

            hierarchy.Insert(0, header.WithChildren(startOfHeader.WithChildren(toggle, label), restOfHeader));

            RegisterCallback((EventCallback<GeometryChangedEvent>)(evt => UpdateLabels()));
        }

        public EnhancedFoldout MakeAsField()
        {
            startOfHeader.StyleAsFieldLabel();
            restOfHeader.StyleAsFieldInput();
            return this.StyleAsField();
        }

        public new void Add(VisualElement element)
        {
            base.Add(element);
            UpdateLabels();
        }
        
        public new void Clear()
        {
            base.Clear();
            UpdateLabels();
        }

        public new void Remove(VisualElement element)
        {

            base.Remove(element);
            UpdateLabels();
        }

        public new void Insert(int index, VisualElement element)
        {
            base.Insert(index, element);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            label.EnableInClassList("hidden", childCount > 0);
            toggle.EnableInClassList("hidden", childCount == 0);
        }
    }
}