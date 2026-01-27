using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class ShowGold : MonoBehaviour
    {
        public TextMeshProUGUI Text;

        public void Update()
        {
            Text.text = PartyGold.Instance.Value + "/" + PartyGold.Instance.MaximiumValue;
        }
    }
}