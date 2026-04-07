using System.Collections.Generic;
using System.Linq;
using Deckbuilding.Windows;
using TMPro;
using UnityEngine;

namespace Common
{
    public class TooltipWindow : SingletonMonoBehavior<TooltipWindow>
    {
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Description;
        public GameObject TagPrefab;
        public GameObject TagContainer;

        private List<ITooltip> _list = new List<ITooltip>();

        public static void Add(ITooltip tooltip)
        {
            Instance._list.Add(tooltip);
            Instance.UpdateText();
        }
        
        public static void Remove(ITooltip tooltip)
        {
            Instance?._list?.Remove(tooltip);
            Instance?.UpdateText();
        }

        private  void UpdateText()
        {
            if (_list.Count == 0)
            {
                Name.text = "";
                Description.text  = "";
                foreach (Transform child in TagContainer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            else
            {
                var instance = _list.First();
                Name.text  = instance.GetName();
                Description.text  = instance.GetDescription();
                
                foreach (Transform child in TagContainer.transform)
                {
                    Destroy(child.gameObject);
                }

                if (instance.GetTags() != null)
                {
                    foreach (var tagData in instance.GetTags())
                    {
                        var go = Instantiate(TagPrefab, TagContainer.transform); 
                        var tagComponent = go.GetComponent<BuildingWindowTag>();
                        tagComponent.Text.text = tagData.TagName;
                        tagComponent.Background.color = tagData.Color;
                        if (tagComponent.Tooltip != null)
                        {
                            tagComponent.Tooltip.Name = tagData.TagName;
                            tagComponent.Tooltip.Description = tagData.TagDescription;
                        }
                    }
                }
                
            }
        }

    }
}