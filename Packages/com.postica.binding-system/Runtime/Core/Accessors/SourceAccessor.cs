using System;
using Postica.BindingSystem.Accessors;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
    internal class SourceAccessor<T> : IAccessor<T>, IAccessor<T, T>, IConcurrentAccessor<T>, IConcurrentAccessor<T, T>
    {
        private readonly object _requestor;

        public SourceAccessor(object requestor)
        {
            _requestor = requestor;
        }

        public T GetValue(object target) => (T)target;

        public T GetValue(T target) => target;

        public IConcurrentAccessor<T> MakeConcurrent() => this;

        public void SetValue(object target, in T value)
        {
            throw new InvalidOperationException($"{_requestor.GetType().Name}<{typeof(T).Name}> cannot write value into self reference for {target}.");
        }

        public void SetValue(ref T target, in T value)
        {
            throw new InvalidOperationException($"{_requestor.GetType().Name}<{typeof(T).Name}> cannot write value into self reference for {target}.");
        }
        
        public void SetValue(T target, in T value)
        {
            throw new InvalidOperationException($"{_requestor.GetType().Name}<{typeof(T).Name}> cannot write value into self reference for {target}.");
        }

        IConcurrentAccessor<T, T> IAccessor<T, T>.MakeConcurrent() => this;
    }
}
