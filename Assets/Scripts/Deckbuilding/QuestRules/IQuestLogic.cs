using UnityEngine;

namespace Deckbuilding.QuestRules
{
    public interface IQuestLogic
    {
        public GameObject GetTrackingObject();

        public bool IsCompleted();
    }
}