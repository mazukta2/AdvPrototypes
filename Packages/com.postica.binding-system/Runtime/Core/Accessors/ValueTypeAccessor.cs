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
    public sealed class ValueTypeAccessor<S, T> : 
        IAccessor, IAccessor<T>, IAccessor<S, T>, 
        IAccessorLink, IBoundAccessor<T>,
        IBoundCompiledAccessor<T>, ICompiledAccessor<S, T> where S : struct
    {
        public delegate T GetterDelegate(in S source);
        public delegate void SetterDelegate(ref S source, in T value);

        private readonly GetterDelegate _getter;
        private readonly SetterDelegate _setter;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private S _cacheValue;
        private bool _cacheReady;
        private IBoundAccessor<S> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        private readonly Func<S, T> _funcGetter;
        private readonly Func<S, T, S> _funcSetter;

        private T GetWrapper(in S source) => _funcGetter(source);
        private void SetWrapper(ref S source, in T value) => source = _funcSetter(source, value);

        public ValueTypeAccessor(Func<S, T> getter, Func<S, T, S> setter)
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

        public ValueTypeAccessor(ValueTypeAccessor<S, T> other)
        {
            _getter = other._getter;
            _setter = other._setter;
            _funcGetter = other._funcGetter;
            _funcSetter = other._funcSetter;
        }

        public ValueTypeAccessor(GetterDelegate getter, SetterDelegate setter)
        {
            _getter = getter;
            _setter = setter;
        }

        IAccessorLink IAccessorLink.Previous {
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
                _boundParent = _parent as IBoundAccessor<S>;
                _parent.Next = this;
            }
        }

        IAccessorLink IAccessorLink.Next {
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

        public T GetValue() => _getter(_boundParent.GetValue());

        public T GetValue(S target) => _getter(target);

        public IAccessor Duplicate() => new ValueTypeAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                var sTarget = (S)target;
                _setter(ref sTarget, tValue);
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        var sTarget = (S)target;
                        _setter(ref sTarget, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    var sTarget = (S)target;
                    _setter(ref sTarget, (T)Convert.ChangeType(value, typeof(T)));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            var sTarget = (S)target;
            _setter(ref sTarget, value);
        }

        public T GetValueToSet()
        {
            _cacheValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            return _getter(_cacheValue);
        }

        public void SetValue(in T value)
        {
            var target = _cacheReady ? _cacheValue : _boundParent.GetValueToSet();
            _setter(ref target, value);
            _boundParent.SetValue(target);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _setter(ref target, value);
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
