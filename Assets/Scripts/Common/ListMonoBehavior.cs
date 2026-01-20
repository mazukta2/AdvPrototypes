using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class ListMonoBehavior<T>: MonoBehaviour where T : ListMonoBehavior<T>
    {
        public static List<T> List { get; protected set; } = new List<T>();
        
        protected void Awake()
        {
            List.Add(this as T);
        } 

        protected void OnDestroy()
        {
            List.Remove(this as T);
        }
    }
}