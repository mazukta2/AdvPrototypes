namespace Deckbuilding.Buildings.BuildingActions
{
    public class RemoveTags : IBuildingAction
    {
        public TagData[] Tags;
        
        public void Execute(Building building)
        {
            foreach (var tag in Tags)
            {
                if (building.Tags.Contains(tag))
                {
                    building.Tags.Remove(tag);
                }
            }
        }

        public object[] GetParameters()
        {
            return new object[] {};
        }
    }
}