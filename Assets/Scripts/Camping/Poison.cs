using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class Poison : MonoBehaviour
    {
        public float DamagePerSecond = 1;
        public float FuelDamagePerSecond = 1;
        public float Radius = 10;

        public void Update()
        {
            if (Vector3.Distance(PartyHealth.Instance.transform.position, transform.position) < Radius)
            {
                if (PartyCamp.Instance.WasBuild(UpgradeEnum.Gasmask))
                {
                    PartyFuel.Instance.ProgressValue = Mathf.MoveTowards(PartyFuel.Instance.ProgressValue, 0, FuelDamagePerSecond * Time.deltaTime);
                }
                else
                {
                    PartyHealth.Instance.Value -= DamagePerSecond * Time.deltaTime;
                }
            }
        }
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(this.transform.position, Radius);
        }
    }
}