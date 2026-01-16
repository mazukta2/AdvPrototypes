using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public class DropdownPreviewItem : SmartDropdown.IBuildingBlock
    {
        protected string _name;
        protected string _path;
        protected Type _type;
        protected string _description;
        protected string _secondLabel;
        protected VisualElement _searchElement;
        protected readonly Action _onSelectCallback;
        protected readonly bool _alterGroup;
        protected readonly object _value;

        protected Texture2D _icon;

        protected SearchTags _searchTags;

        public string Name => _name;
        public SearchTags SearchTags => _searchTags;

        public string Description
        {
            get => _description;
            set => _description = value;
        }
        
        public string SecondLabel
        {
            get => _secondLabel;
            set => _secondLabel = value;
        }

        public static bool CanPreview(object value) => CanPreview(value?.GetType());

        public static bool CanPreview(Type type)
        {
            return type == typeof(Color)
                   || type == typeof(float)
                   || type == typeof(double)
                   || type == typeof(int)
                   || type == typeof(short)
                   || type == typeof(long)
                   || type == typeof(bool)
                   || type == typeof(string)
                   || type == typeof(Gradient)
                   || type == typeof(AnimationCurve)
                   || type == typeof(Vector2)
                   || type == typeof(Vector3)
                   || type == typeof(Vector4)
                   || type == typeof(Vector2Int)
                   || type == typeof(Vector3Int)
                   || type?.IsEnum == true
                   || typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        public DropdownPreviewItem(string name, Action onSelectCallback, object value, bool canAlterGroup = true)
            : this(name, null, onSelectCallback, value, null, canAlterGroup)
        {
        }

        public DropdownPreviewItem(string name, Type type, Action onSelectCallback, object value,
            string description = null, bool canAlterGroup = true)
        {
            _name = name;
            _description = description;
            _onSelectCallback = onSelectCallback ?? throw new ArgumentNullException(nameof(onSelectCallback));
            _searchTags = name;
            _secondLabel = type?.GetAliasName();
            _alterGroup = canAlterGroup;
            _value = value;
            _type = type;
        }

        public virtual VisualElement GetDrawer(Action closeWindow, bool darkMode)
        {
            var ve = new VisualElement();
            ve.AddToClassList("sd-dropdown-preview-item");
            ve.AddToClassList("sd-clickable");

            var image = new Image();
            image.AddToClassList("sd-icon");
            image.AddToClassList("sd-image");

            Texture2D icon = null;
            var valuePreview = GetValuePreview(ref icon, _value, _type);

            _icon = icon;

            image.image = icon;
            image.EnableInClassList("sd-has-value", icon);

            var border = new VisualElement();
            border.AddToClassList("sd-border");
            image.Add(border);

            ve.Add(image);

            if (valuePreview != null)
            {
                ve.Add(valuePreview.WithClass("sd-value-field", "sd-value-field--" + _value?.GetType().GetAliasName()));
            }

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

            ve.AddManipulator(new Clickable(_onSelectCallback + closeWindow));
            return ve;
        }

        public static VisualElement GetValuePreview(ref Texture2D icon, object value, Type type)
        {
            VisualElement valuePreview = null;
            if (value?.GetType().IsEnum == true)
            {
                valuePreview = new Label(value.ToString()).WithClass("sd-enum-value");
                return valuePreview.WithoutClass(BaseField<int>.ussClassName);
            }
            switch (value)
            {
                case UnityEngine.Object obj:
                    icon = AssetPreview.GetMiniThumbnail(obj);
                    if (!obj)
                    {
                        icon = ObjectIcon.GetFor(type ?? obj.GetType());
                    }
                    break;
                case Color color:
                {
                    valuePreview = new ColorField { value = color, showAlpha = true, showEyeDropper = false, hdr = color.maxColorComponent > 1 };
                    break;
                }
                case short s:
                    valuePreview = new Label(s.ToString());
                    break;
                case float f:
                    valuePreview = new Label(f.ToString("F2"));
                    break;
                case double d:
                    valuePreview = new Label(d.ToString("F2"));
                    break;
                case int i:
                    valuePreview = new Label(i.ToString());
                    break;
                case long l:
                    valuePreview = new Label(l.ToString());
                    break;
                case string str:
                    valuePreview = new Label(str);
                    break;
                case bool b:
                    valuePreview = new Label(b ? "True" : "False").WithClassEnabled("sd-bool-true", b);
                    break;
                case Gradient gradient:
                    valuePreview = new GradientField(){value = gradient, hdr = gradient.colorKeys.Any(k => k.color.maxColorComponent > 1)};
                    break;
                case AnimationCurve curve:
                    valuePreview = new CurveField(){value = curve};
                    break;
                case Vector2 v:
                    valuePreview = new VisualElement().WithClass("sd-vector-value").WithChildren(
                        new Label(v.x.ToString("F2")).WithClass("vector-component", "vector-component--x"),
                        new Label(v.y.ToString("F2")).WithClass("vector-component", "vector-component--y"));
                    break;
                case Vector3 v:
                    valuePreview = new VisualElement().WithClass("sd-vector-value").WithChildren(
                        new Label(v.x.ToString("F2")).WithClass("vector-component", "vector-component--x"),
                        new Label(v.y.ToString("F2")).WithClass("vector-component", "vector-component--y"),
                        new Label(v.z.ToString("F2")).WithClass("vector-component", "vector-component--z"));
                    break;
                case Vector4 v:
                    valuePreview = new VisualElement().WithClass("sd-vector-value").WithChildren(
                        new Label(v.x.ToString("F2")).WithClass("vector-component", "vector-component--x"),
                        new Label(v.y.ToString("F2")).WithClass("vector-component", "vector-component--y"),
                        new Label(v.z.ToString("F2")).WithClass("vector-component", "vector-component--z"),
                        new Label(v.w.ToString("F2")).WithClass("vector-component", "vector-component--w"));
                    break;
                case Vector3Int v:
                    valuePreview = new VisualElement().WithClass("sd-vector-value").WithChildren(
                        new Label(v.x.ToString("F2")).WithClass("vector-component", "vector-component--x"),
                        new Label(v.y.ToString("F2")).WithClass("vector-component", "vector-component--y"),
                        new Label(v.z.ToString("F2")).WithClass("vector-component", "vector-component--z"));
                    break;
                case Vector2Int v:
                    valuePreview = new VisualElement().WithClass("sd-vector-value").WithChildren(
                        new Label(v.x.ToString("F2")).WithClass("vector-component", "vector-component--x"),
                        new Label(v.y.ToString("F2")).WithClass("vector-component", "vector-component--y"));
                    break;
                case null when type != null:
                    icon = ObjectIcon.GetFor(type);
                    break;
                default:
                    valuePreview = new Label("null").WithClass("sd-null-value");
                    break;
            }

            return valuePreview.WithoutClass(BaseField<int>.ussClassName);
        }

        public virtual void OnPathResolved(SmartDropdown.IPathNode node)
        {
            _path = node.Path;
            // Update search tags
            if (string.IsNullOrEmpty(_name))
            {
                _searchTags = _name = node.Name;
            }

            if (_alterGroup
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