using System;
using Common;
using UnityEngine;

namespace Deckbuilding.QuestRules
{
    public class ExploreDungeons : IQuestRule
    {
        public IQuestLogic Create()
        {
            return new Logic();
        }
        
        public class Logic : IQuestLogic
        {
            public Logic()
            {
            }
            
            public GameObject GetTrackingObject()
            {
                return null;
            }

            public bool IsCompleted()
            {
                return false;//PartyResources.Instance.Get(PartyResources.ResourceType.Report) >= 4;
            }

            public void Clear()
            {
            }
        }
    }
}