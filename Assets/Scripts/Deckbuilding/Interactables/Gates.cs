using System;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Gates : MonoBehaviour
    {
        public Animator Animator;
        
        public void OnEnable()
        {
            GetComponent<Interactable>().OnReaching = () =>
            {
                GatesWindow.Instance.Open((i) => i.Set(this));
            };
        }

        public void OpenGates()
        {
            Animator.SetBool("Open", true);
        }
        public void CloseGates()
        {
            Animator.SetBool("Open", false);
        }
    }
}