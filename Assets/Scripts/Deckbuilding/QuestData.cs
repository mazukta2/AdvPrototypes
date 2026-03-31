using UnityEngine;

namespace Deckbuilding
{
    [CreateAssetMenu(fileName = "QuestData", menuName = "ScriptableObjects/QuestData", order = 1)]
    public class QuestData : ScriptableObject
    {
        public string Name;
        [Multiline]public string Description;
        public int Reward;
    }
}