using System;
using Common;
using Deckbuilding.Buildings;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deckbuilding.Interactables
{
    public class Interactable : ListMonoBehavior<Interactable>, ITooltip
    {
        public BuildingTypes BuildingType;
        public string Name;
        [Multiline] public string Description;
        public Outline Outline;
        public float Distance = 20;
        public bool Selected;
        private bool _isOverObject;
        private bool _isHiglighted;
        public Action OnReaching { get; set; }
        public Building Building { get; set; }


        private void OnMouseEnter()
        {
            _isOverObject = true;
        }

        private void OnMouseExit()
        {
            _isOverObject = false;
            
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
            return Building;
        }

        public void InteractOnEndOfMovement()
        {
            if (!this.enabled)
                return;
            
            OnReaching?.Invoke();
        }

        public BuildingTypes GetBuidingType()
        {
            return BuildingType;
        }

        public void RebuildTooltip()
        {
            if (_isHiglighted)
            {
                TooltipWindow.Remove(this);
                TooltipWindow.Add(this);
            }
        }
    }
}