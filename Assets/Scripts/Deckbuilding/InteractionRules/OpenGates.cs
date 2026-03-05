using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    public class OpenGates : IRuleAction
    {
        public void Execute(Interactable interactable)
        {
            Debug.Log("Gates opened");
        }
    }
}