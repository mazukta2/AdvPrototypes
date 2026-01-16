using System;
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
    public class GenericAccessor: IAccessor, IConcurrentAccessor, IAccessorLink
    {
        private IAccessorLink _parent;
        private IAccessorLink _child;

        private Func<object, object> _getter;
        private Action<object, object> _setter;

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

        public Type ObjectType { get; set; }

        public Type ValueType { get; set; }

        public bool CanRead => _getter != null;

        public bool CanWrite => _setter != null;

        public object GetValue(object target) => _getter?.Invoke(target);

        public GenericAccessor(Type source, Type field, Func<object, object> getter, Action<object, object> setter)
        {
            ObjectType = source;
            ValueType = field;
            _getter = getter;
            _setter = setter;
        }

        public IAccessor Duplicate() => new GenericAccessor(ObjectType, ValueType, _getter, _setter);

        public virtual void SetValue(object target, object value)
        {
            _setter?.Invoke(target, value);
        }

        public IConcurrentAccessor MakeConcurrent() => Duplicate() as IConcurrentAccessor;
    }
}
