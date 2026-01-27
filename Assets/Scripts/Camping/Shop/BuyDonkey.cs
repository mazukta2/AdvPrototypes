using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyDonkey : MonoBehaviour
    {
		public int Amount = 5;
        public TextMeshProUGUI Text;
        public Button Button;
        public int Cost = 10;

        public void OnEnable()
        {
            Button.onClick.AddListener(Buy);
            Text.text = $"Купить ослика ({Cost} монет)";
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

            PartyGold.Instance.MaximiumValue += Amount;
            DonkeySpawner.Instance.SpawnDonkey();
        }
    }
}