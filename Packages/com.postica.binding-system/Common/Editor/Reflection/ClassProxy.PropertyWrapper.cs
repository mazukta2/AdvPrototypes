using System;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public class PropertyWrapper : IWrapper
        {
            protected MethodProxy _setterProxy;
            protected MethodProxy _getterProxy;
            protected Type _type;
            protected string _name;
            
            public  ClassProxy ClassProxy { get; set; }
            public IWrapper InitializeAndClone()
            {
                _getterProxy ??= ClassProxy.Method($"get_{_name}", _type);
                _setterProxy ??= ClassProxy.Method($"set_{_name}", typeof(void), _type);
                var clone = (PropertyWrapper) MemberwiseClone();
                clone.ClassProxy = ClassProxy;
                return clone;
            }

            public PropertyWrapper(string name, Type type)
            {
                _name = name;
                _type = type;
            }
            
            public object RawValue
            {
                get
                {
                    _getterProxy ??= ClassProxy.Method($"get_{_name}", _type);
                    if(_getterProxy == null) return null;
                    _getterProxy.Instance = ClassProxy.Instance;
                    return _getterProxy.Call();
                }
                set
                {
                    _setterProxy ??= ClassProxy.Method($"set_{_name}", typeof(void), _type);
                    if(_setterProxy == null) return;
                    _setterProxy.Instance = ClassProxy.Instance;
                    _setterProxy.Call(value);
                }
            }
        }

        public class PropertyWrapper<T> : PropertyWrapper
        {
            public PropertyWrapper(string name) : base(name, typeof(T))
            {
            }

            public T Value
            {
                get => To<T>(RawValue);
                set => RawValue = From<T>(value);
            }
            
            public static implicit operator T(PropertyWrapper<T> wrapper) => wrapper != null ? wrapper.Value : default;
        }
    }
}