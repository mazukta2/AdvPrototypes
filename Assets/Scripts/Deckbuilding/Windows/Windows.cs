using System;
using Common;
using UnityEngine;

namespace Deckbuilding.Windows
{
    public class Windows : SingletonMonoBehavior<Windows>
    {
        public void OnEnable()
        {
            CloseAll();
        }

        public void CloseAll()
        {
            foreach(var window in GetComponentsInChildren<IWindow>())
                window.Close();
        }
    }
}