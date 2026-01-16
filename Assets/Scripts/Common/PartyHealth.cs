using Common;
using QFSW.QC;
using UnityEngine;

namespace Common
{
    public class PartyHealth : SingletonMonoBehavior<PartyHealth>
    {
        public float Value = 100;
        public float Max = 100;



        public static bool IsDead()
        {
            return Instance.Value <= 0;
        }
        

        [Command("damage-self")]
        public static void DebugDamage()
        {
            PartyHealth.Instance.Value -= Instance.Max / 3f;
        }
        
    }
}