using System;
using Common;
using Deckbuilding.Interactables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Windows
{
    public class LighthouseWindow : Window<LighthouseWindow>
    {
        public TextMeshProUGUI Cost;
        public Button Pay;
        public TextMeshProUGUI Decription;
        [Multiline] public string TurnOnDecriptionText;
        [Multiline] public string TurnOffDecriptionText;
        private Lighthouse _instance;

        public void OnEnable()
        {
            Cost.text = GameSettings.Instance.GatesCost.ToString();
            
            Pay.onClick.AddListener(() =>
            {
                PartyResources.Instance.Change(PartyResources.ResourceType.Gold, -GameSettings.Instance.GatesCost);
                _instance.TurnOn();
                Close();
            });
            
        }

        protected void Update()
        {
            Pay.interactable = !_instance.IsTurnOn() && PartyResources.Instance.Get(PartyResources.ResourceType.Gold) >= GameSettings.Instance.GatesCost;
            
            if (_instance.IsTurnOn())
            {
                Decription.text = TurnOnDecriptionText;
            }
            else
            {
                Decription.text = TurnOffDecriptionText;
            }
        }

        public void Set(Lighthouse instance)
        {
            _instance = instance;
        }
    }
}