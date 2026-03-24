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
        [Multiline] public string DestroyedDescriptionText;
        private Lighthouse _instance;

        public void OnEnable()
        {
            Cost.text = GameSettings.Instance.GatesCost.ToString();
            
            Pay.onClick.RemoveAllListeners();
            Pay.onClick.AddListener(() =>
            {
                PartyResources.Instance.Change(PartyResources.ResourceType.Fuel, -GameSettings.Instance.GatesCost);
                _instance.TurnOn();
                Close();
            });
        }

        protected void Update()
        {
            Pay.interactable = !_instance.IsTurnOn() && !_instance.IsDestroyed() && PartyResources.Instance.Get(PartyResources.ResourceType.Fuel) >= GameSettings.Instance.GatesCost;
            
            if (_instance.IsTurnOn())
            {
                Decription.text = TurnOnDecriptionText;
            }
            else
            {
                Decription.text = TurnOffDecriptionText;
            }

            if (_instance.IsDestroyed())
            {
                Decription.text = DestroyedDescriptionText;
            }
        }

        public void Set(Lighthouse instance)
        {
            _instance = instance;
        }
    }
}