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
    public sealed class MethodAccessor<S, T> :
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

        private readonly MethodFactory.BaseReturnMethod<S, T> _returnMethod;
        private readonly MethodFactory.BaseVoidMethod<S> _voidMethod;
        private readonly SetParameter _mainParameter;

        private readonly bool _canRead;

        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private int _mainParameterIndex;
        private object[] _parameters;

        private S _cachedValue;
        private bool _cacheReady;

        private RefBoundGetterDelegate<S> _boundGetter;
        private RefBoundGetterDelegate<S> _boundGetterForSet;
        private RefBoundSetterDelegate<S> _boundSetter;
        
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public MethodAccessor(MethodAccessor<S, T> other)
        {
            _returnMethod = other._returnMethod?.Duplicate();
            _voidMethod = other._voidMethod?.Duplicate();
            _mainParameter = new SetParameter();
            Parameters = other._parameters;

            _canRead = other._canRead;
        }

        public MethodAccessor(MethodInfo method)
        {
            if(method.ReturnType != typeof(void))
            {
                _returnMethod = MethodFactory.GetReturnMethodFor<S, T>(method);
                _canRead = true;
            }
            else
            {
                _voidMethod = MethodFactory.GetVoidMethodFor<S>(method);
                _canRead = false;
            }
            _mainParameter = new SetParameter();
        }

        private int RetrieveMainParamIndex(MethodInfo method)
        {
            int index = 0;
            foreach(var param in method.GetParameters())
            {
                if(param.ParameterType.IsAssignableFrom(typeof(T)))
                {
                    return index;
                }
            }

            return -1;
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
                    if (_parent == null) return;
                    
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

        public bool CanRead => _canRead;

        public bool CanWrite => _mainParameterIndex >= 0;

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
                    RebuildAccessorsFunctors();
                }
            }
        }

        public object GetValue(object target) => _returnMethod.Invoke((S)target);

        public T GetValue() => _returnMethod.Invoke(_boundGetter());

        public T GetValue(S target) => _returnMethod.Invoke(target);

        public IAccessor Duplicate() => new MethodAccessor<S, T>(this);

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
            Invoke((S)target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(in S target)
        {
            if (_voidMethod == null)
            {
                _returnMethod.Invoke(target);
            }
            else
            {
                _voidMethod.Invoke(target);
            }
        }

        public void SetValue(object target, in T value)
        {
            _mainParameter.value = value;
            Invoke((S)target);
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundGetterForSet();
            _cacheReady = true;
            return _returnMethod.Invoke(_cachedValue);
        }

        public void SetValue(in T value)
        {
            _mainParameter.value = value;

            if (_cacheReady)
            {
                Invoke(_cachedValue);
            }
            else
            {
                Invoke(_boundGetterForSet());
            }

            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _mainParameter.value = value;
            Invoke(target);
        }


        private void RebuildAccessorsFunctors()
        {
            if(_voidMethod == null && _returnMethod == null)
            {
                return;
            }

            var parameters = Parameters;
            if(0 <= _mainParameterIndex && _mainParameterIndex < parameters.Length)
            {
                parameters[_mainParameterIndex] = _mainParameter;
            }

            _voidMethod?.AssignParameters(parameters);
            _returnMethod?.AssignParameters(parameters);
        }

        T IAccessor<T>.GetValue(object target) => _returnMethod.Invoke((S)target);
        private T GetValueSpecial(object target) => _returnMethod.Invoke((S)target);

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
