using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace Postica.BindingSystem.Odin
{
    interface IExposedOdinDrawer
    {
        bool CallNextDrawer(GUIContent label);
    }
}