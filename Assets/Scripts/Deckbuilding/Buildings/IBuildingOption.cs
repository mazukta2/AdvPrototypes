namespace Deckbuilding.Buildings
{
    public interface IBuildingOption
    {
        public string GetName();
        public string GetDescription();

        public void Click(BuildingWindowContext context);
    }
}