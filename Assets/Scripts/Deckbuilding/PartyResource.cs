using System.Collections.Generic;
using Common;

namespace Deckbuilding
{
    public class PartyResources : SingletonMonoBehavior<PartyResources>
    {
        public Dictionary<ResourceType, int> Resources = new Dictionary<ResourceType, int>();


        public void Change(ResourceType type, int amount)
        {
            if (!Resources.ContainsKey(type))
            {
                Resources[type] = 0;
            }
            
            Resources[type] += amount;
        }
        
        public int Get(ResourceType type)
        {
            if (!Resources.ContainsKey(type))
            {
                return 0;
            }

            return Resources[type];
        }
        
        public enum ResourceType
        {
            Report,
            Gold
        }

        public void Set(ResourceType type, int value)
        {
            Resources[type] = value;
        }
    }
}