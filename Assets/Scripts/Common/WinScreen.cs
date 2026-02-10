using System;
using Common;
using UnityEngine;

namespace Camping
{
    public class WinScreen : SingletonMonoBehavior<WinScreen>
    {
        public GameObject Screen;

        public void OnEnable()
        {
            Screen.SetActive(false);
        }

        public void Show()
        {
            Screen.SetActive(true);
        }

        public bool IsWin()
        {
            return Screen.activeSelf;
        }
    }
}