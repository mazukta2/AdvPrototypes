using System;
using Common;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Sawmill : ListMonoBehavior<Sawmill>
    {
        public int FuelPerSeason;
        public GameObject ClaimObject;
        
        public void OnEnable()
        {
            GetComponent<Interactable>().OnReaching = () =>
            {
                SawmillWindow.Instance.Open((i) => i.Set(this));
            };
        }

        public bool Claim { get; set; }


        public static void NewSeason()
        {
            foreach (var sawmill in List)
            {
                if (sawmill.Claim)
                {
                    PartyResources.Instance.Change(PartyResources.ResourceType.Fuel, sawmill.FuelPerSeason);
                }
            }
        }
    }
}