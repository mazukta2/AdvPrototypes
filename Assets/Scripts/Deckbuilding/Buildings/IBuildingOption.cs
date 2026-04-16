namespace Deckbuilding.Buildings
{
    public interface IBuildingOption
    {
        public string GetName(BuildingWindowContext context);
        public string GetDescription(BuildingWindowContext context, PartyMember partyMember);
        public string GetDescription(BuildingWindowContext context);

        public void Click(BuildingWindowContext context, PartyMember partyMember);
        bool HasSelector();
    }
}