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
            GetComponent<Interactable>().OnReaching = () =>
            {
                BuildingWindow.Instance.Set(gameObject);
                BuildingWindow.Instance.Open();
            };
        }
    }
}