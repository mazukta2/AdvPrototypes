using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class FoodResource : CampingResourceBase
    {
        public float BuildingModificator = 2f;
        public override string GetName()
        {
            return "Еда:";
        }

        public override void TakeResource()
        {
            PartySupply.Instance.Value++;
        }

        public override float GetProgressModificator()
        {
            if (PartyCamp.Instance.WasBuild(BuildingsEnum.Kitchen))
                return BuildingModificator;
            return 1f;
        }
    }
}