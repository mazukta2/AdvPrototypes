using System;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public abstract class BaseAccessor<S, T> :
        IAccessor,
        IAccessor<T>, IAccessor<S, T>,
        IAccessorLink, IBoundAccessor<T> where S : class
    {
        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private S _cachedValue;
        private bool _cacheReady;
        private IBoundAccessor<S> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public IAccessorLink Previous
        {
            get => _parent;
            set
            {
                if (_parent != value)
                {
                    if (_parent != null && _parent.Next == this)
                    {
                        _parent.Next = null;
                    }
                    _parent = value;
                    if (_parent != null)
                    {
                        _boundParent = _parent as IBoundAccessor<S>;
                        _parent.Next = this;
                    }
                }
            }
        }

        public IAccessorLink Next
        {
            get => _child;
            set
            {
                if (_child != value)
                {
                    _child = value;
                    if (_child != null)
                    {
                        _child.Previous = this;
                    }
                }
            }
        }

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(T);

        public abstract bool CanRead { get; }

        public abstract bool CanWrite { get; }

        public virtual object GetValue(object target) => GetValue((S)target);

        public T GetValue() => GetValue(_boundParent.GetValue());

        public abstract T GetValue(S target);

        public abstract IAccessor Duplicate();

        public virtual void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                SetValue(ref Ref((S)target), tValue);
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        SetValue(ref Ref((S)target), (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    SetValue(ref Ref((S)target), (T)Convert.ChangeType(value, typeof(T)));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            SetValue(ref Ref((S)target), value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref S Ref(S target)
        {
            _cachedValue = target;
            return ref _cachedValue;
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            return GetValue(_cachedValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedValue = _boundParent.GetValueToSet();
            }

            SetValue(ref _cachedValue, value);
            
            if (typeof(S).IsValueType)
            {
                _boundParent.SetValue(_cachedValue);
            }
            _cacheReady = false;
        }

        public abstract void SetValue(ref S target, in T value);

        T IAccessor<T>.GetValue(object target) => GetValue((S)target);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

    }
}
