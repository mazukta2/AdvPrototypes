using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class PartySupply : SingletonMonoBehavior<PartySupply>
    {
        public float Value = 3;
        public float ProgressValue;
        public float ProgressMax = 100;
        
        public float DecayRate = 0.5f;
        public float DecayMovingRate = 6;
        public float HeatlhDecay = 0.5f;
        public float HeatlhMovingDecay = 4;


        public void Update()
        {
            var decay = DecayRate;
            if (PartyMovement.IsMoving())
                decay = DecayMovingRate;
            
            ProgressValue = Mathf.MoveTowards(ProgressValue, 0, decay * Time.deltaTime);
            if (ProgressValue == 0 && Value != 0)
            {
                Value--;
                ProgressValue = ProgressMax;
            }

            if (ProgressValue == 0 && Value == 0)
            {
                var healthDecay = HeatlhDecay;
                if (PartyMovement.IsMoving())
                    healthDecay = HeatlhMovingDecay;
                
                var health = PartyHealth.Instance;
                health.Value = Mathf.MoveTowards(health.Value, 0, healthDecay * Time.deltaTime);
            }
        }
    }
}