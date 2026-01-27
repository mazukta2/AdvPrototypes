using System;
using Common;
using UnityEngine;
using UnityEngine.AI;

namespace Camping
{
    public class Donkey : ListMonoBehavior<Donkey>
    {
        public GameObject Follow;
        public GameObject Foolower;
        public NavMeshAgent Agent;

        public GameObject View;

        public void Update()
        {
            UpdateFolllower();
            
            if (Follow != null)
                Agent.SetDestination(Follow.transform.position);

            View.SetActive(!PartyCamp.IsPartyCampling());
        }

        private void UpdateFolllower()
        {
            if (Follow == null)
            {
                foreach (var donkey in Donkey.List)
                {
                    if (donkey != this)
                    {
                        if (donkey.Foolower == null)
                        {
                            donkey.Foolower = donkey.Follow;
                            Follow = donkey.gameObject;
                            return;
                        }
                    }
                }

                Follow = PartyMovement.Instance.gameObject;
            }

        }
    }
}