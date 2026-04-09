using System;
using Common;
using Deckbuilding.Buildings;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deckbuilding.Interactables
{
    public class WorldTooltip : ListMonoBehavior<WorldTooltip>, ITooltip
    {
        public string Name;
        [Multiline] public string Description;
        public Outline Outline;
        public bool Selected;

        private void OnMouseEnter()
        {
            if (!this.enabled)
                return;
            TooltipWindow.Add(this);
            Outline.enabled = true;
            Selected = true;
        }

        private void OnMouseExit()
        {
            if (!this.enabled)
                return;
            
            TooltipWindow.Remove(this);
            Outline.enabled = false;
            Selected = false;
        }

        protected void OnEnable()
        {
            Outline.enabled = false;
        }

        protected void OnDisable()
        {
            TooltipWindow.Remove(this);
            Outline.enabled = false;
            Selected = false;
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