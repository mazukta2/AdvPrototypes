using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "PartyMemberClass", menuName = "ScriptableObjects/PartyMemberClass", order = 1)]
    public class PartyMemberClass : ScriptableObject
    {
        public string Name;
        [Multiline]public string Description;
        public Sprite Icon;
        public Color Color = Color.white;
        public float MaxHealth = 100;
        public int Charge = 1;
    }
}