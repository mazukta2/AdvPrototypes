using System;
using TMPro;
using UnityEngine;

namespace Camping
{
    public class ToolsResource : CampingResourceBase
    {
        public override string GetName()
        {
            return "Инструменты:";
        }

        public override void TakeResource()
        {
            PartyTools.Instance.Value++;
        }
        
        public override float GetProgressModificator()
        {
            return 1f;
        }
    }
}