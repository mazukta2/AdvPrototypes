using UnityEngine;
using UnityEngine.EventSystems;

namespace Common
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Name;
        [Multiline]public string Description;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipWindow.Add(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipWindow.Remove(this);
        }
    }
}