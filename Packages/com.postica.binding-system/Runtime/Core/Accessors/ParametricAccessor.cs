using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    public sealed class ParametricAccessor<S, T> :
            IAccessor,
            IAccessor<T>, IAccessor<S, T>,
            IAccessorLink, IBoundAccessor<T>,
            IParametricAccessor
    {
        private class SetParameter : IValueProvider<T>
        {
            public T value;
            public T Value => value;
            public object UnsafeValue => value;
        }

        private readonly MethodFactory.BaseReturnMethod<S, T> _getter;
        private readonly MethodFactory.BaseVoidMethod<S> _setter;
        private readonly SetParameter _mainParameter;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;
        private readonly bool _isValueType = typeof(S).IsValueType;

        private int _mainParameterIndex;
        private object[] _setParameters;

        private S _cachedValue;
        private bool _cacheReady;
        private IBoundAccessor<S> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public ParametricAccessor(ParametricAccessor<S, T> other)
        {
            _getter = other._getter?.Duplicate();
            _setter = other._setter?.Duplicate();
            _mainParameter = new SetParameter();
            Parameters = other._setParameters;
        }

        public ParametricAccessor(MethodInfo getter, MethodInfo setter)
        {
            if(getter != null)
            {
                _getter = MethodFactory.GetReturnMethodFor<S, T>(getter);
            }
            if(setter != null)
            {
                _setter = MethodFactory.GetVoidMethodFor<S>(setter);
            }
            _mainParameter = new SetParameter();
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
                    RebuildAccessorsFunctors();
                }
            }
        }

        public object[] Parameters 
        { 
            get => _setParameters;
            set
            {
                if (_setParameters != value)
                {
                    if (value != null)
                    {
                        _setParameters = new object[value.Length];
                        Array.Copy(value, _setParameters, value.Length);
                    }
                    else
                    {
                        _setParameters = null;
                    }
                    RebuildAccessorsFunctors();
                }
            }
        }

        public object GetValue(object target) => _getter.Invoke((S)target);

        public T GetValue() => _getter.Invoke(_boundParent.GetValue());

        public T GetValue(S target) => _getter.Invoke(target);

        public IAccessor Duplicate() => new ParametricAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                _mainParameter.value = tValue;
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        _mainParameter.value = (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    _mainParameter.value = (T)Convert.ChangeType(value, typeof(T));
                }
            }
            _setter.Invoke((S)target);
        }

        public void SetValue(object target, in T value)
        {
            _mainParameter.value = value;
            _setter.Invoke((S)target);
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            return _getter.Invoke(_cachedValue);
        }

        public void SetValue(in T value)
        {
            _mainParameter.value = value;

            if (!_cacheReady)
            {
                _cachedValue = _boundParent.GetValueToSet();
            }

            _setter.Invoke(_cachedValue);
            if (_isValueType)
            {
                _boundParent.SetValue(_cachedValue);
            }

            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _mainParameter.value = value;
            _setter.Invoke(target);
        }


        private void RebuildAccessorsFunctors()
        {
            if(_setter == null)
            {
                return;
            }

            var parameters = Parameters;
            if(0 <= _mainParameterIndex && _mainParameterIndex < parameters.Length)
            {
                parameters[_mainParameterIndex] = _mainParameter;
            }
            _getter?.AssignParameters(parameters);
            _setter?.AssignParameters(parameters);
        }


        T IAccessor<T>.GetValue(object target) => _getter.Invoke((S)target);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

    }
}
