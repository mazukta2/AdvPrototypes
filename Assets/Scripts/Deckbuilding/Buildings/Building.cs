using System.Collections.Generic;
using System.Linq;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Buildings
{
    public class Building : ListMonoBehavior<Building>
    {
        public BuildingData Data;
        public List<TagData> Tags = new List<TagData>();
        public Dictionary<TagData, int> Counters = new Dictionary<TagData, int>();
        
        public void OnEnable()
        {
            var interactables = GetComponent<Interactable>();
            
            interactables.OnReaching = () =>
            {
                BuildingWindow.Instance.Set(gameObject);
                BuildingWindow.Instance.Open();
            };
            interactables.Name = Data.BuidlingName;
            interactables.Description = Data.BuidlingShortDescription;
            Tags.AddRange(Data.Tags);
            interactables.Tags = Tags.ToArray();
        }

        public void RemoveTag(TagData tagData)
        {
            Tags.Remove(tagData);
            Counters.Remove(tagData);
            if (tagData.OnRemove != null)
            {
                foreach (var buildingAction in tagData.OnRemove)
                {
                    buildingAction.Execute(this);
                }
            }
            
            var interactables = GetComponent<Interactable>();
            interactables.Tags = Tags.ToArray();
            interactables.RebuildTooltip();

            BuildingWindow.Instance.RebuildWindow();
        }

        public void AddTag(TagData tagData, int seasons = 0)
        {
            Tags.Add(tagData);
            if (seasons > 0) Counters.Add(tagData, seasons);
            
            if (tagData.OnAdd != null)
            {
                foreach (var buildingAction in tagData.OnAdd)
                {
                    buildingAction.Execute(this);
                }
            }
            
            var interactables = GetComponent<Interactable>();
            interactables.Tags = Tags.ToArray();
            interactables.RebuildTooltip();

            BuildingWindow.Instance.RebuildWindow();
        }

        public static void NewSeason()
        {
            foreach (var b in List)
            {
                foreach (var counter in b.Counters.ToArray())
                {
                    b.Counters[counter.Key] = counter.Value - 1;
                    if (counter.Value <= 1)
                    {
                        b.RemoveTag(counter.Key);
                    }
                }
            }
        }
    }
}