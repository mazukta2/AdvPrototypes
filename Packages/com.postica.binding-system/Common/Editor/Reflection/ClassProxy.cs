using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Postica.Common.Reflection
{
    public class ClassProxy<T> : ClassProxy
    {
        public new T Instance
        {
            get => (T) base.Instance;
            set => base.Instance = value;
        }
        
        public ClassProxy() : base(typeof(T))
        {
            
        }
        
        public ClassProxy(T instance) : this()
        {
            Instance = instance;
        }    
        
        public static implicit operator T(ClassProxy<T> proxy) => proxy.Instance;
        public static implicit operator ClassProxy<T>(T instance) => new ClassProxy<T>(instance);
    }
    
    public partial class ClassProxy
    {
        public interface IWrapper
        {
            ClassProxy ClassProxy { get; set; }
            IWrapper InitializeAndClone();
        }
        
        public interface ISafeProxy
        {
            void SetInstance(object instance);
        }
        
        private readonly Dictionary<IWrapper, IWrapper> _instanceWrappers = new();
        
        private List<MethodProxy> _methods = new();
        public object Instance { get; protected set; }
        public Type Type { get; }
        
        protected ClassProxy(Type type)
        {
            Type = type;
        }
        
        public ClassProxy()
        {
            Type = GetType().GetCustomAttribute<ForAttribute>().Type;
        }
        
        public ClassProxy(object instance) : this()
        {
            Instance = instance is ClassProxy proxy ? proxy.Instance : instance;
        }

        public void NewInstance()
        {
            Instance = Activator.CreateInstance(Type);
        }

        protected T This<T>(T wrapper) where T : IWrapper
        {
            if (_instanceWrappers.TryGetValue(wrapper, out var instanceWrapper))
            {
                return (T) instanceWrapper;
            }
            
            wrapper.ClassProxy = this;
            var result = (T) wrapper.InitializeAndClone();
            _instanceWrappers[wrapper] = result;
            return result;
        }
        
        protected T Static<T>(T wrapper) where T : IWrapper
        {
            wrapper.ClassProxy = this;
            return wrapper;
        }

        protected void InitializeWrappers(params IWrapper[] wrappers)
        {
            foreach (var wrapper in wrappers)
            {
                if (wrapper == null)
                {
                    continue;
                }
                wrapper.ClassProxy = this;
            }
        }
        
        public MethodProxy Method(string methodName, Type returnType, params Type[] types)
        {
            var key = GetKey(methodName, types);
            var method = _methods.Find(m => m.Key == key);
            if (method != null) return method;

            for (int i = 0; i < types.Length; i++)
            {
                types[i] = Refit(types[i]);
            }

            returnType = Refit(returnType);

            Type methodType = null;
            if (returnType == typeof(void))
            {
                methodType = types.Length switch
                {
                    0 => typeof(VoidMethodProxy),
                    1 => typeof(VoidMethodProxy<>).MakeGenericType(types[0]),
                    2 => typeof(VoidMethodProxy<,>).MakeGenericType(types[0], types[1]),
                    3 => typeof(VoidMethodProxy<,,>).MakeGenericType(types[0], types[1], types[2]),
                    4 => typeof(VoidMethodProxy<,,,>).MakeGenericType(types[0], types[1], types[2], types[3]),
                    _ => throw new NotSupportedException("Method with no return type is not supported")
                };
            }
            else
            {
                methodType = types.Length switch
                {
                    0 => typeof(MethodProxy<>).MakeGenericType(returnType),
                    1 => typeof(MethodProxy<,>).MakeGenericType(types[0], returnType),
                    2 => typeof(MethodProxy<,,>).MakeGenericType(types[0], types[1], returnType),
                    3 => typeof(MethodProxy<,,,>).MakeGenericType(types[0], types[1], types[2], returnType),
                    4 => typeof(MethodProxy<,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], returnType),
                    _ => throw new NotSupportedException("Method with no return type is not supported")
                };
            }
            
            method = (MethodProxy) Activator.CreateInstance(methodType, key, Instance, Type, methodName);
            
            _methods.Add(method ?? new EmptyMethodProxy(key));
            return method;
        }
        
        #region [  KEY RETRIEVAL METHODS  ]

        private static T To<T>(object value)
        {
            if (!typeof(ClassProxy).IsAssignableFrom(typeof(T)) || value is ClassProxy) return (T)value;
            
            if(value == null) return default;
            
            var proxy = Activator.CreateInstance<T>() as ClassProxy;
            proxy.Instance = value;
            return proxy is T tp ? tp : default;
        }
        
        private static object From<T>(object value)
        {
            if (typeof(ClassProxy).IsAssignableFrom(typeof(T)) && value is ClassProxy proxy)
            {
                return proxy.Instance;
            }
            return value;
        }
        
        private static Type Refit(Type type)
        {
            var attribute = type.GetCustomAttribute<ForAttribute>();
            return attribute != null ? attribute.Type : type;
        }
        
        private static string GetKey(string name, params Type[] args)
        {
            // Use StringBuilder for better performance
            var sb = new StringBuilder(name);
            sb.Append("(");
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    sb.Append(args[i].FullName).Append(',');
                }
                sb.Length--;
            }
            sb.Append(")");
            return sb.ToString();
        }
        
        #endregion
    }
}