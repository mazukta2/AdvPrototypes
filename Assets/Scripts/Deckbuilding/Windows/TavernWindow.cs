using System;
using Common;
using Deckbuilding.Interactables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Windows
{
    public class TavernsWindow : Window<TavernsWindow>
    {
        public Button Rest;

        public void OnEnable()
        {
            Rest.onClick.RemoveAllListeners();
            Rest.onClick.AddListener(() =>
            {
                ChangePartyScreen.Instance.EndSeason();
                Close();
            });
            
        }

    }
}