using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class GoldResource : CampingResourceBase
    {
        public override string GetName()
        {
            return "Золото:";
        }

        public override void TakeResource()
        {
            PartyGold.Instance.Value++;
        }
        
        public override float GetProgressModificator()
        {
            return 1f;
        }

        public override bool CanTakeResource()
        {
            return PartyGold.Instance.Value < PartyGold.Instance.MaximiumValue;
        }
    }
}