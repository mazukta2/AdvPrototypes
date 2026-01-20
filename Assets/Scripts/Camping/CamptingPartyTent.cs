using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class CamptingPartyTent : MonoBehaviour
    {
        public float HealthRestoreRate = 1f;
        public void Update()
        {
            if (!PartyCamp.Instance.IsCampling)
                return;
            
            if (PartyFuel.Instance.Value == 0)
                return;
            
            if (PartySupply.Instance.Value == 0)
                return;


            if (!PartyCamp.Instance.WasBuild(BuildingsEnum.Tent))
                return;
            
            PartyHealth.Instance.Value = Mathf.MoveTowards(PartyHealth.Instance.Value, PartyHealth.Instance.Max, HealthRestoreRate * Time.deltaTime);
        }
    }
}