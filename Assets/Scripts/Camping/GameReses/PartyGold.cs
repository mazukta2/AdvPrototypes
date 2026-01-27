using Common;
using QFSW.QC;

namespace Camping
{
    public class PartyGold : SingletonMonoBehavior<PartyGold>
    {
        public float Value = 1;

        [Command("get-gold")]
        public static void GetGold()
        {
            PartyGold.Instance.Value+=10;
        }
    }
}