using System.Collections.Generic;
using System.Linq;
using Common;
using Deckbuilding.Tags;
using Deckbuilding.Windows;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class ChangePartyScreenMember : ListMonoBehavior<ChangePartyScreenMember>
    {
        public TextMeshProUGUI PersonName;
        public TextMeshProUGUI ClassName;
        public Button SelectButton;
        public bool IsSelected;
        public Image Image;
        public GameObject TagPrefab;
        public GameObject TagContainer;

        private PartyMember _member;

        public void OnEnable()
        {
            SelectButton.onClick.AddListener(OnSelect);
        }
        
        
        public void Init(PartyMemberClass partyMemberClass)
        {
            ClassName.text = partyMemberClass.Name;
            Image.sprite = partyMemberClass.Icon;
            Image.color = partyMemberClass.Color;

            _member = new PartyMember()
            {
                Class = partyMemberClass,
                CurrentHealth = partyMemberClass.MaxHealth,
                MaxHealth = partyMemberClass.MaxHealth,
                Charge = partyMemberClass.Charge,
                Name = CreateName(),
                Tags = CreateTags(partyMemberClass),
            };
            
            PersonName.text = _member.Name;
            
            
            foreach (var heroTag in _member.Tags)
            {
                var go = Instantiate(TagPrefab, TagContainer.transform); 
                var tagComponent = go.GetComponent<SimpleTag>();
                var tagName =  heroTag.Data.Name;
                tagComponent.Text.text = tagName;
                tagComponent.Background.color = heroTag.Data.Color;
                if (tagComponent.Tooltip != null)
                {
                    tagComponent.Tooltip.Name = heroTag.Data.Name;
                    tagComponent.Tooltip.Description = heroTag.Data.Description 
                                                       + "\r\n"+
                                                       string.Join("\r\n",
                                                           heroTag.Data.SkillModifiers
                                                               .Select(t => 
                                                                   t.Skill.Name + 
                                                                   (t.Value>=0 ? " +" :" ")+ t.Value));
                }
            }
        }

        private HeroTag[] CreateTags(PartyMemberClass partyMemberClass)
        {
            var list = new List<HeroTag>();
            foreach (var heroTag in partyMemberClass.Tags.OrderBy(t => t.Chance))
            {
                if (heroTag.Tag.OpositeTags.Intersect(list.Select(t => t.Data)).Any())
                {
                    continue;
                }
                
                var chance = heroTag.Chance;
                if (Random.value <= chance)
                {
                    list.Add(new HeroTag()
                    {
                        Data = heroTag.Tag
                    });
                }
                if (list.Count >= 4)
                {
                    break;
                }
            }

            return list.ToArray();
        }

        private void OnSelect()
        {
            IsSelected = !IsSelected;
            SelectButton.GetComponent<Image>().color = IsSelected ? Color.green : Color.white;
            if (IsSelected)
            {
                PartyMembers.Instance.Add(_member);
            }
            else
            {
                PartyMembers.Instance.Remove(_member);
            }
        }

        private string CreateName()
        {
            var names = GameSettings.Instance.Names.Split(",");
            return names[Random.Range(0, names.Length)].Trim();
        }
    }
}