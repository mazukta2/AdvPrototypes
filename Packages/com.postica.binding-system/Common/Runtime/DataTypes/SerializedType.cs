using System;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// This class is used to serialize types in Unity.
    /// </summary>
    [Serializable]
    public class SerializedType
    {
        [SerializeField] private string _type;
        
        private Type _backingType;

        /// <summary>
        /// The assembly qualified name of the type.
        /// </summary>
        public string AssemblyQualifiedName => _type;
        
        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name => Get()?.Name;

        /// <summary>
        /// Constructor for SerializedType.
        /// </summary>
        /// <param name="type"></param>
        public SerializedType(Type type)
        {
            _type = type?.AssemblyQualifiedName;
            _backingType = type;
        }

        /// <summary>
        /// Get the serialized type.
        /// </summary>
        /// <returns></returns>
        public Type Get()
        {
            if (_type == null)
            {
                return null;
            }
            _backingType ??= Type.GetType(_type, false);
            return _backingType;
        }

        public static implicit operator Type(SerializedType serializedType)
        {
            return serializedType.Get();
        }

        public static implicit operator SerializedType(Type type)
        {
            return new SerializedType(type);
        }
    }
}
