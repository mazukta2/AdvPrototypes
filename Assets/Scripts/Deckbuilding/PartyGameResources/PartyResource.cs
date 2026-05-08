using UnityEngine;

namespace Deckbuilding.PartyGameResources
{
    public class PartyResource
    {
        private readonly PartyResourceData _data;
        private int _amount;

        public PartyResource(PartyResourceData partyResourceData)
        {
            _data = partyResourceData;
        }

        public void Add(int amount)
        {
            _amount += amount;
        }

        public void Set(int value)
        {
            _amount += value;
        }

        public int Get()
        {
            return _amount;
        }

        public string GetName()
        {
            return _data.Name;
        }

        public string GetDescription()
        {
            return _data.Description;
        }

        public Sprite GetImage()
        {
            return _data.Icon;
        }

        public Color GetColor()
        {
            return _data.Color;
        }
    }
}