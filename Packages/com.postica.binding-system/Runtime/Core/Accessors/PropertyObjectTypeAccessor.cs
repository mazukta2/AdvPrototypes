using System;
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
    internal sealed class PropertyObjectTypeAccessor<S, T> :
        IAccessor,
        IAccessor<T>, IAccessor<S, T>,
        IAccessorLink, IBoundAccessor<T>,
        ICompiledAccessor<T>,
        IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
    {

        private readonly Func<S, T> _getter;
        private readonly Action<S, T> _setter;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private S _cachedValue;
        private bool _cacheReady;
        private RefBoundGetterDelegate<S> _boundGetter;
        private RefBoundGetterDelegate<S> _boundGetterForSet;
        
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public PropertyObjectTypeAccessor(PropertyObjectTypeAccessor<S, T> other)
        {
            _getter = other._getter;
            _setter = other._setter;
        }

        public PropertyObjectTypeAccessor(Func<S, T> getter, Action<S, T> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public IAccessorLink Previous
        {
            get => _parent;
            set
            {
                if (_parent == value) return;
                
                if (_parent != null && _parent.Next == this)
                {
                    _parent.Next = null;
                }
                
                _parent = value;
                
                if (_parent == null) return;
                
                if (_parent is IBoundCompiledAccessor<S> boundCompiledAccessor)
                {
                    _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                    _boundGetterForSet = boundCompiledAccessor.CompileBoundGetterForSet();
                }
                else if (_parent is IBoundAccessor<S> boundAccessor)
                {
                    _boundGetter = boundAccessor.GetValue;
                    _boundGetterForSet = boundAccessor.GetValueToSet;
                }
                _parent.Next = this;
            }
        }

        public IAccessorLink Next
        {
            get => _child;
            set
            {
                if (_child == value) return;
                
                _child = value;
                if (_child != null)
                {
                    _child.Previous = this;
                }
            }
        }

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(T);

        public bool CanRead => _getter != null;

        public bool CanWrite => _setter != null;

        public object GetValue(object target) => _getter((S)target);

        public T GetValue() => _getter(_boundGetter());

        public T GetValue(S target) => _getter(target);

        public IAccessor Duplicate() => new PropertyObjectTypeAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                _setter((S)target, tValue);
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        _setter((S)target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    _setter((S)target, (T)Convert.ChangeType(value, typeof(T)));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            _setter((S)target, value);
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundGetterForSet();
            _cacheReady = true;
            return _getter(_cachedValue);
        }

        public void SetValue(in T value)
        {
            _setter(_cacheReady ? _cachedValue : _boundGetterForSet(), value);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _setter(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _getter((S)target);
        private T GetValueSpecial(object target) => _getter((S)target);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
}
