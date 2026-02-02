using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyExitGold : MonoBehaviour
    {
        public Button Button;
        public int Cost = 20;

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
            Button.interactable = !WinScreen.Instance.IsWin() && PartyGold.Instance.Value >= Cost;
        }

        public void Buy()
        {
            PartyGold.Instance.Value -= Cost;
            WinScreen.Instance.Show();
        }
    }
}