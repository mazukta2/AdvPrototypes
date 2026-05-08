using System;
using Common;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangeQuestScreenMember : ListMonoBehavior<ChangeQuestScreenMember>
    {
        public TextMeshProUGUI QuestName;
        public TextMeshProUGUI QuestDescription;
        public Button SelectButton;
        public bool IsSelected;


        public QuestData QuestData { get; set; }

        public void OnEnable()
        {
            SelectButton.onClick.AddListener(OnSelect);
        }

        protected void Update()
        {
            SelectButton.interactable = IsSelected ||  PartyQuests.Instance.Quests.Count <= 0;
        }


        public void Init(QuestData quest)
        {
            QuestData = quest;
            QuestName.text = quest.Name;
            QuestDescription.text = quest.Description;
        }

        private void OnSelect()
        {
            IsSelected = !IsSelected;
            SelectButton.GetComponent<Image>().color = IsSelected ? Color.green : Color.white;
            if (IsSelected)
            {
                var quest = new PartyQuest() { Data = QuestData, Logic = QuestData.Rule.Create() };
                PartyQuests.Instance.Add(quest);
                QuestHud.Instance.Set(quest);
            }
            else
            {
                PartyQuests.Instance.Clear();
                QuestHud.Instance.Clear();
            }
        }
    }
}