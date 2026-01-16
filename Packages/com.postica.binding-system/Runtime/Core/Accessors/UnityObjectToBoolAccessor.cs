using System;
using Postica.BindingSystem.Accessors;
using UnityEngine.Scripting;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [Preserve]
    internal sealed class UnityObjectToBoolAccessor : IAccessor<bool>, IConcurrentAccessor<bool>, IAccessor, IConcurrentAccessor
    {
        private readonly object _requestor;
        
        public UnityObjectToBoolAccessor(object requestor)
        {
            _requestor = requestor;
        }

        Type IAccessor.ObjectType => typeof(Object);

        Type IAccessor.ValueType => typeof(bool);

        bool IAccessor.CanRead => true;

        bool IAccessor.CanWrite => false;

        object IAccessor.GetValue(object target)
        {
            return GetValue(target);
        }

        object IConcurrentAccessor.GetValue(object target)
        {
            return GetValue(target);
        }

        void IAccessor.SetValue(object target, object value)
        {
            SetValue(target, (bool)value);
        }
        
        void IConcurrentAccessor.SetValue(object target, object value)
        {
            SetValue(target, (bool)value);
        }

        IAccessor IAccessor.Duplicate()
        {
            return new UnityObjectToBoolAccessor(_requestor);
        }

        IConcurrentAccessor IAccessor.MakeConcurrent()
        {
            return this;
        }

        public bool GetValue(object target) => (Object)target;

        public IConcurrentAccessor<bool> MakeConcurrent() => this;

        public void SetValue(object target, in bool value)
        {
            throw new InvalidOperationException($"{_requestor.GetType().Name}<{typeof(bool).Name}> cannot convert to bool from self reference of {target}.");
        }
    }
}
