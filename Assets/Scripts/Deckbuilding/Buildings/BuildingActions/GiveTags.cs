using System;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class GiveTags : IBuildingAction
    {
        public TagData[] Tags;
        public int Time;
        public bool ToZone;
        
        public void Execute(Building building)
        {
            if (ToZone)
            {
                var zone = building.GetZone();
                
                foreach (var zoneBuilding in Building.List)
                {
                    if (zone == zoneBuilding.GetZone())
                    {
                        AddTag(zoneBuilding);
                    }
                }
            }
            else
            {
                AddTag(building);
            }
            
        }

        public object[] GetParameters()
        {
            return Array.Empty<object>();
        }

        private void AddTag(Building building)
        {
            
            
            foreach (var tag in Tags)
            {
                if (!building.Tags.Contains(tag))
                {
                    building.Tags.Add(tag);
                }

                if (Time > 0)
                    building.Counters[tag] = Time;
            }
        }
    }
}