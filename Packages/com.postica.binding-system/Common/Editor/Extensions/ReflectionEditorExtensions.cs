using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Postica.Common.Utilities;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public static class ReflectionEditorExtensions
    {
        private static readonly Dictionary<FieldInfo, (Func<object, object> getter, Action<object, object> setter)> _fieldsCache
            = new Dictionary<FieldInfo, (Func<object, object> getter, Action<object, object> setter)>();

        private static readonly Dictionary<MethodInfo, Func<object, object, object>> _methodCalls = new Dictionary<MethodInfo, Func<object, object, object>>();

        private delegate TRet FuncRef<T1, out TRet>(in T1 value);
        private delegate TRet FuncRef<S, T1, out TRet>(S source, in T1 value);
        
        public static bool TryGetDescription(this Type type, out string description)
        {
            var attribute = type.GetCustomAttribute<TypeDescriptionAttribute>(true);
            if (attribute != null && !string.IsNullOrEmpty(attribute.Description))
            {
                description = attribute.Description;
                return true;
            }
            
            description = DocumentationUtils.GetClassSummary(type);
            return !string.IsNullOrEmpty(description);
        }

        public static Action<T, S> FieldSetter<T, S>(string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fieldInfo = typeof(T).GetField(fieldName, flags);

            var sourceParam = Expression.Parameter(typeof(T));
            var valueParam = Expression.Parameter(typeof(S));
            Expression body = Expression.Assign(Expression.Field(sourceParam, fieldInfo), valueParam);
            var lambda = Expression.Lambda(typeof(Action<T, S>), body, sourceParam, valueParam);
            return (Action<T, S>)lambda.Compile();
        }

        public static Func<T, S> FieldGetter<T, S>(string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fieldInfo = typeof(T).GetField(fieldName, flags);

            var sourceParam = Expression.Parameter(typeof(T));
            Expression returnExpression = Expression.Field(sourceParam, fieldInfo);
            var lambda = Expression.Lambda<Func<T, S>>(returnExpression, sourceParam);
            return lambda.Compile();
        }

        internal static Func<T, S> FieldGetter<T, S>(this Type type, string fieldName)
        {
            return FieldGetter<T, S>(fieldName);
        }

        public static Func<object, object> GetGetter(this FieldInfo fieldInfo)
        {
            if (fieldInfo == null) { return default; }

            if (!_fieldsCache.TryGetValue(fieldInfo, out var accessor))
            {
                accessor = GetFieldAccessor(fieldInfo);
                _fieldsCache[fieldInfo] = accessor;
            }
            return accessor.getter;
        }

        public static object GetFastValue(this FieldInfo fieldInfo, object target)
        {
            try
            {
                return target == null ? null : GetGetter(fieldInfo)?.Invoke(target);
            }
            catch (InvalidCastException)
            {
                //System.Diagnostics.Debugger.Break();
                return null;
            }
        }
        
        public static void SetFieldValue(this object target, string fieldName, object value)
        {
            var fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fieldInfo?.SetFastValue(target, value);
        }

        public static Action<object, object> GetSetter(this FieldInfo fieldInfo)
        {
            if (fieldInfo == null) { return default; }

            if (!_fieldsCache.TryGetValue(fieldInfo, out var accessor))
            {
                accessor = GetFieldAccessor(fieldInfo);
                _fieldsCache[fieldInfo] = accessor;
            }
            return accessor.setter;
        }

        public static void SetFastValue(this FieldInfo fieldInfo, object target, object value) => GetSetter(fieldInfo)?.Invoke(target, value);

        public static (Func<object, object> getter, Action<object, object> setter) GetFieldAccessor(FieldInfo fieldInfo)
        {
            Func<object, object> getter = null;
            Action<object, object> setter = null;

            var source = fieldInfo.DeclaringType;
            var sourceParam = Expression.Parameter(typeof(object));

            //if (fieldInfo.DeclaringType.IsValueType)
            //{
            //    getter = t => fieldInfo.GetValue(t);
            //}
            //else
            {
                Expression returnExpression = Expression.Field(Expression.Convert(sourceParam, source), fieldInfo);
                if (!fieldInfo.FieldType.IsClass)
                {
                    returnExpression = Expression.Convert(returnExpression, typeof(object));
                }
                var getterLambda = Expression.Lambda<Func<object, object>>(returnExpression, sourceParam);
                getter = getterLambda.Compile();
            }

            // if (!fieldInfo.IsInitOnly)
            {
                if (fieldInfo.DeclaringType.IsValueType)
                {
                    setter = (o, v) => fieldInfo.SetValue(o, v);
                }
                else
                {
                    var valueParam = Expression.Parameter(typeof(object));
                    var convertedValue = Expression.Convert(valueParam, fieldInfo.FieldType);
                    Expression body = Expression.Assign(Expression.Field(Expression.Convert(sourceParam, source), fieldInfo), convertedValue);
                    if (!fieldInfo.FieldType.IsClass)
                    {
                        body = Expression.Convert(body, typeof(object));
                    }
                    var setterLambda = Expression.Lambda<Action<object, object>>(body, sourceParam, valueParam);
                    setter = setterLambda.Compile();
                }
            }
            return (getter, setter);
        }
        
        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            TSource source,
            Expression<Func<TSource, TProperty>> propertyLambda)
         => GetPropertyInfo(propertyLambda);
        
        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(
            Expression<Func<TSource, TProperty>> propertyLambda)
        {
            if (propertyLambda.Body is not MemberExpression member)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));
            }

            if (member.Member is not PropertyInfo propInfo)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));
            }

            Type type = typeof(TSource);
            if (propInfo.ReflectedType != null && type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));
            }

            return propInfo;
        }

        internal static Func<object, object> GetAsFunc(this MethodInfo methodInfo, object instance)
        {
            if (_methodCalls.TryGetValue(methodInfo, out var func))
            {
                return v => func(instance, v);
            }
            var parameters = methodInfo.GetParameters();
            if (methodInfo.ReturnType == typeof(void) || parameters?.Length != 1)
            {
                throw new ArgumentException("Invalid method signature for a simple function. Should be <returnType> Method(<paramType> param1)");
            }

            var proxyCallType = parameters[0].IsIn ? typeof(ProxyCallByRef<,,>).MakeGenericType(methodInfo.DeclaringType, GetParameterType(parameters[0].ParameterType), methodInfo.ReturnType)
                                                    : typeof(ProxyCall<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, methodInfo.ReturnType);
            var funcPrototype = parameters[0].IsIn ? typeof(FuncRef<,,>).MakeGenericType(methodInfo.DeclaringType, GetParameterType(parameters[0].ParameterType), methodInfo.ReturnType)
                                                   : typeof(Func<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, methodInfo.ReturnType);
            var funcInstance = Delegate.CreateDelegate(funcPrototype, methodInfo);
            var proxyCall = (ProxyCall)Activator.CreateInstance(proxyCallType, funcInstance);

            _methodCalls[methodInfo] = proxyCall.Call;

            return v => proxyCall.Call(instance, v);
        }

        private static Type GetParameterType(Type parameterType)
        {
            if (parameterType.FullName?.EndsWith("&") == true)
            {
                var type = Type.GetType(parameterType.AssemblyQualifiedName.Replace("&", ""));
                return type ?? parameterType;
            }
            return parameterType;
        }

        private abstract class ProxyCall
        {
            public abstract object Call(object instance, object param);
        }

        private class ProxyCall<S, T, TRet> : ProxyCall
        {
            private readonly Func<S, T, TRet> _call;

            public ProxyCall(object call)
            {
                _call = (Func<S, T, TRet>)call;
            }

            public override object Call(object instance, object param) => param == null ? _call((S)instance, default)
                                                                                        : _call((S)instance, (T)param);
        }

        private class ProxyCallByRef<S, T, TRet> : ProxyCall
        {
            private readonly FuncRef<S, T, TRet> _call;

            public ProxyCallByRef(object call)
            {
                _call = (FuncRef<S, T, TRet>)call;
            }

            public override object Call(object instance, object param) => param == null ? _call((S)instance, default)
                                                                                        : _call((S)instance, (T)param);
        }
    }
}