using System;
using Common;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Lighthouse : ListMonoBehavior<Lighthouse>
    {
        public Animator Animator;
        public bool _state;
        
        public void OnEnable()
        {
            GetComponent<Interactable>().OnReaching = () =>
            {
                LighthouseWindow.Instance.Open((i) => i.Set(this));
            };
        }

        public void TurnOn()
        {
            Animator.SetBool("Active", true);
            _state = true;
        }
        public void TurnOff()
        {
            Animator.SetBool("Active", false);
            _state = false;
        }

        public static void NewSeason()
        {
            foreach (var g in List)
            {
                g.TurnOff();
            }
        }

        public bool IsTurnOn()
        {
            return _state;
        }
    }
}