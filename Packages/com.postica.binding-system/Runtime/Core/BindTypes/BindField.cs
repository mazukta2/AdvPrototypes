using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [Serializable]
    public struct BindField
    {
        public interface IValue
        {

        }

        [SerializeReference]
        private object _value;
        [SerializeField]
        private Object _unityObjectValue;
        [SerializeField]
        private bool _isUnityObject;

        public object Value 
        { 
            get => _isUnityObject ? _unityObjectValue : _value;
            set
            {
                if(value is Object obj)
                {
                    _unityObjectValue = obj;
                    _isUnityObject = true;
                }
                else
                {
                    _unityObjectValue = null;
                    _value = value;

                    if(value != null)
                    {
                        _isUnityObject = false;
                    }
                }
            }
        }
    }
}
