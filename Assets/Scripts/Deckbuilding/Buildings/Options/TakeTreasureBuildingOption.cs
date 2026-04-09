using System;
using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class TakeTreasureBuildingOption : IBuildingOption
    {
        public TagData TreasureTag;
        public TagData GuardsTag;
        public TagData[] KilledGuardsTags;
        public int KilledGuardsAmount = 2;
        public int Money;
        public int EnemyCount;

        public ConditionDesc JustTakeDesc;
        public ConditionDesc TriggeredGuardsDesc;
        
        
        public string GetName(BuildingWindowContext context)
        {
            return GetStateDesc(context).OptionName;
        }
        
        public string GetDescription(BuildingWindowContext context)
        {
            return string.Format(GetStateDesc(context).OptionDesc, Money);
        }

        public void Click(BuildingWindowContext context)
        {
            var state = GetState(context);
            WorldMessenger.Instance.ShowMessage(context.Building.transform.position, GetStateDesc(context).ActionDescText);
            
            if (state == State.JustTake)
            {
                context.Building.RemoveTag(TreasureTag);
                PartyResources.Instance.Change(PartyResources.ResourceType.Gold, Money);
            } else if (state == State.TriggeredGuards)
            {
                context.Building.RemoveTag(GuardsTag);
                foreach (var tagData in KilledGuardsTags)
                {
                    context.Building.AddTag(tagData, KilledGuardsAmount);
                }
                context.Window.Close();
                for (int i = 0; i < EnemyCount; i++)
                {
                    EnemySpawner.Instance.Spawn(context.Building.transform.position);
                }
            }
            
        }

        public ConditionDesc GetStateDesc(BuildingWindowContext context)
        {
            return GetState(context) switch
            {
                State.JustTake => JustTakeDesc,
                State.TriggeredGuards => TriggeredGuardsDesc,
                _ => JustTakeDesc
            };
        }


        public State GetState(BuildingWindowContext context)
        {
            if (context.Building.Tags.Contains(GuardsTag))
            {
                return State.TriggeredGuards;
            }
            return State.JustTake;
        }
        
        public enum State
        {
            JustTake,
            TriggeredGuards,
        }
        
        [Serializable]
        public struct ConditionDesc
        {
            public string OptionName;
            [Multiline]public string OptionDesc;
            [Multiline] public string ActionDescText;
        }
    }
}