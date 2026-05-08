using System.Collections.Generic;
using System.Linq;
using Deckbuilding.Buildings;
using UnityEngine;

namespace Deckbuilding.BuildingRules
{
    [CreateAssetMenu(fileName = "BuildingCombinationRule", menuName = "Deckbuilding/BuildingCombinationRule")]
    public class BuildingCombinationRule : ScriptableObject
    {
        [SerializeField] public bool Disable;
        
        [SerializeField] public TagData[] With;
        [SerializeField] public TagData[] Without;
        
        [SerializeReference] public IBuildingOption[] Options;
        
        [SerializeReference] public IBuildingAction[] OnChangeSeason;
        
        public void HandleSeasonChange(Building building)
        {
            if (Disable)
                return;
            
            foreach (var tagData in With)
            {
                if (!building.Tags.Contains(tagData))
                {
                    return;
                }
            }

            foreach (var tagData in Without)
            {
                if (building.Tags.Contains(tagData))
                {
                    return;
                }
            }
            
            foreach (var buildingAction in OnChangeSeason)
            {
                buildingAction.Execute(building);
            }
        }

        public IEnumerable<IBuildingOption> GetOptionsForBuilding(Building building)
        {
            if (Disable)
                return Enumerable.Empty<IBuildingOption>();
            
            
            foreach (var tagData in With)
            {
                if (!building.Tags.Contains(tagData))
                {
                    return Enumerable.Empty<IBuildingOption>();
                }
            }

            foreach (var tagData in Without)
            {
                if (building.Tags.Contains(tagData))
                {
                    return Enumerable.Empty<IBuildingOption>();
                }
            }
            
            return Options;
        }
    }
}