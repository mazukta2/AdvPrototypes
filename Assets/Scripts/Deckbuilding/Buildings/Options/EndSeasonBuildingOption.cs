using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class EndSeasonBuildingOption : IBuildingOption
    {
        public string OptionName;
        [Multiline]public string OptionDesc;
        
        public string GetName(BuildingWindowContext context)
        {
            return OptionName;
        }

        public bool HasSelector()
        {
            return false;
        }
        
        public string GetDescription(BuildingWindowContext context, PartyMember partyMember)
        {
            return OptionDesc;
        }

        public string GetDescription(BuildingWindowContext context)
        {
            return OptionDesc;
        }

        public void Click(BuildingWindowContext context, PartyMember partyMember)
        {
            PartySeasons.Instance.EndSeason();
            context.Window.Close();
        }
    }
}