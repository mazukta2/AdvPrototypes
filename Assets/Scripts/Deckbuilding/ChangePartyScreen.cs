using System;
using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangePartyScreen : SingletonMonoBehavior<ChangePartyScreen>
    {
        public GameObject Screen;
        public GameObject PartyMemberPrefab;
        public GameObject PartyMembersList;
        public Button ContinueButton;
        public int MembersCount = 8;
        public int MaxSelected = 4;
        public GameSettings Settings;
        public TextMeshProUGUI CountText;

        public void OnEnable()
        {
            Screen.SetActive(true);
            ContinueButton.onClick.AddListener(OnContinue);
            ResetMembers();
        }

        public void Update()
        {
            if (PartyHealth.Instance.Value <= 0)
            {
                Screen.SetActive(true);
                PartyHealth.Instance.Value = PartyHealth.Instance.Max;
                ResetMembers();
            }

            var selectedCount = 0;
            foreach (var changePartyScreenMember in ChangePartyScreenMember.List)
            {
                if (changePartyScreenMember.IsSelected)
                {
                    selectedCount++;
                }
            }
            ContinueButton.interactable = selectedCount > 0 && selectedCount <= MaxSelected;
            CountText.text = $"{selectedCount}/{MaxSelected}";
        }

        public void Show()
        {
            Screen.SetActive(true);
            ResetMembers();
        }
        
        private void ResetMembers()
        {
            foreach (Transform child in PartyMembersList.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Destroy(child.gameObject);
                }
            }

            for (int i = 0; i < MembersCount; i++)
            {
                var randomClass = Settings.Classes[UnityEngine.Random.Range(0, Settings.Classes.Length)];
                var member = GameObject.Instantiate(PartyMemberPrefab, PartyMembersList.transform).GetComponent<ChangePartyScreenMember>();
                
                member.Init(randomClass);
            }
        }

        private void OnContinue()
        {
            Screen.SetActive(false);
        }
    }
}