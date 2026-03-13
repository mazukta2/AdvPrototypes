using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    public class OpenGates : IRuleAction
    {
        [Multiline] public string ActionText;
        
        public void Execute(PartyMember member, Interactable interactable)
        {
            interactable.GetComponent<Gates>().OpenGates();
            
            WorldMessenger.Instance.ShowMessage(interactable.transform.position, ActionText);

            member.Charge--;
        }
    }
}