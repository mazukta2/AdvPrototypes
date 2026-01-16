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
    internal sealed class PropertyValueTypeAccessor<S, T> :
        IAccessor,
        IAccessor<T>, IAccessor<S, T>,
        IAccessorLink, IBoundAccessor<T>,
        ICompiledAccessor<T>, IBoundCompiledAccessor<T>,
        ICompiledAccessor<S, T> 
        where S : struct
    {

        private readonly AccessorsFactory.GetterDelegate<S, T> _getter;
        private readonly AccessorsFactory.SetterDelegate<S, T> _setter;

        private readonly bool _canRead;
        private readonly bool _canWrite;

        private S _cachedSValue;
        private bool _cacheReady;

        private RefBoundGetterDelegate<S> _boundGetter;
        private RefBoundGetterDelegate<S> _boundGetterForSet;
        private RefBoundSetterDelegate<S> _boundSetter;
        
        private IAccessorLink _previousLink;
        private IAccessorLink _nextLink;

        public PropertyValueTypeAccessor(PropertyValueTypeAccessor<S, T> other)
        {
            _getter = other._getter;
            _setter = other._setter;

            _canWrite = other._canWrite;
            _canRead = other._canRead;
        }

        public PropertyValueTypeAccessor(AccessorsFactory.GetterDelegate<S, T> getter, AccessorsFactory.SetterDelegate<S, T> setter)
        {
            _getter = getter;
            _setter = setter;

            _canRead = getter != null;
            _canWrite = setter != null;
        }

        public IAccessorLink Previous
        {
            get => _previousLink;
            set
            {
                if (_previousLink == value) return;
                
                if (_previousLink != null && _previousLink.Next == this)
                {
                    _previousLink.Next = null;
                }
                _previousLink = value;
                
                if (_previousLink == null) return;

                if (_previousLink is IBoundCompiledAccessor<S> boundCompiledAccessor)
                {
                    _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                    _boundGetterForSet = boundCompiledAccessor.CompileBoundGetterForSet();
                    _boundSetter = boundCompiledAccessor.CompileBoundSetter();
                }
                else if (_previousLink is IBoundAccessor<S> boundAccessor)
                {
                    _boundGetter = boundAccessor.GetValue;
                    _boundGetterForSet = boundAccessor.GetValueToSet;
                    _boundSetter = boundAccessor.SetValue;
                }

                _previousLink.Next = this;
            }
        }

        public IAccessorLink Next
        {
            get => _nextLink;
            set
            {
                if (_nextLink == value) return;
                
                _nextLink = value;
                if (_nextLink != null)
                {
                    _nextLink.Previous = this;
                }
            }
        }

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(T);

        public bool CanRead => _canRead;

        public bool CanWrite => _canWrite;

        public object GetValue(object target) => _getter((S)target);

        public T GetValue() => _getter(_boundGetter());

        public T GetValue(S target) => _getter(target);

        public IAccessor Duplicate() => new PropertyValueTypeAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                _setter((S)target, tValue);
            }
            else if (value is string)
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

        public void SetValue(object target, in T value)
        {
            _setter((S)target, value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundGetterForSet();
            _cacheReady = true;
            return _getter(_cachedSValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedSValue = _boundGetterForSet();
            }
            _setter(_cachedSValue, value);
            _boundSetter(_cachedSValue);
        }

        public void SetValue(ref S target, in T value)
        {
            _setter(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _getter((S)target);
        private T GetValueSpecial(object target) => _getter((S)target);

        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
}
