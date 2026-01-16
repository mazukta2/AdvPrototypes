using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class TriggersManager : MonoBehaviour
    {
        public ReadOnlyBind<Trigger> defaultTrigger;
        public List<Trigger> triggers = new();
        
        public Trigger CurrentTrigger { get; set; }

        private void Reset()
        {
            triggers = new List<Trigger>(GetComponentsInChildren<Trigger>());
        }

        void Update()
        {
            foreach (var trigger in triggers)
            {
                if (trigger.areaTrigger.Value.isActive)
                {
                    CurrentTrigger = trigger;
                    break;
                }
            }

            CurrentTrigger = defaultTrigger;
        }
    } 
}
