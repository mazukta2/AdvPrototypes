using Common;
using Deckbuilding;
using QFSW.QC;
using UnityEngine;

namespace Common
{
    public class PartyHealth : SingletonMonoBehavior<PartyHealth>
    {
        public static bool IsDead()
        {
            return PartyMembers.Instance.Members.Count > 0 && PartyMembers.Instance.Members.TrueForAll(m => m.CurrentHealth <= 0);
        }
        
        [Command("damage-self")]
        public static void DebugDamage()
        {
            foreach (var member in PartyMembers.Instance.Members)
            {
                member.CurrentHealth -= member.MaxHealth / 3f;
            }
        }

        public void Hit(float damage)
        {
            var randomMember = PartyMembers.Instance.Members[Random.Range(0, PartyMembers.Instance.Members.Count)];
            randomMember.CurrentHealth -= damage;
        }
    }
}