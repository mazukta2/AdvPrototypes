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

        public void OnEnable()
        {
            SelectButton.onClick.AddListener(OnSelect);
        }
        
        
        public void Init(PartyMemberClass partyMemberClass)
        {
            ClassName.text = partyMemberClass.Name;
        }
        
        private void OnSelect()
        {
            IsSelected = !IsSelected;
            SelectButton.GetComponent<Image>().color = IsSelected ? Color.green : Color.white;
        }
    }
}