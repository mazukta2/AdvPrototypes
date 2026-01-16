using System;
using Postica.BindingSystem.Accessors;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    public static class ArrayAccessorTypes
    {
        public delegate void SetDelegate<S, T>(in S source, in T value);
        public delegate T GetDelegate<S, T>(in S source);
        public class DefaultIndex : IValueProvider<int>
        {
            public int Value => 0;
            public object UnsafeValue => 0;
        }
        public class ConstantIndex : IValueProvider<int>
        {
            private int _value;
            public ConstantIndex(object value)
            {
                try
                {
                    if (value is string sValue)
                    {
                        _value = int.Parse(sValue);
                    }
                    else
                    {
                        _value = (int)value;
                    }
                }
                catch
                {
                    _value = 0;
                }
            }

            public int Value => _value;

            public object UnsafeValue => _value;
        }
    }

    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class ArrayAccessor<S, T> :
            IAccessor,
            IAccessor<T>, IAccessor<S, T>,
            IAccessorLink, IBoundAccessor<T>,
            IParametricAccessor,
            IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
    {
        private readonly bool _valueIsValueType = typeof(T).IsValueType;
        private readonly bool _isValueType = typeof(S).IsValueType;
        private readonly ArrayAccessorTypes.GetDelegate<S, T> _getter;
        private readonly ArrayAccessorTypes.SetDelegate<S, T> _setter;
        private readonly int _rank;
        private readonly int[] _tempIndices;
        private readonly IValueProvider<int>[] _parameters;

        private S _cachedValue;
        private bool _cacheReady;
        private IBoundAccessor<S> _boundParent;
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public ArrayAccessor(ArrayAccessor<S, T> other)
        {
            Parameters = other._parameters;
            _rank = other._rank;

            _tempIndices = other._tempIndices.Clone() as int[];
            _parameters = other._parameters?.Clone() as IValueProvider<int>[];

            switch (_rank)
            {
                case 1:
                    _getter = Get1D;
                    _setter = Set1D;
                    break;
                case 2:
                    _getter = Get2D;
                    _setter = Set2D;
                    break;
                case 3:
                    _getter = Get3D;
                    _setter = Set3D;
                    break;
                case 4:
                    _getter = Get4D;
                    _setter = Set4D;
                    break;
                default:
                    _getter = GetND;
                    _setter = SetND;
                    break;
            }
        }

        public ArrayAccessor(Type sourceType)
        {
            if (!sourceType.IsArray)
            {
                throw new ArgumentException($"{nameof(ArrayAccessor<S, T>)}: The provided type is not an array");
            }

            _rank = sourceType.GetArrayRank();
            _tempIndices = _rank > 4 ? new int[_rank] : Array.Empty<int>();
            _parameters = new IValueProvider<int>[_rank];
            for (int i = 0; i < _rank; i++)
            {
                _parameters[i] = new ArrayAccessorTypes.DefaultIndex();
            }

            switch (_rank)
            {
                case 0: throw new ArgumentException($"{nameof(ArrayAccessor<S, T>)}: The provided type is an array of rank 0");
                case 1:
                    _getter = Get1D;
                    _setter = Set1D;
                    break;
                case 2:
                    _getter = Get2D;
                    _setter = Set2D;
                    break;
                case 3:
                    _getter = Get3D;
                    _setter = Set3D;
                    break;
                case 4:
                    _getter = Get4D;
                    _setter = Set4D;
                    break;
                default:
                    _getter = GetND;
                    _setter = SetND;
                    break;
            }
        }
        
        public ArrayAccessor(Type sourceType, object[] parameters) : this(sourceType)
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

        public bool CanRead => true;

        public bool CanWrite => true;

        public int MainParamIndex 
        { 
            get => 0;
            set { /*Nothing for now*/ }
        }

        public object[] Parameters 
        { 
            get => _parameters;
            set
            {
                if (_parameters == value)
                {
                    return;
                }

                if (value != null)
                {
                    var min = Mathf.Min(_rank, value.Length);
                    for (int i = 0; i < min; i++)
                    {
                        _parameters[i] = value[i] as IValueProvider<int> ?? new ArrayAccessorTypes.ConstantIndex(value[i]);
                    }
                    for (int i = min; i < _rank; i++)
                    {
                        _parameters[i] = new ArrayAccessorTypes.DefaultIndex();
                    }
                }
                else
                {
                    // The provided array is null, use the default indices then
                    for (int i = 0; i < _rank; i++)
                    {
                        _parameters[i] = new ArrayAccessorTypes.DefaultIndex();
                    }
                }
            }
        }

        public object GetValue(object target) => _getter((S)target);

        public T GetValue() => _getter.Invoke(_boundParent.GetValue());

        public T GetValue(S target) => _getter.Invoke(target);

        public IAccessor Duplicate() => new ArrayAccessor<S, T>(this);

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
            _cachedValue = _boundParent.GetValueToSet();
            _cacheReady = true;
            return _getter.Invoke(_cachedValue);
        }

        public void SetValue(in T value)
        {
            if (_cacheReady)
            {
                _setter(_cachedValue, value);
            }
            else
            {
                _setter(_boundParent.GetValueToSet(), value);
            }

            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _setter.Invoke(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _getter.Invoke((S)target);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);


        private void Set1D(in S array, in T value)
        {
            (array as T[])[_parameters[0].Value] = value;
        }

        private void Set2D(in S array, in T value)
        {
            (array as T[,])[_parameters[0].Value, _parameters[1].Value] = value;
        }

        private void Set3D(in S array, in T value)
        {
            (array as T[,,])[_parameters[0].Value, _parameters[1].Value, _parameters[2].Value] = value;
        }

        private void Set4D(in S array, in T value)
        {
            (array as T[,,,])[_parameters[0].Value, _parameters[1].Value, _parameters[2].Value, _parameters[3].Value] = value;
        }

        private void SetND(in S array, in T value)
        {
            for (int i = 0; i < _tempIndices.Length; i++)
            {
                _tempIndices[i] = _parameters[i].Value;
            }
            (array as Array).SetValue(value, _tempIndices);
        }
        
        private T GetValueSpecial(object target) => _getter((S)target);
        private T Get1D(in S array) => (array as T[])[_parameters[0].Value];
        private T Get2D(in S array) => (array as T[,])[_parameters[0].Value, _parameters[1].Value];
        private T Get3D(in S array) => (array as T[,,])[_parameters[0].Value, _parameters[1].Value, _parameters[2].Value];
        private T Get4D(in S array) => (array as T[,,,])[_parameters[0].Value, _parameters[1].Value, _parameters[2].Value, _parameters[3].Value];
        private T GetND(in S array)
        {
            for (int i = 0; i < _tempIndices.Length; i++)
            {
                _tempIndices[i] = _parameters[i].Value;
            }
            return (T)(array as Array).GetValue(_tempIndices);
        }

        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
}
