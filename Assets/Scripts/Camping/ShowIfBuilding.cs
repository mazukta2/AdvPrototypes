using System;
using UnityEngine;

namespace Camping
{
    public class ShowIfBuilding : MonoBehaviour
    {
        public BuildingsEnum BuildingType;
        public GameObject Building;
        
        public void Update()
        {
            Building.SetActive(PartyCamp.Instance.WasBuild(BuildingType));
        }
    }
}