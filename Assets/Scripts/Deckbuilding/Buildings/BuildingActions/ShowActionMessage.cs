using UnityEngine;

namespace Deckbuilding.Buildings.BuildingActions
{
    public class ShowActionMessage : IBuildingAction
    {
        [Multiline] public string ActionDescText;
        
        public void Execute(Building building)
        {
            WorldMessenger.Instance.ShowMessage(building.transform.position,ActionDescText);
        }

        public object[] GetParameters()
        {
            return new object[] {};
        }
    }
}