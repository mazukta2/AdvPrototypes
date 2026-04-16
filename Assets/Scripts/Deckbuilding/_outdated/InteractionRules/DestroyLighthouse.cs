using System.Collections.Generic;
using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    
    /*public class DestroyLighthouse : IRuleAction
    {
        [Multiline] public string ActionText;
        [Multiline] public string ActionDescText;
        public int Gold;
        public int Danger;

        private HashSet<PartyMemberClass> _knownClasses = new HashSet<PartyMemberClass>();
        
        public void Execute(PartyMember member, Interactable interactable)
        {
            interactable.GetComponent<Lighthouse>().Decstruction();
            
            PartyResources.Instance.Change(PartyResources.ResourceType.Gold, Gold);
            interactable.GetComponent<Lighthouse>().Zone.DangerLevel += Danger;
            
            WorldMessenger.Instance.ShowMessage(interactable.transform.position, ActionText);

            member.Charge--;
            _knownClasses.Add(member.Class);
        }

        public string GetDescription(PartyMember member, Interactable interactable)
        {
            return !_knownClasses.Contains(member.Class)?  GameSettings.Instance.UknownEffect: ActionDescText;
        }
    }*/
}