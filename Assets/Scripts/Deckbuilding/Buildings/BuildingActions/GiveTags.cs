using System;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class GiveTags : IBuildingAction
    {
        public TagData[] Tags;
        public int Time;
        
        public void Execute(Building building)
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

        public object[] GetParameters()
        {
            return Array.Empty<object>();
        }
    }
}