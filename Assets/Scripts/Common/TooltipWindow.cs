using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Common
{
    public class TooltipWindow : SingletonMonoBehavior<TooltipWindow>
    {
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Description;

        private List<ITooltip> _list = new List<ITooltip>();

        public static void Add(ITooltip tooltip)
        {
            Instance._list.Add(tooltip);
            Instance.UpdateText();
        }
        
        public static void Remove(ITooltip tooltip)
        {
            Instance?._list?.Remove(tooltip);
            Instance?.UpdateText();
        }

        private  void UpdateText()
        {
            if (_list.Count == 0)
            {
                Name.text = "";
                Description.text  = "";
            }
            else
            {
                var instance = _list.First();
                Name.text  = instance.GetName();
                Description.text  = instance.GetDescription();
            }
        }

    }
}