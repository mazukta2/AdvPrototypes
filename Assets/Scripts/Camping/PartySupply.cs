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
        
        public float CampingDecayRate = 0.2f;
        public float DecayRate = 0.5f;
        public float DecayMovingRate = 6;
        public float HeatlhDecay = 0.5f;
        public float HeatlhMovingDecay = 4;
        public float CampingHeatlhDecay = 0.2f;

        public void Update()
        {
            
            ProgressValue = Mathf.MoveTowards(ProgressValue, 0, GetDecayRate() * Time.deltaTime);
            if (ProgressValue == 0 && Value != 0)
            {
                Value--;
                ProgressValue = ProgressMax;
            }

            if (ProgressValue == 0 && Value == 0)
            {
                var health = PartyHealth.Instance;
                health.Value = Mathf.MoveTowards(health.Value, 0, GetHealthDecayRate() * Time.deltaTime);
            }
        }

        protected float GetDecayRate()
        {
            if (PartyMovement.IsMoving())
                return DecayMovingRate;
            if (PartyCamp.IsPartyCampling())
                return CampingDecayRate;
            
            return DecayRate;
        }
        protected float GetHealthDecayRate()
        {
            if (PartyMovement.IsMoving())
                return HeatlhMovingDecay;
            if (PartyCamp.IsPartyCampling())
                return CampingHeatlhDecay;
            
            return HeatlhDecay;
        }
    }
}