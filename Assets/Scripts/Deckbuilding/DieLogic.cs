using System;
using Camping;
using Common;
using UnityEngine;

namespace Deckbuilding
{
    public class DieLogic : MonoBehaviour
    {
        public void Update()
        {
            if (PartyHealth.Instance == null)
                return;
            
            if (WinScreen.Instance.IsWin())
                return;

            if (PartyHealth.Instance.Value <= 0)
            {
                PartyHealth.Instance.Value = PartyHealth.Instance.Max;
                ChangePartyScreen.Instance.Show();
            }
        }

    }
}