using System;
using Common;
using UnityEngine;

namespace Deckbuilding.Windows
{
    public class Windows : SingletonMonoBehavior<Windows>
    {
        
        
        public void OnEnable()
        {
            foreach(var window in GetComponentsInChildren<IWindow>(true))
                window.Init();
            
            CloseAll();
        }

        public void CloseAll()
        {
            foreach(var window in GetComponentsInChildren<IWindow>(true))
                window.Close();
        }
    }
}