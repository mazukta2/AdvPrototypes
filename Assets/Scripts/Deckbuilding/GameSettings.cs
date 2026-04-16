using Deckbuilding.BuildingRules;
using Deckbuilding.InteractionRules;
using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "PartyMemberClass", menuName = "ScriptableObjects/GameSettings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public static GameSettings Instance { get; set; }
        
        public PartyMemberClass[] Classes;
        public QuestData[] Quests;
        public InteractionRule[] InteractionRules;
        public BuildingCombinationRule[] BuildingCombinationRules;

        [Multiline] public string Names;

        public int GatesCost;
        
        [Multiline] public string UknownEffect;
    }
}