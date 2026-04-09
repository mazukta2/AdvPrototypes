namespace Deckbuilding.Buildings
{
    public interface IBuildingAction
    {
        void Execute(Building building);
        object[] GetParameters();
    }
}