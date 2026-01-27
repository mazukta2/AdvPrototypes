using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyUpgrade : MonoBehaviour
    {
        public string Name;
        public TextMeshProUGUI Text;
        public Button Button;
        public UpgradeEnum UpgradeEnum;
        public int Cost = 10;

        public void OnEnable()
        {
            Button.onClick.AddListener(Buy);
            Text.text = Name + $" ({Cost} монет)";
        }

        public void OnDisable()
        {
            Button.onClick.RemoveAllListeners();
        }

        public void Update()
        {
            Button.interactable = PartyGold.Instance.Value >= Cost;
            Button.gameObject.SetActive(!PartyCamp.Instance.WasBuild(UpgradeEnum));
        }

        public void Buy()
        {
            PartyGold.Instance.Value -= Cost;
            PartyCamp.Instance.Build(UpgradeEnum);
        }
    }
}