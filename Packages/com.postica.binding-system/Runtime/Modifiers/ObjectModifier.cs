using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{

    /// <summary>
    /// A modifier which checks if the value is null and returns a fallback value if it is.
    /// </summary>
    [Serializable]
    [HideMember]
    public abstract class ObjectModifier<S> : IObjectModifier<S> where S : class
    {
        [SerializeField]
        [HideInInspector]
        protected SerializedType _targetType;
        
        /// <summary>
        /// Target type of the modifier.
        /// </summary>
        public virtual Type TargetType
        {
            get => _targetType?.Get() ?? typeof(S);
            set => _targetType = value;
        }
        
        ///<inheritdoc/>
        public abstract BindMode ModifyMode { get; set; }

        /// Whether the modifier can modify the type.
        public bool CanModifyType(Type type) => typeof(S).IsAssignableFrom(type);
        
        public T ModifyWrite<T>(T value)
        {
            if(value is S sValue)
            {
                return OnWrite(sValue) is T tValue ? tValue : value;
            }
            return value;
        }
        
        public T ModifyRead<T>(T value)
        {
            if(value is S sValue)
            {
                return OnRead(sValue) is T tValue ? tValue : value;
            }
            return value;
        }

        protected abstract SDerived OnWrite<SDerived>(SDerived value) where SDerived : S;
        protected abstract SDerived OnRead<SDerived>(SDerived value) where SDerived : S;

        ///<inheritdoc/>
        public abstract string Id { get; }

        ///<inheritdoc/>
        public abstract string ShortDataDescription { get; }

        ///<inheritdoc/>
        public virtual object Modify(BindMode mode, object value) => ModifyMode == BindMode.Write ? ModifyWrite(Convert.ChangeType(value, TargetType)) : ModifyRead(Convert.ChangeType(value, TargetType));
    }
}