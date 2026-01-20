using Common;
using UnityEngine;

namespace Camping
{
    public class PartyFuel : SingletonMonoBehavior<PartyFuel>
    {
        public float Value = 1;
        public float ProgressValue;
        public float ProgressMax = 100;
        
        public float CampingDecayRate = 0.5f;
        public float CampingHeatlhDecay = 0.5f;


        public void Update()
        {
            if (!PartyCamp.IsPartyCampling())
            {
                return;
            }
            
            ProgressValue = Mathf.MoveTowards(ProgressValue, 0, CampingDecayRate * Time.deltaTime);
            if (ProgressValue == 0 && Value != 0)
            {
                Value--;
                ProgressValue = ProgressMax;
            }

            if (ProgressValue == 0 && Value == 0)
            {
                var health = PartyHealth.Instance;
                health.Value = Mathf.MoveTowards(health.Value, 0, CampingHeatlhDecay * Time.deltaTime);
            }
        }
    }
}