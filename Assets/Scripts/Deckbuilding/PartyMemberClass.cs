using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "PartyMemberClass", menuName = "ScriptableObjects/PartyMemberClass", order = 1)]
    public class PartyMemberClass : ScriptableObject
    {
        public string Name;
        public Sprite Icon;
        public Color Color = Color.white;
    }
}