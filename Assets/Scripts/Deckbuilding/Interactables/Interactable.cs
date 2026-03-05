using System;
using Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deckbuilding.Interactables
{
    public class Interactable : ListMonoBehavior<Interactable>, ITooltip
    {
        public string Name;
        [Multiline] public string Description;
        public Outline Outline;
        public float Distance = 20;
        public bool Selected;
        public Action OnReaching { get; set; }


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

        public void InteractOnEndOfMovement()
        {
            if (!this.enabled)
                return;
            if (PartyMembers.Instance.SelectedMember != null)
            {
                PartyMembers.Instance.Interact(PartyMembers.Instance.SelectedMember, this);
                PartyMembers.Instance.SelectedMember = null;
            }
            else
            {
                OnReaching?.Invoke();
            }
            
            
        }
    }
}