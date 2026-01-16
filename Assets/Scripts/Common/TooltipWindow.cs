using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Common
{
    public class TooltipWindow : SingletonMonoBehavior<TooltipWindow>
    {
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Description;

        private List<Tooltip> _list = new List<Tooltip>();

        public static void Add(Tooltip tooltip)
        {
            Instance._list.Add(tooltip);
            Instance.UpdateText();
        }
        
        public static void Remove(Tooltip tooltip)
        {
            Instance._list.Remove(tooltip);
            Instance.UpdateText();
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
                Name.text  = instance.Name;
                Description.text  = instance.Description;
            }
        }

    }
}