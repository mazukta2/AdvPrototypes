using System;
using Common;
using TMPro;
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
        public Image Highlight;
        public Image Selected;
        public TextMeshProUGUI Charge;
        private bool _highlight;

        public void Init(PartyMember member)
        {
            Member = member;
            Image.sprite = member.Class.Icon;
            Image.color = member.Class.Color;
            Tooltip.Name = member.Name + " (" + member.Class.Name + ")";
            Tooltip.Description = member.Class.Description;
            Highlight.sprite = member.Class.Icon;
            Selected.sprite = member.Class.Icon;
        }

        public void Update()
        {
            Health.fillAmount = (float)Member.CurrentHealth / Member.MaxHealth;
            if (Member.IsDead)
            {
                Image.sprite = Dead;
                Image.color = DeadColor;
                Selected.gameObject.SetActive(false);
                Highlight.gameObject.SetActive(false);
            }
            else if (_highlight)
            {
                Selected.gameObject.SetActive(false);
                Highlight.gameObject.SetActive(true);
            }
            else if (PartyMembers.Instance.SelectedMember == Member)
            {
                Selected.gameObject.SetActive(true);
                Highlight.gameObject.SetActive(false);
            }
            else
            {
                Image.color = Member.Class.Color;
                Selected.gameObject.SetActive(false);
                Highlight.gameObject.SetActive(false);
            }
            Charge.text = Member.Charge.ToString();
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
            
            if (Member.Charge <= 0)
                return;
            
            if (Member.IsDead)
                return;
            
            PartyMembers.Instance.SelectedMember = Member;
        }
    }
}