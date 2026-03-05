using Deckbuilding.Interactables;

namespace Deckbuilding.InteractionRules
{
    public interface IRuleAction
    {
        public void Execute(Interactable interactable);
    }
}