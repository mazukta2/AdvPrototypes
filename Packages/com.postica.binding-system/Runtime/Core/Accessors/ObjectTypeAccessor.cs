using System;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Scripting;

namespace Postica.BindingSystem.Accessors
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class ObjectTypeAccessor<S, T> : 
        IAccessor, IAccessor<T>, IAccessor<S, T>, 
        IAccessorLink, IBoundAccessor<T>,
        IBoundCompiledAccessor<T>, ICompiledAccessor<S, T> where S : class
    {
        public delegate T GetterDelegate(in S source);
        public delegate void SetterDelegate(S source, in T value);

        private readonly GetterDelegate _getter;
        private readonly SetterDelegate _setter;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private S _cachedValue;
        private bool _cacheReady;
        private IBoundAccessor<S> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        private readonly Func<S, T> _funcGetter;
        private readonly Action<S, T> _funcSetter;

        private T GetWrapper(in S source) => _funcGetter(source);
        private void SetWrapper(S source, in T value) => _funcSetter(source, value);

        public ObjectTypeAccessor(Func<S, T> getter, Action<S, T> setter)
        {
            _funcGetter = getter;
            _funcSetter = setter;

            if (getter != null)
            {
                _getter = GetWrapper;
            }
            if (setter != null)
            {
                _setter = SetWrapper;
            }
        }

        public ObjectTypeAccessor(ObjectTypeAccessor<S, T> other)
        {
            _getter = other._getter;
            _setter = other._setter;
            _funcGetter = other._funcGetter;
            _funcSetter = other._funcSetter;
        }

        public ObjectTypeAccessor(GetterDelegate getter, SetterDelegate setter)
        {
            _getter = getter;
            _setter = setter;
        }

        IAccessorLink IAccessorLink.Previous {
            get => _parent;
            set {
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

        IAccessorLink IAccessorLink.Next {
            get => _child;
            set {
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

        public bool CanRead => _getter != null;

        public bool CanWrite => _setter != null;

        public object GetValue(object target) => _getter((S)target);

        public T GetValue() => _getter(_boundParent.GetValue());

        public T GetValue(S target) => _getter(target);

        public IAccessor Duplicate() => new ObjectTypeAccessor<S, T>(this);

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
            _cachedValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            return _getter(_cachedValue);
        }

        public void SetValue(in T value)
        {
            var target = _cacheReady ? _cachedValue : _boundParent.GetValueToSet();
            _setter(target, value);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _setter(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _getter((S)target);
        private T GetValueSpecial(object target) => _getter((S)target);

        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

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
