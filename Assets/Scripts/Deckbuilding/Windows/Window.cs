using System;
using Common;
using UnityEngine;

namespace Deckbuilding.Windows
{
    public class Window<T>: SingletonMonoBehavior<T>, IWindow where T : SingletonMonoBehavior<T>
    {
        public void Open()
        {
            gameObject.SetActive(true);
        }
        
        public void Open(Action<T> windowAction)
        {
            gameObject.SetActive(true);
            if (windowAction is not null)
            {
                windowAction(gameObject.GetComponent<T>());
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}