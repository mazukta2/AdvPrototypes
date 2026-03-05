using System;
using Deckbuilding;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Common
{
    public class InputSystem : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetMouseButtonDown(0) &&  !EventSystem.current.IsPointerOverGameObject())
            {
                foreach (var interactable in Interactable.List)     
                {
                    if (interactable.Selected)
                    {
                        PartyMovement.Instance.Set(interactable, interactable.Distance);
                        return;
                    }
                }
                
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {  
                    PartyMovement.Instance.Set(hit.point);
                }
            }
        }
    }
}