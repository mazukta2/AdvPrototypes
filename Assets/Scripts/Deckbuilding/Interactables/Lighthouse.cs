using System;
using Common;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Interactables
{
    public class Lighthouse : ListMonoBehavior<Lighthouse>
    {
        public Animator Animator;
        public Zone Zone;
        public int DangerLevelReduction = 2;
        public bool _state;
        public int SeasonToTurnOff = 2;
        public int CurrrenCharge = 0;
        
        public void OnEnable()
        {
            GetComponent<Interactable>().OnReaching = () =>
            {
                LighthouseWindow.Instance.Open((i) => i.Set(this));
            };
        }

        public void TurnOn()
        {
            if (_state)
                return;
            
            Animator.SetBool("Active", true);
            _state = true;
            Zone.DangerLevel -= DangerLevelReduction;
            CurrrenCharge = SeasonToTurnOff;
        }
        public void TurnOff()
        {
            if (!_state)
                return;
            Animator.SetBool("Active", false);
            Zone.DangerLevel += DangerLevelReduction;
            _state = false;
        }

        public static void NewSeason()
        {
            foreach (var g in List)
            {
                if (g.CurrrenCharge > 0)
                    g.CurrrenCharge--;
                
                if (g.CurrrenCharge == 0)
                    g.TurnOff();
            }
        }

        public bool IsTurnOn()
        {
            return _state;
        }
    }
}