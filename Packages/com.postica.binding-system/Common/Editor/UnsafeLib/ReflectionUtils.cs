
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Postica.Common.Reflection
{
    public static class MethodUtils
    {
        public static MethodInfo GetMethod(this Type type, string methodName, Type returnType, params Type[] parameterTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public
                                          | BindingFlags.NonPublic
                                          | BindingFlags.Static
                                          | BindingFlags.Instance);
            return methods.First(m =>
                m.Name == methodName && m.ReturnType == returnType && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetVoidMethod(this Type type, string methodName)
            => GetMethod(type, methodName, typeof(void));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetVoidMethod<TParam1>(this Type type, string methodName)
        => GetMethod(type, methodName, typeof(void), typeof(TParam1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetVoidMethod<TParam1, TParam2>(this Type type, string methodName) =>
            GetMethod(type, methodName, typeof(void), typeof(TParam1), typeof(TParam2));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetVoidMethod<TParam1, TParam2, TParam3>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(void), typeof(TParam1), typeof(TParam2), typeof(TParam3));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetVoidMethod<TParam1, TParam2, TParam3, TParam4>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(void), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetReturnMethod<TReturn>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(TReturn));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetReturnMethod<TParam1, TReturn>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(TReturn), typeof(TParam1));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetReturnMethod<TParam1, TParam2, TReturn>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(TReturn), typeof(TParam1), typeof(TParam2));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetReturnMethod<TParam1, TParam2, TParam3, TReturn>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(TReturn), typeof(TParam1), typeof(TParam2), typeof(TParam3));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetReturnMethod<TParam1, TParam2, TParam3, TParam4, TReturn>(this Type type, string methodName) => 
            GetMethod(type, methodName, typeof(TReturn), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetMethodFromDelegate<TDelegate>(TDelegate del) where TDelegate : Delegate
        {
            return del.Method;
        }
        
        public static MethodInfo GetMethod(Action action) => GetMethodFromDelegate<Delegate>(action);
        public static MethodInfo GetMethod<T>(Action<T> action) => GetMethodFromDelegate<Delegate>(action);
        public static MethodInfo GetMethod<T1, T2>(Action<T1, T2> action) => GetMethodFromDelegate<Delegate>(action);
        public static MethodInfo GetMethod<T1, T2, T3>(Action<T1, T2, T3> action) => GetMethodFromDelegate<Delegate>(action);
        public static MethodInfo GetMethod<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) => GetMethodFromDelegate<Delegate>(action);
        public static MethodInfo GetMethod<T>(Func<T> func) => GetMethodFromDelegate<Delegate>(func);
        public static MethodInfo GetMethod<T1, T2>(Func<T1, T2> func) => GetMethodFromDelegate<Delegate>(func);
        public static MethodInfo GetMethod<T1, T2, T3>(Func<T1, T2, T3> func) => GetMethodFromDelegate<Delegate>(func);
        public static MethodInfo GetMethod<T1, T2, T3, T4>(Func<T1, T2, T3, T4> func) => GetMethodFromDelegate<Delegate>(func);
    }
}