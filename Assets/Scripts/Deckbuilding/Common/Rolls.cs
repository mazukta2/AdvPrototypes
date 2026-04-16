using Deckbuilding.Heroes.Skills;
using UnityEngine;

namespace Deckbuilding.Windows
{
    public static class Rolls
    {
        public static bool Roll(PartyMember partyMember, SkillData skillData, int rollValue)
        {
            var skill = GetSkill(partyMember, skillData);
            var playerRoll = Random.Range(1, 20);
            var opositeRoll = Random.Range(1, 20);
            return playerRoll + skill >= opositeRoll + rollValue;
        }

        public static int GetSkill(PartyMember partyMember, SkillData skillData)
        {
            var amount = 0;
            foreach (var heroTag in partyMember.Tags)
            {
                foreach (var skillModifier in heroTag.Data.SkillModifiers)
                {
                    if (skillModifier.Skill == skillData)
                    {
                        amount+= skillModifier.Value;
                    }
                }
            }

            return amount;
        }
    }
}