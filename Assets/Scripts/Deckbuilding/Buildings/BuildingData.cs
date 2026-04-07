using System.Collections;
using UnityEngine;

namespace Deckbuilding.Buildings
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "ScriptableObjects/BuildingData", order = 1)]
    public class BuildingData : ScriptableObject
    {
        public string BuidlingName;
        [Multiline]public string BuidlingShortDescription;
        [Multiline]public string BuidlingDescription;
        public TagData[] Tags;
        [SerializeReference] public IBuildingOption[] Options;
    }
}