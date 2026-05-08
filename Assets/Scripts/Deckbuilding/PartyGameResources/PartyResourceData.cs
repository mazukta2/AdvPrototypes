using UnityEngine;

namespace Deckbuilding.PartyGameResources
{
    [CreateAssetMenu(fileName = "PartyResourceData", menuName = "ScriptableObjects/PartyResourceData", order = 1)]
    public class PartyResourceData : ScriptableObject
    {
        public string Name;
        [Multiline] public string Description;
        public Sprite Icon;
        public Color Color;

        public PartyResource Create()
        {
            return new PartyResource(this);
        }

    }
}