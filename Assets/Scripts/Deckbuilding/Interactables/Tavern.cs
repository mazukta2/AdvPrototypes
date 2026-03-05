using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Tavern : MonoBehaviour
    {
        
        public void OnEnable()
        {
            GetComponent<Interactable>().OnReaching = () =>
            {
                TavernsWindow.Instance.Open();
            };
        }
    }
}