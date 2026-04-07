using System.Collections.Generic;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Buildings
{
    public class Building : MonoBehaviour
    {
        public BuildingData Data;
        public List<TagData> Tags = new List<TagData>();
        
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
            var interactables = GetComponent<Interactable>();
            interactables.Tags = Tags.ToArray();
            interactables.RebuildTooltip();

            BuildingWindow.Instance.RebuildWindow();
        }
    }
}