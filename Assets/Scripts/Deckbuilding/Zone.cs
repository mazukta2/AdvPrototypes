using System;
using Common;
using UnityEngine;
using UnityEngine.AI;

namespace Deckbuilding
{
    public class Zone : ListMonoBehavior<Zone>
    {
        public string Name;
        public float Radius = 10f;
        public bool Explored = false;
        public int DangerLevel = 2;
        public GameObject EnemyPrefab;

        public void Update()
        {
            if (!Explored && Vector3.Distance(PartyMovement.Instance.transform.position, transform.position) < Radius)
            {
                Explored = true;
                //PartyResources.Instance.Change(PartyResources.ResourceType.Report, 1);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(this.transform.position, Radius);
        }
        
        public Zone GetCurrentZone()
        {
            foreach (var zone in List)
            {
                if (Vector3.Distance(PartyMovement.Instance.transform.position, zone.transform.position) < zone.Radius)
                {
                    return zone;
                }
            }
            return null;
        }


        public bool IsInside(GameObject go)
        {
            if (Vector3.Distance(go.transform.position, transform.position) < Radius)
            {
                return true;
            }

            return false;
        }

        public static void NewSeason()
        {
            foreach (var zone in List)
            {
                if (zone.Explored)
                {
                    zone.Explored = false;
                    //PartyResources.Instance.Change(PartyResources.ResourceType.Gold, 1);
                }
                for (int i = 0; i < zone.DangerLevel; i++)
                {
                    // spawn enemy and attach to navmesh inside radius of zone
                    if (zone.EnemyPrefab != null)
                    {
                        // Random position inside the zone's radius
                        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * zone.Radius /2f;
                        Vector3 randomPosition = zone.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                        // Find nearest NavMesh position
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomPosition, out hit, 50, NavMesh.AllAreas))
                        {
                            GameObject enemy = UnityEngine.Object.Instantiate(zone.EnemyPrefab, hit.position, Quaternion.identity);
                            // Optionally, set up enemy (e.g., assign zone reference, initialize AI, etc.)
                        }
                        else
                        {
                            // Could not find NavMesh position, optionally log or handle
                        }
                    }
                }
            }
            //PartyResources.Instance.Set(PartyResources.ResourceType.Report, 0);

        }
    }
}