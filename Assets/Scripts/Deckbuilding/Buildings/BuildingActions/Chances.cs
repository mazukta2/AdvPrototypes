using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class Chances : IBuildingAction
    {
        [SerializeReference] public List<ChanceElement> Value;
        [SerializeField] public int ChanceOfNothing;

        public void Execute(Building building)
        {
            var max = Value.Sum(x => x.Chance) + ChanceOfNothing;
            var roll = UnityEngine.Random.Range(0, max);
            var current = 0;
            foreach (var chanceElement in Value)
            {
                current += chanceElement.Chance;
                if (roll < current)
                {
                    chanceElement.Action.Execute(building);
                    return;
                }
            }
        }

        public object[] GetParameters()
        {
            return new object[] {};
        }
        
        [Serializable]
        public class ChanceElement
        {
            [SerializeReference] public IBuildingAction Action;
            [SerializeField] public int Chance;
        }
    }
}