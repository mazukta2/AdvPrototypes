using System;
using System.Collections.Generic;
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
    public sealed class InheritanceAccessor<S, C, T> :
        IAccessor, IAccessor<T>, IAccessor<S, T>,
        IConcurrentAccessor, IConcurrentAccessor<T>, IConcurrentAccessor<S, T>,
        IWrapperAccessor
        where C : T
    {
        private readonly IAccessor<S, C> _accessor;

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(T);

        public bool CanRead => (_accessor as IAccessor).CanRead;

        public bool CanWrite => (_accessor as IAccessor).CanWrite;

        internal IAccessor<S, C> SourceAccessor => _accessor;

        public InheritanceAccessor(IAccessor<S, C> accessor)
        {
            _accessor = accessor;
        }

        private InheritanceAccessor(IAccessor<S, C> accessor, Func<C, T> conversion, Func<T, C> reverseConversion)
        {
            _accessor = accessor;
        }

        public object GetValue(object target)
        {
            return _accessor.GetValue((S)target);
        }

        public T GetValue(S target)
        {
            return _accessor.GetValue(target);
        }

        public IConcurrentAccessor MakeConcurrent()
        {
            return new InheritanceAccessor<S, C, T>((_accessor as IAccessor).MakeConcurrent() as IAccessor<S, C>);
        }

        public IAccessor Duplicate()
        {
            return new InheritanceAccessor<S, C, T>(_accessor);
        }

        public void SetValue(object target, object value)
        {
            var t = (S)target;
            _accessor.SetValue(ref t, (C)value);
        }

        public void SetValue(object target, in T value)
        {
            var t = (S)target;
            _accessor.SetValue(ref t, (C)value);
        }

        public void SetValue(ref S target, in T value)
        {
            _accessor.SetValue(ref target, (C)value);
        }

        T IAccessor<T>.GetValue(object target)
        {
            return _accessor.GetValue((S)target);
        }

        T IConcurrentAccessor<T>.GetValue(object target)
        {
            return (_accessor as IConcurrentAccessor<C>).GetValue((S)target);
        }

        T IConcurrentAccessor<S, T>.GetValue(S target)
        {
            return (_accessor as IConcurrentAccessor<S, C>).GetValue(target);
        }

        object IConcurrentAccessor.GetValue(object target)
        {
            return (C)(_accessor as IConcurrentAccessor).GetValue(target);
        }

        void IConcurrentAccessor<T>.SetValue(object target, in T value)
        {
            (_accessor as IConcurrentAccessor<C>).SetValue(target, (C)value);
        }

        void IConcurrentAccessor<S, T>.SetValue(S target, in T value)
        {
            (_accessor as IConcurrentAccessor<S, C>).SetValue(target, (C)value);
        }

        void IConcurrentAccessor.SetValue(object target, object value)
        {
            (_accessor as IConcurrentAccessor).SetValue(target, (T)value);
        }

        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent()
        {
            return new InheritanceAccessor<S, C, T>((_accessor as IAccessor<C>).MakeConcurrent() as IAccessor<S, C>);
        }

        IConcurrentAccessor<S, T> IAccessor<S, T>.MakeConcurrent()
        {
            return new InheritanceAccessor<S, C, T>(_accessor.MakeConcurrent() as IAccessor<S, C>);
        }

        public IEnumerable<object> GetInnerAccessors() => new object[] { _accessor };
    }

}
