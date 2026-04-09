using UnityEngine;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class SpawnEnemies : IBuildingAction
    {
        public int EnemyCount;
        
        public void Execute(Building building)
        {
            for (int i = 0; i < EnemyCount; i++)
            {
                EnemySpawner.Instance.Spawn(building.transform.position);
            }
        }

        public object[] GetParameters()
        {
            return new object[] {EnemyCount};
        }
    }
}