using UnityEngine;

namespace Deckbuilding.Buildings
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "ScriptableObjects/BuildingData", order = 1)]
    public class BuildingData : ScriptableObject
    {
        public string BuidlingName;
        [Multiline]public string BuidlingDescription;
        [SerializeReference] public IBuildingOption[] Options;
    }
}