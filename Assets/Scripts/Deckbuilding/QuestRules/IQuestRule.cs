using UnityEngine;

namespace Deckbuilding.QuestRules
{
    public interface IQuestRule
    {
        public IQuestLogic Create();
    }
}