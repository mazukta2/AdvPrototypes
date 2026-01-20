using System;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class BuyBuildingAvalability : MonoBehaviour
    {
        public Button Button;
        public BuildingsEnum BuildingType;

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
            Button.interactable = PartyGold.Instance.Value > 0;
            Button.gameObject.SetActive(!PartyCamp.Instance.IsAvailable(BuildingType));
        }

        public void Buy()
        {
            PartyGold.Instance.Value--;
            PartyCamp.Instance.SetAvailable(BuildingType);
        }
    }
}