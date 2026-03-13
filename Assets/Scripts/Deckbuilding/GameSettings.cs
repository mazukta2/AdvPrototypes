using Deckbuilding.InteractionRules;
using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "PartyMemberClass", menuName = "ScriptableObjects/GameSettings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public static GameSettings Instance { get; set; }
        
        public PartyMemberClass[] Classes;
        public InteractionRule[] InteractionRules;

        public int GatesCost;
        
        [Multiline] public string UknownEffect;
    }
}