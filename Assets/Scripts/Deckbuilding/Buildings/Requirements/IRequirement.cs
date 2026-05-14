namespace Deckbuilding.Buildings.Requirements
{
    public interface IRequirement
    {
        bool Check();
        string GetDesc();
    }
}