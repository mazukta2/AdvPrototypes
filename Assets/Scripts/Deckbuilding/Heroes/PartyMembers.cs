using System;
using System.Collections.Generic;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.InteractionRules;

namespace Deckbuilding
{
    public class PartyMembers : SingletonMonoBehavior<PartyMembers>
    {
        public List<PartyMember> Members = new List<PartyMember>();
        public PartyMember SelectedMember;

        public PartyMember Add(PartyMember member)
        {
            Members.Add(member);
            PartyMembersHud.Instance.Add(member);
            return member;
        }

        protected void Update()
        {
            if (SelectedMember != null && SelectedMember.IsDead)
            {
                SelectedMember = null;
            }
        }

        public void Remove(PartyMember member)
        {
            Members.Remove(member);
            PartyMembersHud.Instance.Remove(member);
        }

        public void Clear()
        {
            foreach (var member in Members)
            {
                PartyMembersHud.Instance.Remove(member);
            }
            Members.Clear();
        }

    }
}