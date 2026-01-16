using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class PartyCamp : SingletonMonoBehavior<PartyCamp>
    {
        public bool IsCampling;
        public GameObject Character;
        public GameObject Camp;
        public float Range = 10;

        public static void SetCamp(bool camping)
        {
            Instance.IsCampling = camping;
            Instance.UpdateChar();
        }

        protected void OnEnable()
        {
            UpdateChar();
        }

        private void UpdateChar()
        {
            Character.SetActive(!IsCampling);
            Camp.SetActive(IsCampling);
        }

        public static bool IsPartyCampling()
        {
            if (Instance == null) return false;
            return Instance.IsCampling;
        }
    }
}