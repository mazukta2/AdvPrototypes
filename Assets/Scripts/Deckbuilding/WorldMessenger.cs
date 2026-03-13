using Common;
using UnityEngine;

namespace Deckbuilding
{
    public class WorldMessenger : SingletonMonoBehavior<WorldMessenger>
    {
        public GameObject MessagePrefab;
        
        public void ShowMessage(Vector3 worldPosition, string message)
        {
            var m = Instantiate(MessagePrefab, transform).GetComponent<Message>();
            m.Set(worldPosition, message);
        }
    }
}