namespace Deckbuilding.Buildings
{
    public interface IBuildingOption
    {
        public string GetName();

        public void Click(BuildingWindowContext context);
    }
}