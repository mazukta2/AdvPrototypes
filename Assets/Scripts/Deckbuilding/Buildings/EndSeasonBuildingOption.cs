namespace Deckbuilding.Buildings
{
    public class EndSeasonBuildingOption : IBuildingOption
    {
        public string OptionName;
        
        public string GetName()
        {
            return OptionName;
        }

        public void Click(BuildingWindowContext context)
        {
            PartySeasons.Instance.EndSeason();
            context.Window.Close();
        }
    }
}