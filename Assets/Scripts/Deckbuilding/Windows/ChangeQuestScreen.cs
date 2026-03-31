using System;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangeQuestScreen : Window<ChangeQuestScreen>
    {
        public GameObject QuestMemberPrefab;
        public GameObject QuestyMembersList;
        public Button ContinueButton;
        public int MembersCount = 2;
        public GameSettings Settings;

        public void Start()
        {
            ContinueButton.onClick.AddListener(OnContinue);
        }
        
        public void OnEnable()
        {
            PartyQuests.Instance.Clear();
            QuestHud.Instance.Clear();
            ResetMembers();
        }
        
        public void Update()
        {
            ContinueButton.interactable = PartyQuests.Instance.Quests.Count > 0;
        }
        
        private void ResetMembers()
        {
            foreach (Transform child in QuestyMembersList.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Destroy(child.gameObject);
                }
            }

            for (int i = 0; i < MembersCount; i++)
            {
                var randomClass = Settings.Quests[UnityEngine.Random.Range(0, Settings.Quests.Length)];
                var member = GameObject.Instantiate(QuestMemberPrefab, QuestyMembersList.transform).GetComponent<ChangeQuestScreenMember>();
                
                member.Init(randomClass);
            }
        }

        private void OnContinue()
        {
            ChangePartyScreen.Instance.Open();
            this.Close();
        }
    }
}