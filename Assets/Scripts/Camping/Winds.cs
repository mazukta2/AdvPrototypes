using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class Winds : MonoBehaviour
    {
        public float FuelDamagePerSecond = 1;
        public float Radius = 10;

        public void Update()
        {
            if (Vector3.Distance(PartyHealth.Instance.transform.position, transform.position) < Radius)
            {
                PartyFuel.Instance.ProgressValue = Mathf.MoveTowards(PartyFuel.Instance.ProgressValue, 0, FuelDamagePerSecond * Time.deltaTime);
            }
        }
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(this.transform.position, Radius);
        }
    }
}