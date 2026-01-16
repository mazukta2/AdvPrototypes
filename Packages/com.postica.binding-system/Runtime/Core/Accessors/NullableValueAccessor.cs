using System;
using Postica.BindingSystem.Accessors;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class NullableValueAccessor<T> :
        IAccessor,
        IAccessor<T>, IAccessor<T?, T>,
        IAccessorLink, IBoundAccessor<T>
        where T : struct
    {
        private T? _cachedSValue;
        private T _cachedTValue;
        private bool _cacheReady;
        private readonly bool _isHasValue;
        private IBoundAccessor<T?> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        private T? CachedValue
        {
            get
            {
                if (_cacheReady) return _cachedSValue;
                
                _cachedSValue = _boundParent.GetValueToSet();
                _cacheReady = true;

                return _cachedSValue;
            }
        }

        public NullableValueAccessor(string memberName)
        {
            _isHasValue = memberName == nameof(Nullable<int>.HasValue);
        }

        public NullableValueAccessor(NullableValueAccessor<T> other)
        {
            _isHasValue = other._isHasValue;
        }

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
                        _boundParent = _parent as IBoundAccessor<T?>;
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

        public Type ObjectType => typeof(T?);

        public Type ValueType => typeof(T);

        public bool CanRead => true;

        public bool CanWrite => !_isHasValue;

        public object GetValue(object target) => _isHasValue ? ((T?)target).HasValue : ((T?)target).Value;

        public T GetValue() =>  GetValidValue(_boundParent.GetValue());

        public T GetValue(T? target) => _isHasValue ? GetHasValue(target) : GetValidValue(target);

        public IAccessor Duplicate() => new NullableValueAccessor<T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                _boundParent.SetValue(tValue);
            }
            else
            {
                _boundParent.SetValue(null);
            }
        }

        public void SetValue(object target, in T value)
        {
            _boundParent.SetValue(value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            _cachedTValue = _isHasValue ? GetHasValue(_cachedSValue) : GetValidValue(_cachedSValue);
            return _cachedTValue;
        }

        public void SetValue(in T value)
        {
            var valueToSet = (T?)value;
            _boundParent.SetValue(valueToSet);
            _cacheReady = false;
        }

        public void SetValue(ref T? target, in T value)
        {
            if (_boundParent != null)
            {
                SetValue(value);
            }
        }
        
        private static T GetHasValue(in T? source) => source.HasValue is T t ? t : default;
        private static T GetValidValue(in T? source) => source.HasValue ? source.Value : default;

        T IAccessor<T>.GetValue(object target) => GetValidValue((T?)target);

        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<T?, T>(this);

        IConcurrentAccessor<T?, T> IAccessor<T?, T>.MakeConcurrent() => new WrapConcurrentAccessor<T?, T>(this);

        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<T?, T>(this);
    }
}