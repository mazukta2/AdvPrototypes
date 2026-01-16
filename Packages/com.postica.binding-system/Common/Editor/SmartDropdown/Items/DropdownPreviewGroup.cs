using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.Common
{

    public class DropdownPreviewGroup
    {
        private readonly Type _type;
        private readonly string _description;
        private readonly bool _oneLine;
        
        private static Texture2D _gameObjectIcon;
        private static Texture2D _genericObjectIcon;
        private static Texture2D GameObjectIcon => _gameObjectIcon ??= EditorGUIUtility.IconContent("GameObject Icon").image as Texture2D;
        private static Texture2D GenericObjectIcon => _genericObjectIcon ??= ObjectIcon.GetFor<Object>();

        public object PreviewValue { get; set; }

        public DropdownPreviewGroup(Type type, object previewValue, bool oneLine = false)
        {
            _type = type;
            _oneLine = oneLine;
            _description = type?.GetAliasName();
            PreviewValue = previewValue;
        }
        
        public DropdownPreviewGroup(string description, Type type, object previewValue, bool oneLine = false)
        {
            _type = type;
            _oneLine = oneLine;
            _description = description;
            PreviewValue = previewValue;
        }

        public VisualElement CreateGroupUI(SmartDropdown.IPathGroup group, Action onClick, Action closeWindow, bool isDarkSkin)
        {
            Texture2D icon = null;
            var deferredPreview = false;
            var unityObject = default(UnityEngine.Object);
            
            var groupUI = new VisualElement();
            groupUI.AddToClassList("sd-preview-group");
            groupUI.EnableInClassList("sd-preview-group--one-line", _oneLine);

            var imagePreview = new Image().WithClass("sd-icon");
            VisualElement valueField = null;

            if (PreviewValue is UnityEngine.Object obj)
            {
                if (!obj)
                {
                    imagePreview.image = ObjectIcon.GetFor(_type ?? obj.GetType());
                }
                else
                {
                    icon = AssetPreview.GetAssetPreview(obj);

                    if (!icon && AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
                    {
                        deferredPreview = true;
                        unityObject = obj;
                    }
                    else
                    {
                        imagePreview.image = icon ? icon : AssetPreview.GetMiniThumbnail(obj);
                    }

                    if (obj is Component c && imagePreview.image == GameObjectIcon)
                    {
                        imagePreview.image = ObjectIcon.GetFor(c);
                    }
                    else if (imagePreview.image == GenericObjectIcon)
                    {
                        imagePreview.image = ObjectIcon.GetFor(obj);
                    }
                }
                
                imagePreview.EnableInClassList("sd-has-value", true);
            }
            else
            {
                valueField = DropdownPreviewItem.GetValuePreview(ref icon, PreviewValue, _type);
            }
            

            var ve = new VisualElement();
            ve.AddToClassList("sd-clickable");
            ve.AddToClassList("sd-group");

            if(valueField != null)
            {
                ve.Add(valueField.WithClass("sd-value-field", "sd-value-field--" + PreviewValue?.GetType().GetAliasName()));
            }
            else
            {
                ve.Add(imagePreview);
                if(imagePreview.image)
                {
                    group.Icon = imagePreview.image as Texture2D;
                }
            }

            if (deferredPreview)
            {
                LoadDeferredPreview(imagePreview, unityObject, group);
            }

            var labels = new VisualElement().WithClass("sd-label-container");

            labels.Add(new Label(group.Name).WithClass("sd-label", "sd-label-main"));
            labels.Add(new Label(group.Description ?? _description ?? _type?.GetAliasName()).WithClass("sd-label", "sd-label-description"));

            ve.Add(labels);

            var rightArrow = new Label(@"›");
            rightArrow.AddToClassList("sd-right-arrow");
            ve.Add(rightArrow);

            ve.AddManipulator(new Clickable(onClick));

            groupUI.Add(ve);

            return groupUI;
        }

        private async void LoadDeferredPreview(Image imagePreview, UnityEngine.Object unityObject, SmartDropdown.IPathGroup group)
        {
            var icon = AssetPreview.GetAssetPreview(unityObject);
            var attempts = 20;
            while(!icon && AssetPreview.IsLoadingAssetPreview(unityObject.GetInstanceID()) && attempts-- > 0)
            {
                await Task.Delay(50);
                icon = AssetPreview.GetAssetPreview(unityObject);
            }

            imagePreview.image = icon ? icon : AssetPreview.GetMiniThumbnail(unityObject);
            
            if (unityObject is Component c && imagePreview.image == GameObjectIcon)
            {
                imagePreview.image = ObjectIcon.GetFor(c);
            }
            else if (imagePreview.image == GenericObjectIcon)
            {
                imagePreview.image = ObjectIcon.GetFor(unityObject);
            }

            if (imagePreview.image)
            {
                group.Icon = imagePreview.image as Texture2D;
            }
        }
    }

}