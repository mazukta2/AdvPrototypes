using Common.Utilities;
using UnityEngine;

namespace Common
{
    public class CharacterHealth : MonoBehaviourSingleton<CharacterHealth>
    {
        public float CurrentHealth;
        public float MaxHealth;


		[QFSW.QC.Command("hurt-self")]
        public static void HurtSelf()
        {
        }
    }
}