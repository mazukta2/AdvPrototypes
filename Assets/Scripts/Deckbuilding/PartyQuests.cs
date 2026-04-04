using System;
using System.Collections.Generic;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.InteractionRules;

namespace Deckbuilding
{
    public class PartyQuests : SingletonMonoBehavior<PartyQuests>
    {
        public List<PartyQuest> Quests = new List<PartyQuest>();

        public PartyQuest Add(PartyQuest quest)
        {
            Quests.Add(quest);
            return quest;
        }

        protected void Update()
        {
        }

        public void Remove(PartyQuest member)
        {
            Quests.Remove(member);
        }

        public void Clear()
        {
            foreach (var member in Quests)
            {
            }
            Quests.Clear();
        }

        public static void NewSeason()
        {
            foreach (var member in Instance.Quests)
            {
                if (member.Logic.IsCompleted())
                {
                    member.OnComplete();
                }
            }
            Instance.Clear();
        }
    }
}