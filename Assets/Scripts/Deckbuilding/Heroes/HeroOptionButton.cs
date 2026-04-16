using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckbuilding.Heroes
{
    public class HeroOptionButton : MonoBehaviour
    {
        public Button Button;
        public TextMeshProUGUI Name;
        public Image Icon;
        public Sprite Dead;
        public Sprite NormalIcon;
        public Color DeadColor;
        public Tooltip Tooltip;
    }
}