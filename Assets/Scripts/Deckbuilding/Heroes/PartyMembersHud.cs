using System;
using Common;
using UnityEngine;

namespace Deckbuilding
{
    public class PartyMembersHud : SingletonMonoBehavior<PartyMembersHud>
    {
        public GameObject List;
        public GameObject Prefab;

        public void Add(PartyMember member)
        {
            var item  = GameObject.Instantiate(Prefab, List.transform).GetComponent<PartyMembersHudMember>();
            item.Init(member);
        }

        public void Remove(PartyMember member)
        {
            foreach (var hudMember in PartyMembersHudMember.List)
            {
                if (hudMember.Member == member)
                    GameObject.Destroy(hudMember.gameObject);
            }
        }
    }
}