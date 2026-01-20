using System;
using System.Collections.Generic;
using Common;
using QFSW.QC;
using UnityEngine;

namespace Camping
{
    public class PartyCamp : SingletonMonoBehavior<PartyCamp>
    {
        public bool IsCampling;
        public GameObject Character;
        public GameObject Camp;
        public float Range = 10;
        public List<BuildingsEnum> Buildings = new List<BuildingsEnum>();

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

        public void Build(BuildingsEnum buildingType)
        {
            Buildings.Add(buildingType);
        }

        public bool WasBuild(BuildingsEnum buildingType)
        {
            return Buildings.Contains(buildingType);
        }
        
        [Command("build")]
        public static void BuildDebug(BuildingsEnum buildingType)
        {
            PartyCamp.Instance.Build(buildingType);
        }
    }
}