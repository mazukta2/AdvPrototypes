using System;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
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
        public TextMeshProUGUI CostText;
        public int ExtraCost = 2;
        private bool _requestEndSeason;

        public void OnEnable()
        {
            ContinueButton.onClick.AddListener(OnContinue);
        }
        
        public void Update()
        {
            if ((_requestEndSeason || PartyHealth.IsDead()) && !Screen.activeSelf)
            {
                _requestEndSeason = false;
                Screen.SetActive(true);
                PartyMembers.Instance.Clear();
                PartyMovement.NewSeason();
                Enemy.ResetEnemies();
                Bullet.DestroyAll();
                Lighthouse.NewSeason();
                Zone.NewSeason();
                Gates.NewSeason();
                ResetMembers();
            }

            var selectedCount = GetSelectedCount();
            var cost = GetCost();

            CostText.text = $"Стоимость: {cost} золота";
            ContinueButton.interactable = selectedCount > 0 && cost <= PartyResources.Instance.Get(PartyResources.ResourceType.Gold);
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

        private int GetCost()
        {
            var selectedCount = GetSelectedCount();
            var cost = 0;
            if (selectedCount > MaxSelected)
            {
                cost = (selectedCount - MaxSelected) * ExtraCost;
            }

            return cost;
        }

        public void EndSeason()
        {
            _requestEndSeason = true;
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
            PartyResources.Instance.Change(PartyResources.ResourceType.Gold, -GetCost());
        }
    }
}