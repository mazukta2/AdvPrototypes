using System;
using Common;
using Deckbuilding.Buildings;
using Deckbuilding.Interactables;
using TMPro;
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
        
        private BuildingWindowContext _context;

        public void OnEnable()
        {
            NameText.text = _context.Data.BuidlingName;
            DescriptionText.text = _context.Data.BuidlingDescription;

            foreach (Transform child in OptionContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var option in _context.Data.Options)
            {
                var go = Instantiate(OptionPrefab, OptionContainer.transform); 
                var optionComponent = go.GetComponent<BuildingWindowOption>();
                optionComponent.Text.text = option.GetName();
                optionComponent.Button.onClick.AddListener(() =>
                {
                    option.Click(_context);
                });
            }
        }

        public void Update()
        {
        }
        

        public void Set(GameObject building)
        {

            _context = new BuildingWindowContext()
            {
                Window = this,
                Data = building.GetComponent<Building>().Data
            };

        }
    }
}