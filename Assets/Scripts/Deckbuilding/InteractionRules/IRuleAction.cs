using Deckbuilding.Interactables;

namespace Deckbuilding.InteractionRules
{
    public interface IRuleAction
    {
        public void Execute(PartyMember member, Interactable interactable);
        string GetDescription();
    }
}