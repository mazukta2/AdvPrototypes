using System.Collections.Generic;
using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    public class RestoreHealth : IRuleAction
    {
        [Multiline] public string ActionText;
        [Multiline] public string ActionDescText;

        private HashSet<PartyMemberClass> _knownClasses = new HashSet<PartyMemberClass>();
        
        public void Execute(PartyMember member, Interactable interactable)
        {
            member.CurrentHealth = member.MaxHealth;
            WorldMessenger.Instance.ShowMessage(interactable.transform.position, ActionText);

            member.Charge--;
            _knownClasses.Add(member.Class);
        }

        public string GetDescription(PartyMember member, Interactable interactable)
        {
            return !_knownClasses.Contains(member.Class)?  GameSettings.Instance.UknownEffect: ActionDescText;
        }
    }
}