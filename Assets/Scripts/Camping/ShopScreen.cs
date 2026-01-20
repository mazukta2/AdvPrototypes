using System;
using UnityEngine;

namespace Camping
{
    public class ShopScreen : MonoBehaviour
    {
        public float Range = 25f;
        public GameObject TargetPosition;
        public GameObject Screen;

        public void Update()
        {
            Screen.SetActive(
                Vector3.Distance(TargetPosition.transform.position, PartyCamp.Instance.transform.position) < Range);
        }
    }
}