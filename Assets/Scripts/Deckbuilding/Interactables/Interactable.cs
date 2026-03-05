using System;
using Common;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Interactable : MonoBehaviour, ITooltip
    {
        public string Name;
        [Multiline] public string Description;
        public Outline Outline;
        public float Distance = 20;
        private bool _selected;
        public Action OnReaching { get; set; }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0) && _selected)
            {
                PartyMovement.Instance.Set(this, Distance);
            }
        }

        private void OnMouseEnter()
        {
            if (!this.enabled)
                return;
            TooltipWindow.Add(this);
            Outline.enabled = true;
            _selected = true;
        }

        private void OnMouseExit()
        {
            if (!this.enabled)
                return;
            
            TooltipWindow.Remove(this);
            Outline.enabled = false;
            _selected = false;
        }

        protected void OnEnable()
        {
            Outline.enabled = false;
        }

        protected void OnDisable()
        {
            TooltipWindow.Remove(this);
            Outline.enabled = false;
            _selected = false;
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
            OnReaching?.Invoke();
        }
    }
}