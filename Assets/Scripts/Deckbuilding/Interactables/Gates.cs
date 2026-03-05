using System;
using Common;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Gates : ListMonoBehavior<Gates>
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

        public static void NewSeason()
        {
            foreach (var g in List)
            {
                g.CloseGates();
            }
        }
    }
}