using UnityEngine;

namespace Deckbuilding.Buildings
{
    [CreateAssetMenu(fileName = "TagData", menuName = "Deckbuilding/TagData")]
    public class TagData : ScriptableObject
    {
        public string TagName;
        [Multiline] public string TagDescription;
        public Color Color;
    }
}