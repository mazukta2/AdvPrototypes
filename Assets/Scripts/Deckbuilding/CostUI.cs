using System;
using TMPro;
using UnityEngine;

namespace Deckbuilding
{
    public class CostUI : MonoBehaviour
    {
        public string Name;
        public PartyResources.ResourceType ResourceType;
        public int Cost;
        
        public TextMeshProUGUI CostText;

        public void OnEnable()
        {
            CostText.text = $"{Name} {Cost} {GetResourceText(ResourceType)}";
        }

        public string GetResourceText(PartyResources.ResourceType resourceType)
        {
            return resourceType switch
            {
                PartyResources.ResourceType.Fuel => "Топливо",
                PartyResources.ResourceType.Gold => "Золото",
                PartyResources.ResourceType.Report => "Отчеты",
                _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
            };

        }

        public bool IsValid()
        {
            return PartyResources.Instance.Get(ResourceType) >= Cost;
        }

        public void Take()
        {
            PartyResources.Instance.Change(ResourceType, -Cost);
        }
    }
}