using Common;
using QFSW.QC;
using UnityEngine;

namespace Camping
{
    public class PartyGold : SingletonMonoBehavior<PartyGold>
    {
        public float Value = 1;
        public float MaximiumValue = 5;

        [Command("get-gold")]
        public static void GetGold()
        {
            PartyGold.Instance.Value = Mathf.MoveTowards(Instance.Value, Instance.MaximiumValue, 10);
        }
        
        
        public static void Add(int value)
        {
            PartyGold.Instance.Value = Mathf.MoveTowards(Instance.Value, Instance.MaximiumValue, value);
        }
    }
}