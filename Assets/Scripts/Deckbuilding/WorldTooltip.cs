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
        private bool _isOverObject;
        private bool _isHiglighted;
        public bool Selected { get; set; }


        private void OnMouseEnter()
        {
            _isOverObject = true;
        }

        private void OnMouseExit()
        {
            _isOverObject = false;
        }

        protected void OnEnable()
        {
            Outline.enabled = false;
        }

        public void Update()
        {
            var isOverUI = (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject());
            
            var needToHighlight = !isOverUI && _isOverObject;
            if (_isHiglighted && !needToHighlight)
            {
                _isHiglighted = false;
                TooltipWindow.Remove(this);
                Outline.enabled = false;
                Selected = false;
                
            } else if (!_isHiglighted && needToHighlight)
            {
                _isHiglighted = true;
                TooltipWindow.Add(this);
                Outline.enabled = true;
                Selected = true;
            }
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