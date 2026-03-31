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
        public CostUI CostUI;

        public PartyQuest Quest { get; set; }

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
            Quest = new PartyQuest() { Data = quest};
            QuestName.text = quest.Name;
            QuestDescription.text = quest.Description;
            CostUI.Cost = quest.Reward;
        }

        private void OnSelect()
        {
            IsSelected = !IsSelected;
            SelectButton.GetComponent<Image>().color = IsSelected ? Color.green : Color.white;
            if (IsSelected)
            {
                PartyQuests.Instance.Add(Quest);
                QuestHud.Instance.Set(Quest);
            }
            else
            {
                if (Quest != null)
                {
                    PartyQuests.Instance.Remove(Quest);
                    QuestHud.Instance.Clear();
                }
            }
        }
    }
}