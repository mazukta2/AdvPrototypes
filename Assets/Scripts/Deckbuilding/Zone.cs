using System;
using Common;
using UnityEngine;

namespace Deckbuilding
{
    public class Zone : ListMonoBehavior<Zone>
    {
        public float Radius = 10f;
        public bool Explored = false;

        public void Update()
        {
            if (!Explored && Vector3.Distance(PartyMovement.Instance.transform.position, transform.position) < Radius)
            {
                Explored = true;
                PartyResources.Instance.Change(PartyResources.ResourceType.Report, 1);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(this.transform.position, Radius);
        }

        public static void NewSeason()
        {
            foreach (var zone in List)
            {
                if (zone.Explored)
                {
                    zone.Explored = false;
                    PartyResources.Instance.Change(PartyResources.ResourceType.Gold, 1);
                }
            }
            PartyResources.Instance.Set(PartyResources.ResourceType.Report, 0);
        }
    }
}