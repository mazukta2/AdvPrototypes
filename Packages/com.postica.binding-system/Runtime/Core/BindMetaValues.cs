using System;
using System.Collections.Generic;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// The main class of the Binding System to access important information.
    /// </summary>
    internal class BindMetaValues : ScriptableObject
    {
        [System.Serializable]
        private class ObjectsList : SerializedList<Object>
        {
        }
        
        [SerializeField]
        private SerializedDictionary<string, ObjectsList> _objects = new();
        [SerializeField]
        private SerializedValueDictionary _values = new();
        
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            return defaultValue;
        }
        
        public void SetValue<T>(string key, T value)
        {
            _values[key] = value;
        }
        
        public bool TrySetValue<T>(string key, T value)
        {
            if (_values.TryGetValue(key, out var existing) && Equals(existing, value)) return false;
            _values[key] = value;
            return true;
        }
        
        public IList<Object> GetList(string key)
        {
            if(_objects.TryGetValue(key, out var list)) return list;
            
            list = new ObjectsList();
            _objects[key] = list;
            return list;
        }
        
        public IList<Object> GetSanitizedList(string key)
        {
            if (_objects.TryGetValue(key, out var list))
            {
                list.RemoveAll(obj => !obj);
                return list;
            }
            
            list = new ObjectsList();
            _objects[key] = list;
            return list;
        }
        
        public BindMetaValues Sanitize()
        {
            foreach (var key in _objects.Keys)
            {
                _objects[key].RemoveAll(obj => !obj);
            }

            return this;
        }
        
    }
}
