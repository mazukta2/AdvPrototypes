using Sirenix.OdinInspector;
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

        [Button]
        public void Give()
        {
            PartyResources.Instance.Change(this, 1);
        }
        
        [Button]
        public void Give5()
        {
            PartyResources.Instance.Change(this, 5);
        }
    }
}