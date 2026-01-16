using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    public delegate void ValueChanged<in T>(T oldValue, T newValue);

    public delegate T ModifyDelegate<T>(in T value);
}
