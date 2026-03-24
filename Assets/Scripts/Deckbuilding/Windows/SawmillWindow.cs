using System;
using Common;
using Deckbuilding.Interactables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Windows
{
    public class SawmillWindow : Window<SawmillWindow>
    {
        public Button Claim;
        private Sawmill _instance;
        public CostUI Cost;

        public void OnEnable()
        {
            Claim.onClick.RemoveAllListeners();
            Claim.onClick.AddListener(() =>
            {
                _instance.Claim = true;
                _instance.ClaimObject.SetActive(true);
                Cost.Take();
                Close();
            });
            
        }

        public void Update()
        {
            if (_instance == null) return;
            Claim.interactable = !_instance.Claim && Cost.IsValid();
        }

        public void Set(Sawmill sawmill)
        {
            _instance = sawmill;
        }
    }
}