using System;
using Deckbuilding.PartyGameResources;
using UnityEngine;

namespace Deckbuilding
{
    public class GameResourcePile : MonoBehaviour
    {
        public int Amount = 2;
        public PartyResourceData ResourceType;
        public float Distance = 3;

        public void Update()
        {
            if (Vector3.Distance(PartyMovement.Instance.transform.position, transform.position) < Distance)
            {
                PartyResources.Instance.Change(ResourceType, Amount);
                Destroy(gameObject);
            }
        }
    }
}