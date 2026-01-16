using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class FuelResource : CampingResourceBase
    {
        public override string GetName()
        {
            return "Топливо:";
        }

        public override void TakeResource()
        {
            PartyFuel.Instance.Value++;
        }
    }
}