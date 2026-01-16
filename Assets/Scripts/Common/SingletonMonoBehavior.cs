using UnityEngine;

namespace Common
{
    public class SingletonMonoBehavior<T>: MonoBehaviour where T : SingletonMonoBehavior<T>
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