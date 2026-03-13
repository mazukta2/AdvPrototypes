using System.Collections.Generic;
using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    public class NoEffect : IRuleAction
    {
        [Multiline] public string ActionText;
        [Multiline] public string ActionDescText;

        private HashSet<PartyMemberClass> _knownClasses = new HashSet<PartyMemberClass>();
        
        public void Execute(PartyMember member, Interactable interactable)
        {
            WorldMessenger.Instance.ShowMessage(interactable.transform.position, string.Format(ActionText, member.Class.Name));
            member.Charge--;
            _knownClasses.Add(member.Class);
        }

        public string GetDescription(PartyMember member, Interactable interactable)
        {
            return !_knownClasses.Contains(member.Class)?  GameSettings.Instance.UknownEffect: ActionDescText;
        }
    }
}