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
        private bool _selected;
        public Action OnClick { get; set; }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0) && _selected)
            {
                OnClick?.Invoke();
            }
        }

        private void OnMouseEnter()
        {
            TooltipWindow.Add(this);
            Outline.enabled = true;
            _selected = true;
        }

        private void OnMouseExit()
        {
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
        }
        
        public string GetName()
        {
            return Name;
        }

        public string GetDescription()
        {
            return Description;
        }
    }
}