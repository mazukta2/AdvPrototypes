using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class TakeTreasureBuildingOption : IBuildingOption
    {
        public string OptionName;
        [Multiline]public string OptionDesc;
        public TagData TreasureTag;
        public int Money;
        [Multiline] public string ActionDescText;
        
        public string GetName()
        {
            return OptionName;
        }
        public string GetDescription()
        {
            return string.Format(OptionDesc, Money);
        }

        public void Click(BuildingWindowContext context)
        {
            context.Building.RemoveTag(TreasureTag);
            PartyResources.Instance.Change(PartyResources.ResourceType.Gold, Money);
            WorldMessenger.Instance.ShowMessage(context.Building.transform.position, ActionDescText);
        }
    }
}