namespace Deckbuilding.Buildings.BuildingActions
{
    public class RemoveTags : IBuildingAction
    {
        public TagData[] Tags;
        public bool FromZone;
        
        public void Execute(Building building)
        {
            if (FromZone)
            {
                var zone = building.GetZone();
                foreach (var zoneBuilding in Building.List)
                {
                    if (zone == zoneBuilding.GetZone())
                    {
                        RemoveTag(zoneBuilding);
                    }
                }
            }
            else
            {
                RemoveTag(building);
            }
        }

        public object[] GetParameters()
        {
            return new object[] {};
        }
        
        public void RemoveTag(Building building)
        {
            foreach (var tag in Tags)
            {
                if (building.Tags.Contains(tag))
                {
                    building.Tags.Remove(tag);
                }
            }
        }
    }
}