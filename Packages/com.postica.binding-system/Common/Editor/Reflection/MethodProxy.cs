using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Postica.Common.Reflection
{
    public abstract class MethodProxy
    {
        private Type[] _parameterTypes;
        public string Key { get; }
        public bool IsStatic => MethodInfo.IsStatic;
        public object Instance { get; set; }
        public Type Type { get; }
        public string Name { get; }

        protected MethodInfo MethodInfo { get; }

        protected MethodProxy(string key, object instance, Type type, string name)
        {
            Key = key;
            Instance = instance;
            Type = instance?.GetType() ?? type;
            Name = name;

            MethodInfo = GetMethodInfo();
        }
        
        private static Type Refit(Type type)
        {
            var attribute = type.GetCustomAttribute<ClassProxy.ForAttribute>();
            return attribute != null ? attribute.Type : type;
        }

        public abstract object Call(params object[] args);

        public override bool Equals(object obj)
        {
            return obj is MethodProxy proxy && Key == proxy.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        private MethodInfo GetMethodInfo()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Static;
            var methodInfo = Type?.GetMethods(flags).FirstOrDefault(IsMatch);
            return methodInfo;
        }

        protected abstract Type[] GetParameterTypes();
        protected abstract Type GetReturnType();

        protected static T To<T>(object value)
        {
            if (value is ClassProxy classProxy)
            {
                return To<T>(classProxy.Instance);
            }

            return (T)value;
        }

        private bool IsMatch(MethodInfo method)
        {
            if (method.Name != Name)
            {
                return false;
            }

            if (method.ReturnParameter?.ParameterType != Refit(GetReturnType()))
            {
                return false;
            }

            _parameterTypes ??= GetParameterTypes();
            var parameters = method.GetParameters();
            if (parameters.Length != _parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < _parameterTypes.Length; i++)
            {
                if (parameters[i].ParameterType != _parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal class EmptyMethodProxy : MethodProxy
    {
        public EmptyMethodProxy(string key) : base(key, null, null, null)
        {
        }

        public override object Call(params object[] args)
        {
            return null;
        }

        protected override Type[] GetParameterTypes() => Type.EmptyTypes;

        protected override Type GetReturnType() => typeof(void);
    }

    internal class VoidMethodProxy : MethodProxy
    {
        private Action _staticAction;
        private Action<object> _action;

        public VoidMethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticAction = (Action)Delegate.CreateDelegate(typeof(Action), MethodInfo);
            }
            else
            {
                _action = CreateAction(MethodInfo);
            }
        }
        
        private static Action<object> CreateAction(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo);
            return Expression.Lambda<Action<object>>(call, instance).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                _staticAction();
            }
            else
            {
                _action(Instance);
            }
            return null;
        }
        
        public void Call()
        {
            if (IsStatic)
            {
                _staticAction();
            }
            else
            {
                _action(Instance);
            }
        }

        protected override Type[] GetParameterTypes() => Type.EmptyTypes;

        protected override Type GetReturnType() => typeof(void);
    }

    internal class VoidMethodProxy<T1> : MethodProxy
    {
        private Action<T1> _staticAction;
        private Action<object, T1> _action;

        public VoidMethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (MethodInfo == null)
            {
                return;
            }
            
            if (IsStatic)
            {
                _staticAction = (Action<T1>)Delegate.CreateDelegate(typeof(Action<T1>), MethodInfo);
            }
            else
            {
                _action = CreateAction(MethodInfo);
            }
        }
        
        private static Action<object, T1> CreateAction(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1);
            return Expression.Lambda<Action<object, T1>>(call, instance, arg1).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                _staticAction?.Invoke(To<T1>(args[0]));
            }
            else
            {
                _action?.Invoke(Instance, To<T1>(args[0]));
            }
            return null;
        }
        
        public void Call(T1 arg1)
        {
            if(IsStatic)
            {
                _staticAction?.Invoke(arg1);
            }
            else
            {
                _action?.Invoke(Instance, arg1);
            }
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1) };

        protected override Type GetReturnType() => typeof(void);
    }

    internal class MethodProxy<TResult> : MethodProxy
    {
        private Func<TResult> _staticAction;
        private Func<object, TResult> _action;

        public MethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (MethodInfo == null)
            {
                return;
            }
            
            if (IsStatic)
            {
                _staticAction = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), MethodInfo);
            }
            else
            {
                _action = CreateFunc(MethodInfo);
            }
        }
        
        private static Func<object, TResult> CreateFunc(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo);
            return Expression.Lambda<Func<object, TResult>>(call, instance).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                return _staticAction != null ? _staticAction() : null;
            }

            return _action != null ? _action(Instance) : null;
        }
        
        public TResult Call()
        {
            if (IsStatic)
            {
                return _staticAction != null ? _staticAction() : default;
            }

            return _action != null ? _action(Instance) : default;
        }

        protected override Type[] GetParameterTypes() => Type.EmptyTypes;

        protected override Type GetReturnType() => typeof(TResult);
    }

    internal class MethodProxy<T1, TResult> : MethodProxy
    {
        private Func<T1, TResult> _staticFunc;
        private Func<object, T1, TResult> _func;

        public MethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticFunc = (Func<T1, TResult>)Delegate.CreateDelegate(typeof(Func<T1, TResult>), MethodInfo);
            }
            else
            {
                _func = CreateFunc(MethodInfo);
            }
        }
        
        private static Func<object, T1, TResult> CreateFunc(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1);
            return Expression.Lambda<Func<object, T1, TResult>>(call, instance, arg1).Compile();
        }

        public override object Call(params object[] args)
        {
            return IsStatic ? _staticFunc(To<T1>(args[0])) : _func(Instance, To<T1>(args[0]));
        }
        
        public TResult Call(T1 arg1)
        {
            return IsStatic ? _staticFunc(arg1) : _func(Instance, arg1);
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1) };

        protected override Type GetReturnType() => typeof(TResult);
    }

    internal class VoidMethodProxy<T1, T2> : MethodProxy
    {
        private Action<T1, T2> _staticAction;
        private Action<object, T1, T2> _action;

        public VoidMethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticAction = (Action<T1, T2>)Delegate.CreateDelegate(typeof(Action<T1, T2>), MethodInfo);
            }
            else
            {
                _action = CreateAction(MethodInfo);
            }
        }
        
        private static Action<object, T1, T2> CreateAction(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2);
            return Expression.Lambda<Action<object, T1, T2>>(call, instance, arg1, arg2).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                _staticAction(To<T1>(args[0]), To<T2>(args[1]));
            }
            else
            {
                _action(Instance, To<T1>(args[0]), To<T2>(args[1]));
            }
            return null;
        }
        
        public void Call(T1 arg1, T2 arg2)
        {
            if (IsStatic)
            {
                _staticAction(arg1, arg2);
            }
            else
            {
                _action(Instance, arg1, arg2);
            }
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2) };

        protected override Type GetReturnType() => typeof(void);
    }

    internal class MethodProxy<T1, T2, TResult> : MethodProxy
    {
        private Func<T1, T2, TResult> _staticFunc;
        private Func<object, T1, T2, TResult> _func;

        public MethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticFunc = (Func<T1, T2, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, TResult>), MethodInfo);
            }
            else
            {
                _func = CreateFunc(MethodInfo);
            }
        }
        
        private static Func<object, T1, T2, TResult> CreateFunc(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2);
            return Expression.Lambda<Func<object, T1, T2, TResult>>(call, instance, arg1, arg2).Compile();
        }

        public override object Call(params object[] args)
        {
            if(_staticFunc != null)
            {
                return _staticFunc(To<T1>(args[0]), To<T2>(args[1]));
            }
            
            return _func(Instance, To<T1>(args[0]), To<T2>(args[1]));
        }
        
        public TResult Call(T1 arg1, T2 arg2)
        {
            if(_staticFunc != null)
            {
                return _staticFunc(arg1, arg2);
            }
            return _func(Instance, arg1, arg2);
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2) };

        protected override Type GetReturnType() => typeof(TResult);
    }

    internal class VoidMethodProxy<T1, T2, T3> : MethodProxy
    {
        private Action<T1, T2, T3> _staticAction;
        private Action<object, T1, T2, T3> _action;

        public VoidMethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticAction = (Action<T1, T2, T3>)Delegate.CreateDelegate(typeof(Action<T1, T2, T3>), MethodInfo);
            }
            else
            {
                _action = CreateAction(MethodInfo);
            }
        }
        
        private static Action<object, T1, T2, T3> CreateAction(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var arg3 = Expression.Parameter(typeof(T3), "arg3");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2, arg3);
            return Expression.Lambda<Action<object, T1, T2, T3>>(call, instance, arg1, arg2, arg3).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                _staticAction(To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]));
            }
            else
            {
                _action(Instance, To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]));
            }
            return null;
        }
        
        public void Call(T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsStatic)
            {
                _staticAction(arg1, arg2, arg3);
            }
            else
            {
                _action(Instance, arg1, arg2, arg3);
            }
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2), typeof(T3) };

        protected override Type GetReturnType() => typeof(void);
    }

    internal class MethodProxy<T1, T2, T3, TResult> : MethodProxy
    {
        private Func<T1, T2, T3, TResult> _staticFunc;
        private Func<object, T1, T2, T3, TResult> _func;

        public MethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticFunc = (Func<T1, T2, T3, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, TResult>), MethodInfo);
            }
            else
            {
                _func = CreateFunc(MethodInfo);
            }
        }
        
        private static Func<object, T1, T2, T3, TResult> CreateFunc(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var arg3 = Expression.Parameter(typeof(T3), "arg3");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2, arg3);
            return Expression.Lambda<Func<object, T1, T2, T3, TResult>>(call, instance, arg1, arg2, arg3).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                return _staticFunc(To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]));
            }
            return _func(Instance, To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]));
        }
        
        public TResult Call(T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsStatic)
            {
                return _staticFunc(arg1, arg2, arg3);
            }
            return _func(Instance, arg1, arg2, arg3);
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2), typeof(T3) };

        protected override Type GetReturnType() => typeof(TResult);
    }

    internal class VoidMethodProxy<T1, T2, T3, T4> : MethodProxy
    {
        private Action<T1, T2, T3, T4> _staticAction;
        private Action<object, T1, T2, T3, T4> _action;

        public VoidMethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticAction = (Action<T1, T2, T3, T4>)Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4>), MethodInfo);
            }
            else
            {
                _action = CreateAction(MethodInfo);
            }
        }
        
        private static Action<object, T1, T2, T3, T4> CreateAction(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var arg3 = Expression.Parameter(typeof(T3), "arg3");
            var arg4 = Expression.Parameter(typeof(T4), "arg4");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2, arg3, arg4);
            return Expression.Lambda<Action<object, T1, T2, T3, T4>>(call, instance, arg1, arg2, arg3, arg4).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                _staticAction(To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]), To<T4>(args[3]));
            }
            else
            {
                _action(Instance, To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]), To<T4>(args[3]));
            }
            return null;
        }
        
        public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (IsStatic)
            {
                _staticAction(arg1, arg2, arg3, arg4);
            }
            else
            {
                _action(Instance, arg1, arg2, arg3, arg4);
            }
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };

        protected override Type GetReturnType() => typeof(void);
    }

    public class MethodProxy<T1, T2, T3, T4, TResult> : MethodProxy
    {
        private Func<T1, T2, T3, T4, TResult> _staticFunc;
        private Func<object, T1, T2, T3, T4, TResult> _func;

        public MethodProxy(string key, object instance, Type type, string name) : base(key, instance, type, name)
        {
            if (IsStatic)
            {
                _staticFunc = (Func<T1, T2, T3, T4, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, T4, TResult>), MethodInfo);
            }
            else
            {
                _func = CreateFunc(MethodInfo);
            }
        }
        
        private static Func<object, T1, T2, T3, T4, TResult> CreateFunc(MethodInfo methodInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg1 = Expression.Parameter(typeof(T1), "arg1");
            var arg2 = Expression.Parameter(typeof(T2), "arg2");
            var arg3 = Expression.Parameter(typeof(T3), "arg3");
            var arg4 = Expression.Parameter(typeof(T4), "arg4");
            var call = Expression.Call(Expression.Convert(instance, methodInfo.DeclaringType), methodInfo, arg1, arg2, arg3, arg4);
            return Expression.Lambda<Func<object, T1, T2, T3, T4, TResult>>(call, instance, arg1, arg2, arg3, arg4).Compile();
        }

        public override object Call(params object[] args)
        {
            if (IsStatic)
            {
                return _staticFunc(To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]), To<T4>(args[3]));
            }
            return _func(Instance, To<T1>(args[0]), To<T2>(args[1]), To<T3>(args[2]), To<T4>(args[3]));
        }
        
        public TResult Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (IsStatic)
            {
                return _staticFunc(arg1, arg2, arg3, arg4);
            }
            return _func(Instance, arg1, arg2, arg3, arg4);
        }

        protected override Type[] GetParameterTypes() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };

        protected override Type GetReturnType() => typeof(TResult);
    }
}