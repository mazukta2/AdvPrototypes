using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class EndSeasonBuildingOption : IBuildingOption
    {
        public string OptionName;
        [Multiline]public string OptionDesc;
        
        public string GetName()
        {
            return OptionName;
        }

        public string GetDescription()
        {
            return OptionDesc;
        }

        public void Click(BuildingWindowContext context)
        {
            PartySeasons.Instance.EndSeason();
            context.Window.Close();
        }
    }
}