using System;
using Common;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuildingButton : MonoBehaviour
    {
        public BuildingsEnum BuildingType;
        public Button Button;
        public Tooltip Tooltip;
        public Color BuilddedColor;
        public string BuildingBuildedName;
        private LayoutElement _layoutElement;

        protected void OnEnable()
        {
            Button.onClick.AddListener(Click);
            _layoutElement = GetComponent<LayoutElement>();
        }


        protected void OnDisable()
        {
            Button.onClick.RemoveListener(Click);
        }

        protected void Update()
        {
            Button.interactable = PartyTools.Instance.Value > 0 && !PartyCamp.Instance.WasBuild(BuildingType);

            if (PartyCamp.Instance.IsAvailable(BuildingType))
            {
                _layoutElement.ignoreLayout = false;
                Button.gameObject.SetActive(true);
            }
            else
            {
                _layoutElement.ignoreLayout = true;
                Button.gameObject.SetActive(false);
            }
            

            if (PartyCamp.Instance.WasBuild(BuildingType))
            {
                Button.transition = Selectable.Transition.None;
                Button.targetGraphic.color = BuilddedColor;
                Tooltip.Name = BuildingBuildedName;
            }
        }

        private void Click()
        {
            PartyTools.Instance.Value--;
            PartyCamp.Instance.Build(BuildingType);
        }
    }
}