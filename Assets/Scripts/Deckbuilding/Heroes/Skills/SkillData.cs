using UnityEngine;

namespace Deckbuilding.Heroes.Skills
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "ScriptableObjects/SkillData", order = 1)]
    public class SkillData : ScriptableObject
    {
        public string Name;
    }
}