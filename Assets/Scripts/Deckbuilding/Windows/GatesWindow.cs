using System;
using Common;
using Deckbuilding.Interactables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Windows
{
    public class GatesWindow : Window<GatesWindow>
    {
        public TextMeshProUGUI Cost;
        public Button Pay;
        private Gates _gates;

        public void OnEnable()
        {
            Cost.text = GameSettings.Instance.GatesCost.ToString();
            
            Pay.onClick.AddListener(() =>
            {
                PartyResources.Instance.Change(PartyResources.ResourceType.Gold, -GameSettings.Instance.GatesCost);
                _gates.OpenGates();
                Close();
            });
            
        }

        protected void Update()
        {
            Pay.interactable = PartyResources.Instance.Get(PartyResources.ResourceType.Gold) >= GameSettings.Instance.GatesCost;
        }

        public void Set(Gates gates)
        {
            _gates = gates;
        }
    }
}