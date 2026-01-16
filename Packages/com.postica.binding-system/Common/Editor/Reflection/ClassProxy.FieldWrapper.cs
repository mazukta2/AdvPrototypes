using System.Reflection;
using System;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public class FieldWrapper : IWrapper
        {
            private string _name;
            private FieldInfo _fieldInfo;
            private Func<object, object> _getter;
            private Action<object, object> _setter;
            
            public ClassProxy ClassProxy { get; set; }
            public IWrapper InitializeAndClone()
            {
                _getter ??= FieldInfo.IsStatic ? FieldInfo.GetValue : FieldInfo.GetGetter();
                _setter ??= FieldInfo.IsStatic ? FieldInfo.SetValue : FieldInfo.GetSetter();
                var clone = (FieldWrapper) MemberwiseClone();
                clone.ClassProxy = ClassProxy;
                return clone;
            }

            public FieldWrapper(string name) => _name = name;
            
            private FieldInfo FieldInfo => _fieldInfo ??= ClassProxy.Type.GetField(_name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            
            public object Value
            {
                get
                {
                    _getter ??= FieldInfo.IsStatic ? FieldInfo.GetValue : FieldInfo.GetGetter();
                    return _getter.Invoke(_fieldInfo.IsStatic ? null : ClassProxy.Instance);
                }
                set
                {
                    _setter ??= FieldInfo.IsStatic ? FieldInfo.SetValue : FieldInfo.GetSetter();
                    _setter.Invoke(_fieldInfo.IsStatic ? null : ClassProxy.Instance, value);
                }
            }
        }
        
        public class FieldWrapper<T> : FieldWrapper
        {
            public FieldWrapper(string name) : base(name) { }
            
            public new T Value
            {
                get => To<T>(base.Value);
                set => base.Value = From<T>(value);
            }
            
            public static implicit operator T(FieldWrapper<T> wrapper) => wrapper.Value;
        }
    }
}