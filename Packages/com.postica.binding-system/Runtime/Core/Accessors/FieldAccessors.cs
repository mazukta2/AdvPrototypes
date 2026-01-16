using System;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using Postica.Common.Reflection;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{

    internal abstract class FieldAccessor
    {
        
    }
    
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal abstract class BaseFieldAccessor<S, T> : FieldAccessor,
        IAccessor,
        IAccessorLink
    {
        protected RefBoundGetterDelegate<S> _boundGetter;
        protected RefBoundGetterDelegate<S> _boundGetterForSet;
        protected RefBoundSetterDelegate<S> _boundSetter;
        
        private IAccessorLink _parent;
        private IAccessorLink _child;

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
                    _boundSetter = boundCompiledAccessor.CompileBoundSetter();
                    _boundGetterForSet = boundCompiledAccessor.CompileBoundGetterForSet();
                }
                else if (_parent is IBoundAccessor<S> boundAccessor)
                {
                    _boundGetter = boundAccessor.GetValue;
                    _boundSetter = boundAccessor.SetValue;
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
        public bool CanRead => true;
        public bool CanWrite => true;

        public abstract object GetValue(object target);
        public abstract IAccessor Duplicate();
        public abstract void SetValue(object target, object value);
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
    }
    
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal sealed class StructFieldValueAccessor<S, T> : BaseFieldAccessor<S, T>,
        IAccessor<T>, IAccessor<S, T>, IBoundAccessor<T>,
        ICompiledAccessor<T>, IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
        where S : struct
        where T : struct
    {

        private readonly Reflect.FastFieldAccessor<S, T> _accessor;

        private S _cachedSValue;
        private bool _cacheReady;

        public StructFieldValueAccessor(params string[] fieldNames)
        {
            _accessor = Reflect.FromStruct<S>.Get<T>(fieldNames);
        }

        public StructFieldValueAccessor(StructFieldValueAccessor<S, T> other)
        {
            _accessor = other._accessor;
        }

        public override object GetValue(object target) => _accessor.GetValue(target);

        public T GetValue()
        {
            _cachedSValue = _boundGetter();
            return _accessor.GetValue(ref _cachedSValue);
        }

        public T GetValue(S target) => _accessor.GetValue(ref target);

        public override IAccessor Duplicate() => new StructFieldValueAccessor<S, T>(this);

        public override void SetValue(object target, object value)
        {
            switch (value)
            {
                case T tValue:
                    _accessor.SetValue(target, tValue);
                    break;
                case string:
                    try
                    {
                        _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }

                    break;
                default:
                    _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    break;
            }
        }

        public void SetValue(object target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundGetterForSet();
            _cacheReady = true;
            return _accessor.GetValue(ref _cachedSValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedSValue = _boundGetterForSet();
            }
            _accessor.SetValue(ref _cachedSValue, value);
            _boundSetter(_cachedSValue);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(ref target, value);
        }

        T IAccessor<T>.GetValue(object target) => _accessor.GetValue(target);
        private T GetValueSpecial(object target) => _accessor.GetValue(target);
        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        
        
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
    
       [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal sealed class StructFieldRefAccessor<S, T> : BaseFieldAccessor<S, T>,
        IAccessor<T>, IAccessor<S, T>, IBoundAccessor<T>,
        ICompiledAccessor<T>, IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
        where S : struct
        where T : class
    {

        private readonly Reflect.FastFieldClassAccessor<S, T> _accessor;

        private S _cachedSValue;
        private bool _cacheReady;

        public StructFieldRefAccessor(params string[] fieldNames)
        {
            _accessor = Reflect.FromStruct<S>.GetRef<T>(fieldNames);
        }

        public StructFieldRefAccessor(StructFieldRefAccessor<S, T> other)
        {
            _accessor = other._accessor;
        }

        public override object GetValue(object target) => _accessor.GetValue(target);

        public T GetValue()
        {
            _cachedSValue = _boundGetter();
            return _accessor.GetValue(ref _cachedSValue);
        }

        public T GetValue(S target) => _accessor.GetValue(ref target);

        public override IAccessor Duplicate() => new StructFieldRefAccessor<S, T>(this);

        public override void SetValue(object target, object value)
        {
            switch (value)
            {
                case T tValue:
                    _accessor.SetValue(target, tValue);
                    break;
                case string:
                    try
                    {
                        _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }

                    break;
                default:
                    _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    break;
            }
        }

        public void SetValue(object target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundGetterForSet();
            _cacheReady = true;
            return _accessor.GetValue(ref _cachedSValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedSValue = _boundGetterForSet();
            }
            _accessor.SetValue(ref _cachedSValue, value);
            _boundSetter(_cachedSValue);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(ref target, value);
        }

        T IAccessor<T>.GetValue(object target) => _accessor.GetValue(target);
        private T GetValueSpecial(object target) => _accessor.GetValue(target);
        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        
        
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
    
    
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal sealed class ClassFieldValueAccessor<S, T> : BaseFieldAccessor<S, T>,
        IAccessor<T>, IAccessor<S, T>, IBoundAccessor<T>,
        ICompiledAccessor<T>, IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
        where S : class
        where T : struct
    {

        private readonly Reflect.FastFieldAccessor<T> _accessor;

        private S _cachedSValue;
        private bool _cacheReady;

        public ClassFieldValueAccessor(params string[] fieldNames)
        {
            _accessor = Reflect.From<S>.Get<T>(fieldNames);
        }

        public ClassFieldValueAccessor(ClassFieldValueAccessor<S, T> other)
        {
            _accessor = other._accessor;
        }

        public override object GetValue(object target) => _accessor.GetValue(target);

        public T GetValue()
        {
            return _accessor.GetValue(_boundGetter());
        }

        public T GetValue(S target) => _accessor.GetValue(target);

        public override IAccessor Duplicate() => new ClassFieldValueAccessor<S, T>(this);

        public override void SetValue(object target, object value)
        {
            switch (value)
            {
                case T tValue:
                    _accessor.SetValue(target, tValue);
                    break;
                case string:
                    try
                    {
                        _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }

                    break;
                default:
                    _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    break;
            }
        }

        public void SetValue(object target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundGetterForSet();
            _cacheReady = true;
            return _accessor.GetValue(_cachedSValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedSValue = _boundGetterForSet();
            }
            _accessor.SetValue(_cachedSValue, value);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _accessor.GetValue(target);
        private T GetValueSpecial(object target) => _accessor.GetValue(target);
        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);

        
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
    
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal sealed class ClassFieldRefAccessor<S, T> : BaseFieldAccessor<S, T>,
        IAccessor<T>, IAccessor<S, T>, IBoundAccessor<T>,
        ICompiledAccessor<T>, IBoundCompiledAccessor<T>, ICompiledAccessor<S, T>
        where S : class
        where T : class
    {

        private readonly Reflect.FastFieldClassAccessor<T> _accessor;

        private S _cachedSValue;
        private bool _cacheReady;

        public ClassFieldRefAccessor(params string[] fieldNames)
        {
            _accessor = Reflect.From<S>.GetRef<T>(fieldNames);
        }

        public ClassFieldRefAccessor(ClassFieldRefAccessor<S, T> other)
        {
            _accessor = other._accessor;
        }

        public override object GetValue(object target) => _accessor.GetValue(target);

        public T GetValue()
        {
            return _accessor.GetValue(_boundGetter());
        }

        public T GetValue(S target) => _accessor.GetValue(target);

        public override IAccessor Duplicate() => new ClassFieldRefAccessor<S, T>(this);

        public override void SetValue(object target, object value)
        {
            switch (value)
            {
                case T tValue:
                    _accessor.SetValue(target, tValue);
                    break;
                case string:
                    try
                    {
                        _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }

                    break;
                default:
                    _accessor.SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    break;
            }
        }

        public void SetValue(object target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        public T GetValueToSet()
        {
            _cachedSValue = _boundGetterForSet();
            _cacheReady = true;
            return _accessor.GetValue(_cachedSValue);
        }

        public void SetValue(in T value)
        {
            if (!_cacheReady)
            {
                _cachedSValue = _boundGetterForSet();
            }
            _accessor.SetValue(_cachedSValue, value);
            _cacheReady = false;
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(target, value);
        }

        T IAccessor<T>.GetValue(object target) => _accessor.GetValue(target);
        private T GetValueSpecial(object target) => _accessor.GetValue(target);
        public IConcurrentAccessor<T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        
        
        RefSetterDelegate<S, T> ICompiledAccessor<S, T>.CompileSetter() => SetValue;
        RefGetterDelegate<S, T> ICompiledAccessor<S, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
}
