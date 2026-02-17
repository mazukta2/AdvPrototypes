using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "PartyMemberClass", menuName = "ScriptableObjects/GameSettings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public PartyMemberClass[] Classes;
    }
}