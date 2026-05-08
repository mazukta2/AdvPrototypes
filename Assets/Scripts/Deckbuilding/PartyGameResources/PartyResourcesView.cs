using Common;
using UnityEngine;

namespace Deckbuilding
{
    public class PartyResourcesView : SingletonMonoBehavior<PartyResourcesView>
    {
        public GameObject ResourcePrefab;
        public GameObject Root;

        public void ResetAll()
        {
            // destroy childen
            foreach (Transform child in Root.transform)            
                Destroy(child.gameObject);

            foreach (var resource in PartyResources.Instance.Resources)
            {
                if (resource.Value.Get() != 0)
                {
                    GameObject.Instantiate(ResourcePrefab, Root.transform).GetComponent<PartyResourceView>().ResourceType = resource.Key;
                }
            }
        }
    }
}