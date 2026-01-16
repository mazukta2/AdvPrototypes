using System;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public abstract class MethodWrapper : IWrapper
        {
            private MethodProxy _proxy;
            private Type[] _types;
            private Type _returnType;
            private string _name;
            public ClassProxy ClassProxy { get; set; }
            
            public IWrapper InitializeAndClone()
            {
                var clone = (MethodWrapper) MemberwiseClone();
                clone.ClassProxy = ClassProxy;
                clone._proxy = GetProxy();
                return clone;
            }

            public MethodWrapper(string name, Type returnType, params Type[] types)
            {
                _name = name;
                _returnType = returnType;
                _types = types;
            }
            
            public object CallRaw(params object[] args)
            {
                return GetProxy().Call(args);
            }
            
            protected MethodProxy GetProxy()
            {
                _proxy ??= ClassProxy.Method(_name, _returnType, _types);
                _proxy.Instance = ClassProxy.Instance;
                return _proxy;
            }
        }

        public class VoidMethodWrapper : MethodWrapper
        {
            public VoidMethodWrapper(string name) : base(name, typeof(void), Type.EmptyTypes)
            {
            }
            
            public void Call()
            {
                GetProxy().Call();
            }
        }

        public class MethodWrapper<T> : MethodWrapper
        {
            public MethodWrapper(string name) : base(name, typeof(T), Type.EmptyTypes)
            {
            }

            public T Call() => To<T>(GetProxy().Call());
        }
        
        public class MethodWrapper<T1, T> : MethodWrapper
        {
            public MethodWrapper(string name) : base(name, typeof(T), typeof(T1))
            {
            }

            public T Call(T1 arg1) => To<T>(GetProxy().Call(arg1));
        }
        
        public class MethodWrapper<T1, T2, T> : MethodWrapper
        {
            public MethodWrapper(string name) : base(name, typeof(T), typeof(T1), typeof(T2))
            {
            }

            public T Call(T1 arg1, T2 arg2) => To<T>(GetProxy().Call(arg1, arg2));
        }
        
        public class MethodWrapper<T1, T2, T3, T> : MethodWrapper
        {
            public MethodWrapper(string name) : base(name, typeof(T), typeof(T1), typeof(T2), typeof(T3))
            {
            }

            public T Call(T1 arg1, T2 arg2, T3 arg3) => To<T>(GetProxy().Call(arg1, arg2, arg3));
        }

        public class MethodWrapper<T1, T2, T3, T4, T> : MethodWrapper
        {
            public MethodWrapper(string name) : base(name, typeof(T), typeof(T1), typeof(T2),
                typeof(T3), typeof(T4))
            {
            }

            public T Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => To<T>(GetProxy().Call(arg1, arg2, arg3, arg4));
        }
        
        public class VoidMethodWrapper<T> : MethodWrapper
        {
            public VoidMethodWrapper(string name) : base(name, typeof(void), typeof(T))
            {
            }
            
            public void Call(T arg1)
            {
                GetProxy().Call(arg1);
            }
        }
        
        public class VoidMethodWrapper<T1, T2> : MethodWrapper
        {
            public VoidMethodWrapper(string name) : base(name, typeof(void), typeof(T1), typeof(T2))
            {
            }
            
            public void Call(T1 arg1, T2 arg2)
            {
                GetProxy().Call(arg1, arg2);
            }
        }
        
        public class VoidMethodWrapper<T1, T2, T3> : MethodWrapper
        {
            public VoidMethodWrapper(string name) : base(name, typeof(void), typeof(T1), typeof(T2), typeof(T3))
            {
            }
            
            public void Call(T1 arg1, T2 arg2, T3 arg3)
            {
                GetProxy().Call(arg1, arg2, arg3);
            }
        }

        public class VoidMethodWrapper<T1, T2, T3, T4> : MethodWrapper
        {
            public VoidMethodWrapper(string name) : base(name, typeof(void), typeof(T1), typeof(T2), typeof(T3),
                typeof(T4))
            {
            }

            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                GetProxy().Call(arg1, arg2, arg3, arg4);
            }
        }
    }
}