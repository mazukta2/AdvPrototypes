using TMPro;
using UnityEngine;

namespace Deckbuilding
{
    public class PartyResourcesView : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public PartyResources.ResourceType ResourceType;
        
        
        public void Update()
        {
            Text.text = PartyResources.Instance.Get(ResourceType).ToString();
        }
    }
}