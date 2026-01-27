using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyResources : MonoBehaviour
    {
		public int Amount = 5;
        public ResourcesEnum Resource;
        public TextMeshProUGUI Text;
        public Button Button;
        public int Cost = 10;

        public void OnEnable()
        {
            Button.onClick.AddListener(Buy);
            Text.text = $"Купить {Amount} " + GetName() + $" ({Cost} монет)";
        }

        public void OnDisable()
        {
            Button.onClick.RemoveAllListeners();
        }

        public void Update()
        {
            Button.interactable = PartyGold.Instance.Value >= Cost;
        }

        public void Buy()
        {
            PartyGold.Instance.Value -= Cost;

            if (Resource == ResourcesEnum.Fuel)
            {
                PartyFuel.Instance.Value += Amount;
            } 
            else if (Resource == ResourcesEnum.Supply)
            {
                PartySupply.Instance.Value += Amount;
            }
        }

        public string GetName()
        {
            if (Resource == ResourcesEnum.Fuel)
            {
                return "топлива";
            } 
            else if (Resource == ResourcesEnum.Supply)
            {
                return "припасов";
            }

            return null;
        }
    }
}