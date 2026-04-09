using System;
using Deckbuilding.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Common
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITooltip
    {
        public string Name;
        [Multiline]public string Description;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipWindow.Add(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipWindow.Remove(this);
        }

        protected void OnDisable()
        {
            TooltipWindow.Remove(this);
        }

        public string GetName()
        {
            return Name;
        }

        public string GetDescription()
        {
            return Description;
        }

        public Building GetBuilding()
        {
            return null;
        }
    }
}