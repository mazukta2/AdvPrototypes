using System;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Gates : MonoBehaviour
    {
        public float Distance = 20;
        public void OnEnable()
        {
            GetComponent<Interactable>().OnClick = () =>
            {
                PartyMovement.Instance.Set(gameObject, Distance);
            };
        }
    }
}