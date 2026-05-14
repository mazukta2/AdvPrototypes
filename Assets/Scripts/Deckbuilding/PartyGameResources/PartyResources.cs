using System.Collections.Generic;
using Common;
using Deckbuilding.PartyGameResources;

namespace Deckbuilding
{
    public class PartyResources : SingletonMonoBehavior<PartyResources>
    {
        public Dictionary<PartyResourceData, PartyResource> Resources = new ();

        public void Change(PartyResourceData type, int amount)
        {
            if (!Resources.ContainsKey(type))
            {
                Resources[type] = type.Create();
                Resources[type].Add(amount);
                PartyResourcesView.Instance.ResetAll();
                return;
            }
            
            Resources[type].Add(amount);
            PartyResourcesView.Instance.ResetAll();

        }
        
        public PartyResource Get(PartyResourceData type)
        {
            if (!Resources.ContainsKey(type))
            {
                Resources[type] = type.Create();
                PartyResourcesView.Instance.ResetAll();
            }

            return Resources[type];
        }

        public void Set(PartyResourceData type, int value)
        {
            if (!Resources.ContainsKey(type))
            {
                Resources[type] = type.Create();
                Resources[type].Set(value);
                PartyResourcesView.Instance.ResetAll();
                return;
            }
            
            Resources[type].Set(value);
            
            PartyResourcesView.Instance.ResetAll();
        }
        
        
        /*[Command("add-gold")]
        public static void DebugGold()
        {
            Instance.Change(ResourceType.Gold, 5);
        }*/

    }
}