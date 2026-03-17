using System;
using TMPro;
using UnityEngine;

namespace Deckbuilding.Windows
{
    public class ZoneUI : MonoBehaviour
    {
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DangerText;

        public void Update()
        {
            NameText.text = "Неизвестное место";
            DangerText.text = $"Угроза: 0";
            
            foreach (var zone in Zone.List)
            {
                var playerPosition = PartyMovement.Instance.transform.position;
                var distance = Vector3.Distance(playerPosition, zone.transform.position);
                if (distance < zone.Radius)
                {
                    NameText.text = zone.Name;
                    DangerText.text = $"Угроза: {zone.DangerLevel}";
                }
            }
        }
    }
}