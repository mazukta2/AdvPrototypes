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
        public List<BuildingsEnum> AvailableBuildings = new List<BuildingsEnum>();
        
        public List<UpgradeEnum> Upgrades = new List<UpgradeEnum>();

        public static void SetCamp(bool camping)
        {
            Instance.IsCampling = camping;
            Instance.UpdateChar();
        }

        protected void OnEnable()
        {
            AvailableBuildings.Clear();
            AvailableBuildings.Add(BuildingsEnum.Kitchen);
            AvailableBuildings.Add(BuildingsEnum.Tent);
            AvailableBuildings.Add(BuildingsEnum.Woodwork);
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

        public void Build(UpgradeEnum buildingType)
        {
            Upgrades.Add(buildingType);
        }

        public bool WasBuild(UpgradeEnum buildingType)
        {
            return Upgrades.Contains(buildingType);
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

        public bool IsAvailable(BuildingsEnum buildingType)
        {
            return AvailableBuildings.Contains(buildingType);
        }

        public void SetAvailable(BuildingsEnum buildingType)
        {
            AvailableBuildings.Add(buildingType);
        }
    }
}