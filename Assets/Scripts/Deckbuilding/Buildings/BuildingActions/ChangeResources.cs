using Deckbuilding.PartyGameResources;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class ChangeResources : IBuildingAction
    {
        public PartyResourceData ResourceData;
        public int Value;
        
        public void Execute(Building building)
        {
            PartyResources.Instance.Change(ResourceData, Value);
        }

        public object[] GetParameters()
        {
            return new object[] {Value};
        }
    }
}