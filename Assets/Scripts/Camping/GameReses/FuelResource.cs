using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class FuelResource : CampingResourceBase
    {
        public float BuildingModificator = 2f;
        public override string GetName()
        {
            return "Топливо:";
        }

        public override void TakeResource()
        {
            PartyFuel.Instance.Value++;
        }
        
        public override float GetProgressModificator()
        {
            if (PartyCamp.Instance.WasBuild(BuildingsEnum.Woodwork))
                return BuildingModificator;
            return 1f;
        }
    }
}