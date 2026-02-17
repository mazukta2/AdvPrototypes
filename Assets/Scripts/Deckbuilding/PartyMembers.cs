using System.Collections.Generic;
using Common;

namespace Deckbuilding
{
    public class PartyMembers : SingletonMonoBehavior<PartyMembers>
    {
        public List<PartyMember> Members = new List<PartyMember>();

        public PartyMember Add(PartyMemberClass memberClass)
        {
            var member = new PartyMember()
            {
                Class = memberClass
            };
            Members.Add(member);
            PartyMembersHud.Instance.Add(member);
            return member;
        }

        public void Remove(PartyMember member)
        {
            Members.Remove(member);
            PartyMembersHud.Instance.Remove(member);
        }

    }
}