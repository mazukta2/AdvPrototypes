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
        public Image Health;
        public Tooltip Tooltip;
        public Sprite Dead;
        public Color DeadColor;

        public void Init(PartyMember member)
        {
            Member = member;
            Image.sprite = member.Class.Icon;
            Image.color = member.Class.Color;
            Tooltip.Name = member.Class.Name;
            
        }

        public void Update()
        {
            Health.fillAmount = (float)Member.CurrentHealth / Member.MaxHealth;
            if (Member.IsDead)
            {
                Image.sprite = Dead;
                Image.color = DeadColor;
            }
        }
    }
}