using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class CompoundAccessor<S, T> :
        IAccessor,
        IAccessor<T>, IAccessor<S, T>,
        IConcurrentAccessor, IConcurrentAccessor<T>, IConcurrentAccessor<S, T>,
        IAccessorLink, IBoundAccessor<S>,
        IParametricAccessor,
        IWrapperAccessor,
        ICompiledAccessor<T>,
        IBoundCompiledAccessor<S>
    where S : class
    {
        private readonly bool _targetIsValueType = typeof(S).IsValueType;
        private readonly bool _valueIsValueType = typeof(T).IsValueType;

        private readonly RefBoundGetterDelegate<T> _boundGetter;
        private readonly RefBoundSetterDelegate<T> _boundSetter;
        private readonly IParametricAccessor _leafParametric;

        private readonly RefGetterDelegate<S, T> _getter;
        private readonly RefSetterDelegate<S, T> _setter;

        private S _target;

        public CompoundAccessor(IEnumerable<IAccessor> accessorsChain)
        {
            IAccessorLink currentNode = this;
            foreach (var accessor in accessorsChain)
            {
                if (accessor is not IAccessorLink node) continue;
                
                var copy = accessor.Duplicate();
                node = copy as IAccessorLink;
                
                Next ??= node;
                CanRead &= copy.CanRead;
                if(copy.ValueType.IsValueType)
                {
                    CanWrite &= copy.CanWrite;
                }
                node.Previous = currentNode;
                currentNode = node;
                
                if(_leafParametric == null && copy is IParametricAccessor parametricAccessor)
                {
                    _leafParametric = parametricAccessor;
                }
            }
            
            if(Next == null)
            {
                throw new ArgumentException("Accessors chain cannot be empty");
            }

            if (currentNode is IAccessor currentAccessor)
            {
                CanRead &= currentAccessor.CanRead;
                CanWrite &= currentAccessor.CanWrite;
            }

            if (currentNode is IBoundCompiledAccessor<T> boundCompiledAccessor)
            {
                _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                _boundSetter = boundCompiledAccessor.CompileBoundSetter();
            }
            else if (currentNode is IBoundAccessor<T> boundAccessor)
            {
                _boundGetter = boundAccessor.GetValue;
                _boundSetter = boundAccessor.SetValue;
            }
            
            if (ChainAccessorsFactory.TryGetAccessors(accessorsChain.ToList(), out var getter, out var setter))
            {
                _getter = getter;
                _setter = setter;
            }
        }

        /// <summary>
        /// Constructor for thread-safe version
        /// </summary>
        /// <param name="other"></param>
        private CompoundAccessor(CompoundAccessor<S, T> other)
        {
            CanRead = other.CanRead;
            CanWrite = other.CanWrite;
            
            List<IAccessor> accessorsChain = new List<IAccessor>();
            var next = other.Next;

            IAccessorLink currentNode = this;
            while(next is IAccessor otherNextAccessor)
            {
                var accessor = otherNextAccessor.Duplicate();
                accessorsChain.Add(accessor);
                if (accessor is IAccessorLink node)
                {
                    Next ??= node;
                    CanRead &= accessor.CanRead;
                    if(accessor.ValueType.IsValueType)
                    {
                        CanWrite &= accessor.CanWrite;
                    }
                    node.Previous = currentNode;
                    currentNode = node;
                }
                
                if(_leafParametric == null && accessor is IParametricAccessor parametricAccessor)
                {
                    _leafParametric = parametricAccessor;
                }

                next = next.Next;
            }
            
            if(Next == null)
            {
                throw new ArgumentException("Accessors chain cannot be empty");
            }

            if (currentNode is IAccessor currentAccessor)
            {
                CanRead &= currentAccessor.CanRead;
                CanWrite &= currentAccessor.CanWrite;
            }

            if (currentNode is IBoundCompiledAccessor<T> boundCompiledAccessor)
            {
                _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                _boundSetter = boundCompiledAccessor.CompileBoundSetter();
            }
            else if (currentNode is IBoundAccessor<T> boundAccessor)
            {
                _boundGetter = boundAccessor.GetValue;
                _boundSetter = boundAccessor.SetValue;
            }
            
            if (ChainAccessorsFactory.TryGetAccessors(accessorsChain, out var getter, out var setter))
            {
                _getter = getter;
                _setter = setter;
            }
        }

        public IAccessorLink Previous { get; set; }
        public IAccessorLink Next { get; set; }

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(T);

        public bool CanRead { get; private set; } = true;

        public bool CanWrite { get; private set; } = true;

        public object CurrentSource => _target;

        public int MainParamIndex
        {
            get => _leafParametric?.MainParamIndex ?? 0;
            set { if (_leafParametric != null) { _leafParametric.MainParamIndex = value; } }
        }

        public object[] Parameters
        {
            get => _leafParametric?.Parameters;
            set
            {
                if (_leafParametric != null)
                {
                    _leafParametric.Parameters = value;
                }
            }
        }

        public IEnumerable<object> GetInnerAccessors()
        {
            var list = new List<object>();
            var node = Next;
            while (node != null)
            {
                list.Add(node);
                node = node.Next;
            }
            return list;
        }

        public object GetValue(object target)
        {
            if(_getter != null) { return _getter((S)target); }
            
            SetTarget(target);
            return GetValueInternal();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public S GetValue() => _target;

        public void SetValue(object target, object value)
        {
            T correctValue;
            if (value is T sValue)
            {
                correctValue = sValue;
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        correctValue = (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    correctValue = (T)Convert.ChangeType(value, typeof(T));
                }
            }
            else
            {
                throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
            }

            SetValue(target, correctValue);
        }

#if BIND_AVOID_IL2CPP_CHECKS
        [Il2CppSetOption(Option.NullChecks, false)]
#endif
        public void SetValue(object target, in T value)
        {
            if(_setter != null)
            {
                _target = (S)target;
                _setter(ref _target, value);
                return;
            }
            
            SetTarget(target);
            if (AccessorsFactory.SafeMode)
            {
                try
                {
                    _boundSetter(value);
                }
                catch { }
            }
            else
            {
                _boundSetter(value);
            }
        }

        public void SetValue(in S value)
        {
            _target = value;
        }

        T IAccessor<T>.GetValue(object target)
        {
            SetTarget(target);
            return GetValueInternal();
        }
        
        private T GetValueSpecial(object target)
        {
            SetTarget(target);
            return GetValueInternal();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValueInternal()
        {
            if (!AccessorsFactory.SafeMode) { return _boundGetter(); }
            try
            {
                return _boundGetter();
            }
            catch
            {
                return default;
            }
        }

        private void SetTarget(object target)
        {
            if (target is S sTarget)
            {
                _target = sTarget;
            }
            else if (_targetIsValueType)
            {
                if (target is string)
                {
                    try
                    {
                        _target = (S)Convert.ChangeType(target, typeof(S)); ;
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{target?.GetType().Name} cannot be cast to {typeof(S).Name}");
                    }
                }
                _target = (S)Convert.ChangeType(target, typeof(S)); ;
            }
            else
            {
                throw new InvalidCastException($"{target?.GetType().Name} cannot be cast to {typeof(S).Name}");
            }
        }

        public T GetValue(S target)
        {
            _target = target;
            return GetValueInternal();
        }

        public void SetValue(ref S target, in T value)
        {
            _target = target;
            if (AccessorsFactory.SafeMode)
            {
                try
                {
                    _boundSetter(value);
                }
                catch { }
            }
            else
            {
                _boundSetter(value);
            }
        }

        public IAccessor Duplicate() => new CompoundAccessor<S, T>(this);

        public S GetValueToSet() => _target;

        public IConcurrentAccessor<T> MakeConcurrent() => GetConcurrentVersion();

        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent() => GetConcurrentVersion();

        IConcurrentAccessor IAccessor.MakeConcurrent() => GetConcurrentVersion();

        private CompoundAccessor<S, T> GetConcurrentVersion()
        {
            return new CompoundAccessor<S, T>(this);
        }

        T IConcurrentAccessor<T>.GetValue(object target) => target is S s ? GetValue(s) : default;

        void IConcurrentAccessor<T>.SetValue(object target, in T value)
        {
            lock (target)
            {
                SetValue(target, value);
            }
        }

        T IConcurrentAccessor<S, T>.GetValue(S target) => GetValue(target);
        void IConcurrentAccessor<S, T>.SetValue(S target, in T value)
        {
            lock (target)
            {
                var copy = target;
                SetValue(ref copy, value);
            }
        }

        void IConcurrentAccessor.SetValue(object target, object value)
        {
            lock (target)
            {
                SetValue(target, value);
            }
        }

        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<S> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<S> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<S> CompileBoundSetter() => SetValue;


        private static class ChainAccessorsFactory
        {
            public static bool TryGetAccessors(List<IAccessor> accessors, out RefGetterDelegate<S, T> getter,
                out RefSetterDelegate<S, T> setter)
            {
                if (accessors.Count is <= 1 or > 3)
                {
                    getter = null;
                    setter = null;
                    return false;
                }

                var compiledInterface = typeof(ICompiledAccessor<int, int>).Name;
                if (!accessors.All(a => IsCompiledAccessor(a, compiledInterface)))
                {
                    getter = null;
                    setter = null;
                    return false;
                }

                bool? IsValueType(int index)
                {
                    return accessors.Count > index ? accessors[index].ValueType.IsValueType : null;
                }

                var chainType = (IsValueType(0), IsValueType(1), IsValueType(2)) switch
                {
                    (false, _, null) => typeof(ChainClass<,,>).MakeGenericType(typeof(S), typeof(T), accessors[0].ValueType),
                    (true, _, null) => typeof(ChainStruct<,,>).MakeGenericType(typeof(S), typeof(T), accessors[0].ValueType),
                    (false, false, _) => typeof(ChainClassClass<,,,>).MakeGenericType(typeof(S), typeof(T), accessors[0].ValueType, accessors[1].ValueType),
                    (true, false, _) => typeof(ChainStructClass<,,,>).MakeGenericType(typeof(S), typeof(T), accessors[0].ValueType, accessors[1].ValueType),
                    (false, true, _) => typeof(ChainClassStruct<,,,>).MakeGenericType(typeof(S), typeof(T), accessors[0].ValueType, accessors[1].ValueType),
                    _ => null
                };

                if (chainType == null)
                {
                    getter = null;
                    setter = null;
                    return false;
                }

                var chain = (Chain<S, T>)Activator.CreateInstance(chainType, accessors);
                getter = chain.GetGetter;
                setter = chain.GetSetter;
                return setter != null && getter != null;
            }

            private static bool IsCompiledAccessor(IAccessor a, string compiledInterface)
            {
                var iface = a.GetType().GetInterface(compiledInterface);
                return iface != null 
                    && iface.GetGenericArguments().Length == 2 
                    && iface.GetGenericArguments()[0] == a.ObjectType 
                    && iface.GetGenericArguments()[1] == a.ValueType;
            }

        }
    }

    internal abstract class Chain<S, T>
    {
        public abstract RefGetterDelegate<S, T> GetGetter { get; }
        public abstract RefSetterDelegate<S, T> GetSetter { get; }
    }

    internal class ChainClass<S, T, T1> : Chain<S, T> where T1 : class
    {
        public ChainClass(List<IAccessor> accessors)
        {
            var accessor1 = accessors[0];
            var accessor2 = accessors[1];

            var compiledAccessor1 = accessor1 as ICompiledAccessor<S, T1>;
            var compiledAccessor2 = accessor2 as ICompiledAccessor<T1, T>;

            if (accessor1.CanRead && accessor2.CanRead)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                GetGetter = v => compiledGetter2(compiledGetter1(v));
            }

            if (accessor1.CanRead && accessor2.CanWrite)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledSetter2 = compiledAccessor2.CompileSetter();
                GetSetter = (ref S v, in T t) =>
                {
                    var t1 = compiledGetter1(v);
                    compiledSetter2(ref t1, t);
                };
            }
        }

        public override RefGetterDelegate<S, T> GetGetter { get; }
        public override RefSetterDelegate<S, T> GetSetter { get; }
    }

    internal class ChainStruct<S, T, Ts> : Chain<S, T> where Ts : struct
    {
        public ChainStruct(List<IAccessor> accessors)
        {
            var accessor1 = accessors[0];
            var accessor2 = accessors[1];

            var compiledAccessor1 = accessor1 as ICompiledAccessor<S, Ts>;
            var compiledAccessor2 = accessor2 as ICompiledAccessor<Ts, T>;

            if (accessor1.CanRead && accessor2.CanRead)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                GetGetter = v => compiledGetter2(compiledGetter1(v));
            }

            if (accessor1.CanRead && accessor1.CanWrite && accessor2.CanWrite)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledSetter1 = compiledAccessor1.CompileSetter();
                var compiledSetter2 = compiledAccessor2.CompileSetter();
                GetSetter = (ref S v, in T t) =>
                {
                    var ts = compiledGetter1(v);
                    compiledSetter2(ref ts, t);
                    compiledSetter1(ref v, ts);
                };
            }
        }

        public override RefGetterDelegate<S, T> GetGetter { get; }
        public override RefSetterDelegate<S, T> GetSetter { get; }
    }


    internal class ChainClassClass<S, T, T1, T2> : Chain<S, T> where T1 : class where T2 : class
    {
        public ChainClassClass(List<IAccessor> accessors)
        {
            var accessor1 = accessors[0];
            var accessor2 = accessors[1];
            var accessor3 = accessors[2];

            var compiledAccessor1 = accessor1 as ICompiledAccessor<S, T1>;
            var compiledAccessor2 = accessor2 as ICompiledAccessor<T1, T2>;
            var compiledAccessor3 = accessor3 as ICompiledAccessor<T2, T>;

            if (accessor1.CanRead && accessor2.CanRead && accessor3.CanRead)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledGetter3 = compiledAccessor3.CompileGetter();
                GetGetter = v => compiledGetter3(compiledGetter2(compiledGetter1(v)));
            }

            if (accessor1.CanRead && accessor2.CanRead && accessor3.CanWrite)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledSetter3 = compiledAccessor3.CompileSetter();
                GetSetter = (ref S v, in T t) =>
                {
                    var t2 = compiledGetter2(compiledGetter1(v));
                    compiledSetter3(ref t2, t);
                };
            }
        }

        public override RefGetterDelegate<S, T> GetGetter { get; }
        public override RefSetterDelegate<S, T> GetSetter { get; }
    }

    internal class ChainStructClass<S, T, Ts, T2> : Chain<S, T> where Ts : struct where T2 : class
    {
        public ChainStructClass(List<IAccessor> accessors)
        {
            var accessor1 = accessors[0];
            var accessor2 = accessors[1];
            var accessor3 = accessors[2];

            var compiledAccessor1 = accessor1 as ICompiledAccessor<S, Ts>;
            var compiledAccessor2 = accessor2 as ICompiledAccessor<Ts, T2>;
            var compiledAccessor3 = accessor3 as ICompiledAccessor<T2, T>;

            if (accessor1.CanRead && accessor2.CanRead && accessor3.CanRead)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledGetter3 = compiledAccessor3.CompileGetter();
                GetGetter = v => compiledGetter3(compiledGetter2(compiledGetter1(v)));
            }

            if (accessor1.CanRead && accessor1.CanWrite && accessor2.CanRead && accessor2.CanWrite &&
                accessor3.CanWrite)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledSetter1 = compiledAccessor1.CompileSetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledSetter3 = compiledAccessor3.CompileSetter();

                GetSetter = (ref S v, in T t) =>
                {
                    var ts = compiledGetter1(v);
                    var t2 = compiledGetter2(ts);
                    compiledSetter3(ref t2, t);
                    compiledSetter1(ref v, ts);
                };
            }
        }

        public override RefGetterDelegate<S, T> GetGetter { get; }
        public override RefSetterDelegate<S, T> GetSetter { get; }
    }

    internal class ChainClassStruct<S, T, T1, Ts> : Chain<S, T> where Ts : struct where T1 : class
    {
        public ChainClassStruct(List<IAccessor> accessors)
        {
            var accessor1 = accessors[0];
            var accessor2 = accessors[1];
            var accessor3 = accessors[2];

            var compiledAccessor1 = accessor1 as ICompiledAccessor<S, T1>;
            var compiledAccessor2 = accessor2 as ICompiledAccessor<T1, Ts>;
            var compiledAccessor3 = accessor3 as ICompiledAccessor<Ts, T>;

            if (accessor1.CanRead && accessor2.CanRead && accessor3.CanRead)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledGetter3 = compiledAccessor3.CompileGetter();
                GetGetter = v => compiledGetter3(compiledGetter2(compiledGetter1(v)));
            }

            if (accessor1.CanRead && accessor1.CanWrite && accessor2.CanRead && accessor2.CanWrite &&
                accessor3.CanWrite)
            {
                var compiledGetter1 = compiledAccessor1.CompileGetter();
                var compiledSetter2 = compiledAccessor2.CompileSetter();
                var compiledGetter2 = compiledAccessor2.CompileGetter();
                var compiledSetter3 = compiledAccessor3.CompileSetter();

                GetSetter = (ref S v, in T t) =>
                {
                    var t1 = compiledGetter1(v);
                    var ts = compiledGetter2(t1);
                    compiledSetter3(ref ts, t);
                    compiledSetter2(ref t1, ts);
                };
            }
        }

        public override RefGetterDelegate<S, T> GetGetter { get; }
        public override RefSetterDelegate<S, T> GetSetter { get; }
    }
}
