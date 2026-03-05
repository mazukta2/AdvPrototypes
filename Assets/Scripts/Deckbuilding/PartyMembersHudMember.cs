using System;
using Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class PartyMembersHudMember : ListMonoBehavior<PartyMembersHudMember>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image Image;
        public PartyMember Member;
        public Image Health;
        public Tooltip Tooltip;
        public Sprite Dead;
        public Color DeadColor;
        public Color HighlightColor;
        public Color SelectedColor;
        private bool _highlight;

        public void Init(PartyMember member)
        {
            Member = member;
            Image.sprite = member.Class.Icon;
            Image.color = member.Class.Color;
            Tooltip.Name = member.Class.Name;
            Tooltip.Description = member.Class.Description;
            
        }

        public void Update()
        {
            Health.fillAmount = (float)Member.CurrentHealth / Member.MaxHealth;
            if (Member.IsDead)
            {
                Image.sprite = Dead;
                Image.color = DeadColor;
            }
            else if (_highlight)
            {
                Image.color = HighlightColor;
            }
            else if (PartyMembers.Instance.SelectedMember == Member)
            {
                Image.color = SelectedColor;
            }
            else
            {
                Image.color = Member.Class.Color;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _highlight = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _highlight = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (PartyMembers.Instance.SelectedMember == Member)
            {
                PartyMembers.Instance.SelectedMember = null;
                return;
            }
            
            PartyMembers.Instance.SelectedMember = Member;
        }
    }
}