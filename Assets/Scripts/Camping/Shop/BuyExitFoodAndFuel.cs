using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyExitFoodAndFuel : MonoBehaviour
    {
        public Button Button;
        public int Cost = 10;

        public void OnEnable()
        {
            Button.onClick.AddListener(Buy);
        }

        public void OnDisable()
        {
            Button.onClick.RemoveAllListeners();
        }

        public void Update()
        {
            Button.interactable = !WinScreen.Instance.IsWin() && PartyFuel.Instance.Value >= Cost && PartySupply.Instance.Value >= Cost;
        }

        public void Buy()
        {
            PartyFuel.Instance.Value -= Cost;
            PartySupply.Instance.Value -= Cost;
            WinScreen.Instance.Show();
        }
    }
}