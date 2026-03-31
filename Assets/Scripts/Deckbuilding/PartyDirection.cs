using System;
using System.Linq;
using UnityEngine;

namespace Deckbuilding
{
    public class PartyDirection : MonoBehaviour
    {
        public GameObject Direction;

        public void Update()
        {
            if (PartyQuests.Instance.Quests.Count <= 0)
                return;

            var trackingObject = PartyQuests.Instance.Quests.First().Logic.GetTrackingObject();
            if (trackingObject != null)
            {
                Direction.gameObject.SetActive(true);
                Direction.transform.LookAt(trackingObject.transform);
            } else{
                Direction.gameObject.SetActive(false);
            }
        }
    }
}