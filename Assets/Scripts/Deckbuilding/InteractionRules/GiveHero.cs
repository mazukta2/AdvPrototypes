using System.Collections.Generic;
using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    public class GiveHero : IRuleAction
    {
        [Multiline] public string ActionText;
        [Multiline] public string ActionDescText;

        private HashSet<PartyMemberClass> _knownClasses = new HashSet<PartyMemberClass>();
        
        public void Execute(PartyMember member, Interactable interactable)
        {
            /*var randomClass = GameSettings.Instance.Classes[UnityEngine.Random.Range(0, GameSettings.Instance.Classes.Length)];
            PartyMembers.Instance.Add(randomClass);
            WorldMessenger.Instance.ShowMessage(interactable.transform.position, ActionText);

            member.Charge--;
            _knownClasses.Add(member.Class);*/
        }

        public string GetDescription(PartyMember member, Interactable interactable)
        {
            return !_knownClasses.Contains(member.Class)?  GameSettings.Instance.UknownEffect: ActionDescText;
        }
    }
}