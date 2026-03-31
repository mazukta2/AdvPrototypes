using System;
using Common;
using TMPro;
using UnityEngine;

namespace Deckbuilding
{
    public class QuestHud : SingletonMonoBehavior<QuestHud>
    {
        public TextMeshProUGUI QuestHudText;

        public void Set(PartyQuest member)
        {
            QuestHudText.text = member.Data.Name;
        }

        public void Clear()
        { 
            QuestHudText.text = "";
        }
    }
}