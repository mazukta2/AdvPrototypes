using System;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangePartyScreen : Window<ChangePartyScreen>
    {
        public GameObject PartyMemberPrefab;
        public GameObject PartyMembersList;
        public Button ContinueButton;
        public int MembersCount = 8;
        public int MaxSelected = 4;
        public GameSettings Settings;
        public TextMeshProUGUI CountText;

        public void Start()
        {
            ContinueButton.onClick.AddListener(OnContinue);
        }
        
        public void Update()
        {
            var selectedCount = GetSelectedCount();

            ContinueButton.interactable = selectedCount > 0 && selectedCount <= MaxSelected;
            CountText.text = $"{selectedCount}/{MaxSelected}";
        }

        private static int GetSelectedCount()
        {
            var selectedCount = 0;
            foreach (var changePartyScreenMember in ChangePartyScreenMember.List)
            {
                if (changePartyScreenMember.IsSelected)
                {
                    selectedCount++;
                }
            }

            return selectedCount;
        }

        public void OnEnable()
        {
            ResetMembers();
        }

        public void ResetMembers()
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
            Close();
        }
    }
}