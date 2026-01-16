using System;
using System.Reflection;
using Postica.BindingSystem.Accessors;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class IndexerAccessor<S, T> :
            IAccessor,
            IAccessor<T>, IAccessor<S, T>,
            IAccessorLink, IBoundAccessor<T>,
            IParametricAccessor,
            ICompiledAccessor<T>, IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
    {
        private class SetParameter : IValueProvider<T>
        {
            public T value;
            public T Value => value;
            public object UnsafeValue => value;
        }

        private readonly MethodFactory.BaseReturnMethod<S, T> _getter;
        private readonly MethodFactory.BaseSetterMethod<S, T> _setter;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;
        private readonly bool _isValueType = typeof(S).IsValueType;

        private int _mainParameterIndex;
        private object[] _parameters;

        private S _cachedValue;
        private bool _cacheReady;

        private RefBoundGetterDelegate<S> _boundGetter;
        private RefBoundGetterDelegate<S> _boundGetterForSet;
        private RefBoundSetterDelegate<S> _boundSetter;
        
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public IndexerAccessor(IndexerAccessor<S, T> other)
        {
            _getter = other._getter?.Duplicate();
            _setter = other._setter?.Duplicate() as MethodFactory.BaseSetterMethod<S, T>;
            Parameters = other._parameters;
        }

        public IndexerAccessor(MethodInfo getter, MethodInfo setter)
        {
            if(getter != null)
            {
                _getter = MethodFactory.GetReturnMethodFor<S, T>(getter);
            }
            if(setter != null)
            {
                _setter = MethodFactory.GetSetterMethodFor<S, T>(setter);
            }
        }
        
        public IndexerAccessor(MethodInfo getter, MethodInfo setter, object[] parameters) : this(getter, setter)
        {
            Parameters = parameters;
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
                        if (_parent is IBoundCompiledAccessor<S> boundCompiledAccessor)
                        {
                            _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                            _boundGetterForSet = boundCompiledAccessor.CompileBoundGetterForSet();
                            _boundSetter = boundCompiledAccessor.CompileBoundSetter();
                        }
                        else if (_parent is IBoundAccessor<S> boundAccessor)
                        {
                            _boundGetter = boundAccessor.GetValue;
                            _boundSetter = boundAccessor.SetValue;
                        }
                        
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

        public bool CanRead => _getter != null;

        public bool CanWrite => _setter != null;

        public int MainParamIndex 
        { 
            get => _mainParameterIndex;
            set
            {
                if(_mainParameterIndex != value)
                {
                    _mainParameterIndex = value;
                    RebuildAccessorFunctions();
                }
            }
        }

        public object[] Parameters 
        { 
            get => _parameters;
            set
            {
                if (_parameters != value)
                {
                    if (value != null)
                    {
                        _parameters = new object[value.Length];
                        Array.Copy(value, _parameters, value.Length);
                    }
                    else
                    {
                        _parameters = null;
                    }
                    RebuildAccessorFunctions();
                }
            }
        }

        public object GetValue(object target) => _getter.Invoke((S)target);

        public T GetValue() => _getter.Invoke(_boundGetter());

        public T GetValue(S target) => _getter.Invoke(target);

        public IAccessor Duplicate() => new IndexerAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                _setter.Invoke((S)target, tValue);
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        _setter.Invoke((S)target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    _setter.Invoke((S)target, (T)Convert.ChangeType(value, typeof(T)));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            _setter.Invoke((S)target, value);
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundGetterForSet();
            _cacheReady = true;
            return _getter.Invoke(_cachedValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedValue = _boundGetterForSet();
            }
            
            _setter.Invoke(_cachedValue, value);
            if (_isValueType)
            {
                _boundSetter(_cachedValue);
            }
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _setter.Invoke(target, value);
        }


        private void RebuildAccessorFunctions()
        {
            // if(_setter == null)
            // {
            //     return;
            // }

            var parameters = Parameters;
            _getter?.AssignParameters(parameters);
            _setter?.AssignParameters(parameters);
        }


        T IAccessor<T>.GetValue(object target) => _getter.Invoke((S)target);
        private T GetValueSpecial(object target) => _getter.Invoke((S)target);

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
