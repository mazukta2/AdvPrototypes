using System;
using System.Collections.Generic;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.InteractionRules;
using Deckbuilding.QuestRules;

namespace Deckbuilding
{
    public class PartyQuest
    {
        public QuestData Data;
        public IQuestLogic Logic;

        public void OnComplete()
        {
            PartyResources.Instance.Change(PartyResources.ResourceType.Gold, Data.Reward);
        }

        public void Clear()
        {
            Logic.Clear();
        }
    }
}