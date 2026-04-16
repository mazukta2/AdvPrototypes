using System;
using Deckbuilding.Heroes.Skills;
using UnityEngine;

namespace Deckbuilding.Tags
{
    [CreateAssetMenu(fileName = "HeroTagData", menuName = "ScriptableObjects/HeroTagData", order = 1)]
    public class HeroTagData : ScriptableObject
    {
        public string Name;
        [Multiline]public string Description;
        public Color Color;
        public SkillModifier[] SkillModifiers;
        public HeroTagData[] OpositeTags;
        
        [Serializable]
        public struct SkillModifier
        {
            public SkillData Skill;
            public int Value;

            public string GetFull()
            {
                return "<b>" + Skill.Name +
                       (Value >= 0 ? ": +" : ": ") + Value + "</b>";
            }
        }
    }
}