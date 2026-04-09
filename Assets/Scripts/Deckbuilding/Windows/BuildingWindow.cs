using System;
using Common;
using Deckbuilding.Buildings;
using Deckbuilding.Interactables;
using TMPro;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Windows
{
    public class BuildingWindow : Window<BuildingWindow>
    {
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescriptionText;
        public GameObject OptionPrefab;
        public GameObject OptionContainer;
        public GameObject TagPrefab;
        public GameObject TagContainer;
        
        private BuildingWindowContext _context;

        public void OnEnable()
        {
            NameText.text = _context.Data.BuidlingName;
            DescriptionText.text = _context.Data.BuidlingDescription;


            RebuildWindow();
        }

        public void Update()
        {
        }
        

        public void Set(GameObject building)
        {

            _context = new BuildingWindowContext()
            {
                Window = this,
                Data = building.GetComponent<Building>().Data,
                Building = building.GetComponent<Building>()
            };

        }

        public void RebuildWindow()
        {
            if (!enabled)
                return;
            
            foreach (Transform child in OptionContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var option in _context.Data.Options)
            {
                MakeOption(option);
            }
            
            foreach (Transform child in TagContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var tagData in _context.Building.Tags)
            {
                var go = Instantiate(TagPrefab, TagContainer.transform); 
                var tagComponent = go.GetComponent<BuildingWindowTag>();
                var tagName =  tagData.TagName;
                if (_context.Building.Counters.TryGetValue(tagData, out var counter))
                {
                    tagName += ":"+counter;
                }   
                tagComponent.Text.text = tagName;
                tagComponent.Background.color = tagData.Color;
                if (tagComponent.Tooltip != null)
                {
                    
                    tagComponent.Tooltip.Name = tagData.TagName;
                    tagComponent.Tooltip.Description = tagData.TagDescription;
                }
            }

            foreach (var rule in GameSettings.Instance.BuildingCombinationRules)
            {
                foreach (var option in rule.GetOptionsForBuilding(_context.Building))
                {
                    MakeOption(option);
                }
            }
        }

        public void MakeOption(IBuildingOption option)
        {
            var go = Instantiate(OptionPrefab, OptionContainer.transform); 
            var optionComponent = go.GetComponent<BuildingWindowOption>();
            optionComponent.Text.text = option.GetName(_context);
            optionComponent.Button.onClick.AddListener(() =>
            {
                option.Click(_context);
            });
            optionComponent.Tooltip.Name = option.GetName(_context);
            optionComponent.Tooltip.Description = option.GetDescription(_context);
        }
    }
}