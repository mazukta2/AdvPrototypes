using Common;
using UnityEngine;
using UnityEngine.AI;

namespace Deckbuilding
{
    public class EnemySpawner : SingletonMonoBehavior<EnemySpawner>
    {
        public GameObject EnemyPrefab;
        public float Radius = 40f;

        public void Spawn(Vector3 position)
        {
            
            // spawn enemy and attach to navmesh inside radius of zone
            if (EnemyPrefab != null)
            {
                // Random position inside the zone's radius
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Radius /2f;
                Vector3 randomPosition = position + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Find nearest NavMesh position
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPosition, out hit, 50, NavMesh.AllAreas))
                {
                    var enemy = UnityEngine.Object.Instantiate(EnemyPrefab, hit.position, Quaternion.identity);
                    // Optionally, set up enemy (e.g., assign zone reference, initialize AI, etc.)
                }
                else
                {
                    // Could not find NavMesh position, optionally log or handle
                }
            }
        }
    }
}