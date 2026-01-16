using System;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Scripting;

namespace Postica.BindingSystem.Accessors
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class ModifiersAccessor<S, T> : 
        IAccessor, IAccessor<T>, IAccessor<S, T>,
        IWrapperAccessor where S : class
    {
        private readonly bool _valueIsValueType = typeof(T).IsValueType;
        
        private readonly ModifyDelegate<T> _readValue;
        private readonly ModifyDelegate<T> _writeValue;

        private readonly IAccessor<S, T> _accessor;
        private readonly IBind _owner;
        
        private object[] _innerAccessors; // <-- lazy init

        public ModifiersAccessor(IAccessor<S, T> previous, IBind owner, IModifier[] modifiers)
        {
            _accessor = previous ?? throw new ArgumentNullException(nameof(previous));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            
            (CanRead, CanWrite) = previous is IAccessor accessor
                ? (accessor.CanRead, accessor.CanWrite)
                : (true, true);
            
            var modifier = modifiers.Length > 1
                ? new CompoundModifier<T>(owner, modifiers)
                : modifiers.Length > 0 ? modifiers[0] : null;

            if (modifier == null)
            {
                return;
            }
            
            (_readValue, _writeValue) = modifier.GetBothFunc<T>(owner, null);
        }
        
        public ModifiersAccessor(ModifiersAccessor<S, T> other)
        {
            _accessor = other._accessor;
            _owner = other._owner;
            _readValue = other._readValue;
            _writeValue = other._writeValue;
            CanRead = other.CanRead;
            CanWrite = other.CanWrite;
        }

        public Type ObjectType => typeof(S);
        public Type ValueType => typeof(T);
        public bool CanRead { get; }
        public bool CanWrite { get; }

        public object GetValue(object target) => _readValue(_accessor.GetValue((S)target));
        public T GetValue(S target) => _readValue(_accessor.GetValue(target));
        public IAccessor Duplicate() => new ModifiersAccessor<S, T>(this);

        public void SetValue(object target, object value)
        {
            var sTarget = (S)target;
            if (value is T tValue)
            {
                _accessor.SetValue(ref sTarget, _writeValue(tValue));
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        _accessor.SetValue(ref sTarget, _writeValue((T)Convert.ChangeType(value, typeof(T))));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value?.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    _accessor.SetValue(ref sTarget, _writeValue((T)Convert.ChangeType(value, typeof(T))));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            var sTarget = (S)target;
            _accessor.SetValue(ref sTarget, _writeValue(value));
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(ref target, _writeValue(value));
        }

        T IAccessor<T>.GetValue(object target) => _readValue(_accessor.GetValue((S)target));

        public IConcurrentAccessor<S, T> MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<S, T>(this);
        public IEnumerable<object> GetInnerAccessors() => _innerAccessors ??= new object[] { _accessor };
    }
}
