using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class FoodResource : CampingResourceBase
    {
        public override string GetName()
        {
            return "Еда:";
        }

        public override void TakeResource()
        {
            PartySupply.Instance.Value++;
        }
    }
}