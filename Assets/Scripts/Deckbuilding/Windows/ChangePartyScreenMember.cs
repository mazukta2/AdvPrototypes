using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangePartyScreenMember : ListMonoBehavior<ChangePartyScreenMember>
    {
        public TextMeshProUGUI ClassName;
        public Button SelectButton;
        public bool IsSelected;
        public PartyMemberClass PartyMemberClass;
        public Image Image;

        private PartyMember _addedMember;

        public void OnEnable()
        {
            SelectButton.onClick.AddListener(OnSelect);
        }
        
        
        public void Init(PartyMemberClass partyMemberClass)
        {
            PartyMemberClass = partyMemberClass;
            ClassName.text = partyMemberClass.Name;
            Image.sprite = partyMemberClass.Icon;
            Image.color = partyMemberClass.Color;
        }

        private void OnSelect()
        {
            IsSelected = !IsSelected;
            SelectButton.GetComponent<Image>().color = IsSelected ? Color.green : Color.white;
            if (IsSelected)
            {
                _addedMember = PartyMembers.Instance.Add(PartyMemberClass);
            }
            else
            {
                if (_addedMember != null)
                {
                    PartyMembers.Instance.Remove(_addedMember);
                    _addedMember = null;
                }
            }
        }

    }
}