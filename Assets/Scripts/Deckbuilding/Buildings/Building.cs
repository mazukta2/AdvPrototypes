using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Buildings
{
    public class Building : MonoBehaviour
    {
        public BuildingData Data;
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
            interactables.Tags = Data.Tags;
        }
    }
}