using System;
using Common;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class PartyMembersHudMember : ListMonoBehavior<PartyMembersHudMember>
    {
        public Image Image;
        public PartyMember Member;
        public Tooltip Tooltip;

        public void Init(PartyMember member)
        {
            Member = member;
            Image.sprite = member.Class.Icon;
            Image.color = member.Class.Color;
            Tooltip.Name = member.Class.Name;
            
        }

    }
}