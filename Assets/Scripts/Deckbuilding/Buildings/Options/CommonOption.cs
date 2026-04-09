using System.Linq;
using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class CommonOption: IBuildingOption
    {
        public string OptionName;
        [Multiline]public string OptionDesc;
        public bool CloseWindow;
        
        [SerializeReference] public IBuildingAction[] Actions;
        
        public string GetName(BuildingWindowContext context)
        {
            return OptionName;
        }

        public string GetDescription(BuildingWindowContext context)
        {
            return string.Format(OptionDesc, Actions.SelectMany(a => a.GetParameters()).ToArray());
        }

        public void Click(BuildingWindowContext context)
        {
            foreach (var action in Actions)
            {
                action.Execute(context.Building);
            }
            if (CloseWindow)
                context.Window.Close();
        }
    }
}