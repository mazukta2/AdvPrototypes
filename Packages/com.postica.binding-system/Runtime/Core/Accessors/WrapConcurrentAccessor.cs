using Postica.BindingSystem.Accessors;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
    public sealed class WrapConcurrentAccessor<S, T> :
            IConcurrentAccessor,
            IConcurrentAccessor<S, T>,
            IConcurrentAccessor<T>,
            IAccessorLink,
            IBoundAccessor<T>

    {
        private readonly object _target;
        private readonly IAccessor _accessor;
        private readonly IAccessor<S, T> _accessorST;
        private readonly IAccessor<T> _accessorT;
        private readonly IAccessorLink _link;
        private readonly IBoundAccessor<T> _boundAccessor;


        public WrapConcurrentAccessor(object target)
        {
            _target = target;
            _accessor = target as IAccessor;
            _accessorST = target as IAccessor<S, T>;
            _accessorT = target as IAccessor<T>;
            _link = target as IAccessorLink;
            _boundAccessor = target as IBoundAccessor<T>;
        }

        public IAccessorLink Previous { get => _link.Previous; set => _link.Previous = value; }
        public IAccessorLink Next { get => _link.Next; set => _link.Next = value; }

        public T GetValue(S target) => _accessorST.GetValue(target);

        public T GetValue(object target) => _accessorT.GetValue(target);

        public T GetValue() => _boundAccessor.GetValue();

        public T GetValueToSet() => _boundAccessor.GetValueToSet();

        public void SetValue(S target, in T value)
        {
            lock (_target) // <-- Could be potentially risky...
            {
                _accessorST.SetValue(ref target, value);
            }
        }

        public void SetValue(object target, in T value)
        {
            lock (_target)
            {
                _accessorT.SetValue(target, value);
            }
        }

        public void SetValue(in T value)
        {
            lock (_target)
            {
                _boundAccessor.SetValue(value);
            }
        }

        public void SetValue(object target, object value)
        {
            lock (_target)
            {
                _accessor.SetValue(target, value);
            }
        }

        object IConcurrentAccessor.GetValue(object target) => _accessor.GetValue(target);
    }
}
