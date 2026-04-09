namespace Deckbuilding.Buildings.BuildingActions
{
    public class ChangeResources : IBuildingAction
    {
        public PartyResources.ResourceType ResourceType;
        public int Value;
        
        public void Execute(Building building)
        {
            PartyResources.Instance.Change(ResourceType, Value);
        }

        public object[] GetParameters()
        {
            return new object[] {Value};
        }
    }
}