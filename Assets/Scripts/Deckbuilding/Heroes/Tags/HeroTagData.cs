using UnityEngine;

namespace Deckbuilding.Tags
{
    [CreateAssetMenu(fileName = "HeroTagData", menuName = "ScriptableObjects/HeroTagData", order = 1)]
    public class HeroTagData : ScriptableObject
    {
        public string Name;
        [Multiline]public string Description;
        public Color Color;
        
        public HeroTagData[] OpositeTags;
    }
}