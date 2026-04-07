namespace Deckbuilding.Buildings.BuildingActions
{
    public class GiveTags : IBuildingAction
    {
        public TagData[] Tags;
        
        public void Execute(Building building)
        {
            foreach (var tag in Tags)
            {
                if (!building.Tags.Contains(tag))
                {
                    building.Tags.Add(tag);
                }
            }
        }
    }
}