using System;
using Common;
using Deckbuilding.PartyGameResources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding
{
    public class PartyResourceView : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public PartyResourceData ResourceType;
        public Tooltip Tooltip;
        public Image Image;
        private bool inited;

        public void Update()
        {
            var res = PartyResources.Instance.Get(ResourceType);
            Text.text = res.Get().ToString();

            if (!inited)
            {
                inited = true;
                Tooltip.Name = res.GetName();
                Tooltip.Description = res.GetDescription();
                Image.sprite = res.GetImage();
                Image.color = res.GetColor();
                Text.color = res.GetColor();
            }
        }
    }
}