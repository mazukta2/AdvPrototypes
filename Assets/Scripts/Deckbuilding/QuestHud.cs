using System;
using Common;
using TMPro;
using UnityEngine;

namespace Deckbuilding
{
    public class QuestHud : SingletonMonoBehavior<QuestHud>
    {
        public TextMeshProUGUI QuestHudText;

        public Color ActiveColor;
        public Color CompletedColor;
        private PartyQuest _quest;

        public void Set(PartyQuest member)
        {
            _quest = member;
            QuestHudText.text = member.Data.Name;
        }

        public void Clear()
        {
            _quest = null;
            QuestHudText.text = "";
        }

        public void Update()
        {
            if (_quest == null)
                return;

            if (_quest.Logic.IsCompleted())
            {
                QuestHudText.color = CompletedColor;
            }
            else
            {
                QuestHudText.color = ActiveColor;
            }
        }
    }
}