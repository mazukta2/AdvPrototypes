using System;
using UnityEngine;

namespace Common.Utilities
{
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        public static T Instance { get; protected set; }
        
        protected void Awake()
        {
            Instance = this as T;
        }

        protected void OnDestroy()
        {
            Instance = null;
        }
    }
}