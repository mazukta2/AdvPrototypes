using System;
using System.Reflection;
using UnityEngine;

namespace Postica.BindingSystem
{
    public static class MethodFactory
    {
        public delegate T ReturnDelegate<S, T>(S source);
        public delegate T ReturnDelegate<S, T, P1>(S source, P1 p1);
        public delegate T ReturnDelegate<S, T, P1, P2>(S source, P1 p1, P2 p2);
        public delegate T ReturnDelegate<S, T, P1, P2, P3>(S source, P1 p1, P2 p2, P3 p3);
        public delegate T ReturnDelegate<S, T, P1, P2, P3, P4>(S source, P1 p1, P2 p2, P3 p3, P4 p4);

        public delegate T ReturnValueDelegate<S, T>(in S source);
        public delegate T ReturnValueDelegate<S, T, P1>(in S source, P1 p1);
        public delegate T ReturnValueDelegate<S, T, P1, P2>(in S source, P1 p1, P2 p2);
        public delegate T ReturnValueDelegate<S, T, P1, P2, P3>(in S source, P1 p1, P2 p2, P3 p3);
        public delegate T ReturnValueDelegate<S, T, P1, P2, P3, P4>(in S source, P1 p1, P2 p2, P3 p3, P4 p4);

        public delegate void VoidDelegate<S>(S source);
        public delegate void VoidDelegate<S, P1>(S source, P1 p1);
        public delegate void VoidDelegate<S, P1, P2>(S source, P1 p1, P2 p2);
        public delegate void VoidDelegate<S, P1, P2, P3>(S source, P1 p1, P2 p2, P3 p3);
        public delegate void VoidDelegate<S, P1, P2, P3, P4>(S source, P1 p1, P2 p2, P3 p3, P4 p4);

        public delegate void VoidValueDelegate<S>(in S source);
        public delegate void VoidValueDelegate<S, P1>(in S source, P1 p1);
        public delegate void VoidValueDelegate<S, P1, P2>(in S source, P1 p1, P2 p2);
        public delegate void VoidValueDelegate<S, P1, P2, P3>(in S source, P1 p1, P2 p2, P3 p3);
        public delegate void VoidValueDelegate<S, P1, P2, P3, P4>(in S source, P1 p1, P2 p2, P3 p3, P4 p4);

        private class ConstantProvider<T> : IValueProvider<T>
        {
            private readonly T _value;
            public ConstantProvider(T value) => _value = value;
            public ConstantProvider(object value) => _value = (T)value;
            public T Value => _value;
            public object UnsafeValue => _value;
        }

        private class CastProvider<T> : IValueProvider<T>
        {
            private readonly IValueProvider _provider;
            public CastProvider(IValueProvider provider) => _provider = provider;
            public T Value => (T)_provider.UnsafeValue;
            public object UnsafeValue => _provider.UnsafeValue;
        }

        private class ConstantProvider : IValueProvider
        {
            private readonly object _value;
            public ConstantProvider(object value) => _value = value;
            public object UnsafeValue => _value;
        }

        public static BaseReturnMethod<S, T> GetReturnMethodFor<S, T>(MethodInfo method)
        {
            if (!method.ReturnType.IsAssignableFrom(typeof(T)))
            {
                Debug.LogException(new ArgumentException("Method not compatible with requested getter type", nameof(method)));
                return null;
            }

            var methodParams = method.GetParameters();
            if(methodParams.Length > 4)
            {
                Debug.LogException(new ArgumentException("Method parameters are too many to be handled", nameof(method)));
                return null;
            }

            try
            {
                Type getterType;
                switch (methodParams.Length)
                {
                    case 0:
                        return method.DeclaringType.IsValueType ? (BaseReturnMethod<S, T>)new ReturnValueMethod<S, T>(method) : new ReturnMethod<S, T>(method);
                    case 1:
                        var methodType = method.DeclaringType.IsValueType ? typeof(ReturnValueMethod<,,>) : typeof(ReturnMethod<,,>);
                        getterType = methodType.MakeGenericType(typeof(S), typeof(T), methodParams[0].ParameterType);
                        return (BaseReturnMethod<S, T>)Activator.CreateInstance(getterType, method);
                    case 2:
                        methodType = method.DeclaringType.IsValueType ? typeof(ReturnValueMethod<,,,>) : typeof(ReturnMethod<,,,>);
                        getterType = methodType.MakeGenericType(typeof(S), typeof(T),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType);
                        return (BaseReturnMethod<S, T>)Activator.CreateInstance(getterType, method);
                    case 3:
                        methodType = method.DeclaringType.IsValueType ? typeof(ReturnValueMethod<,,,,>) : typeof(ReturnMethod<,,,,>);
                        getterType = methodType.MakeGenericType(typeof(S), typeof(T),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType);
                        return (BaseReturnMethod<S, T>)Activator.CreateInstance(getterType, method);
                    case 4:
                        methodType = method.DeclaringType.IsValueType ? typeof(ReturnValueMethod<,,,,,>) : typeof(ReturnMethod<,,,,,>);
                        getterType = methodType.MakeGenericType(typeof(S), typeof(T),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType,
                                            methodParams[3].ParameterType);
                        return (BaseReturnMethod<S, T>)Activator.CreateInstance(getterType, method);
                }

                return new FallbackReturnMethod<S, T>(method);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public static BaseVoidMethod<S> GetVoidMethodFor<S>(MethodInfo method)
        {
            var methodParams = method.GetParameters();
            if (methodParams.Length > 4)
            {
                Debug.LogException(new ArgumentException("Method parameters are too many to be handled", nameof(method)));
                return null;
            }

            Type setterType = null;
            try
            {
                switch (methodParams.Length)
                {
                    case 0:
                        return method.DeclaringType.IsValueType ? (BaseVoidMethod<S>)new VoidValueMethod<S>(method) : new VoidMethod<S>(method);
                    case 1:
                        var methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,>) : typeof(VoidMethod<,>);
                        setterType = methodType.MakeGenericType(typeof(S), methodParams[0].ParameterType);
                        return (BaseVoidMethod<S>)Activator.CreateInstance(setterType, method);
                    case 2:
                        methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,>) : typeof(VoidMethod<,,>);
                        setterType = methodType.MakeGenericType(typeof(S),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType);
                        return (BaseVoidMethod<S>)Activator.CreateInstance(setterType, method);
                    case 3:
                        methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,,>) : typeof(VoidMethod<,,,>);
                        setterType = methodType.MakeGenericType(typeof(S),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType);
                        return (BaseVoidMethod<S>)Activator.CreateInstance(setterType, method);
                    case 4:
                        methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,,,>) : typeof(VoidMethod<,,,,>);
                        setterType = methodType.MakeGenericType(typeof(S), 
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType,
                                            methodParams[3].ParameterType);
                        return (BaseVoidMethod<S>)Activator.CreateInstance(setterType, method);
                }

                return new FallbackVoidMethod<S>(method);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public static BaseSetterMethod<S, T> GetSetterMethodFor<S, T>(MethodInfo method)
        {
            var methodParams = method.GetParameters();
            if (methodParams.Length > 4)
            {
                Debug.LogException(new ArgumentException("Method parameters are too many to be handled", nameof(method)));
                return null;
            }

            if (!methodParams[methodParams.Length - 1].ParameterType.IsAssignableFrom(typeof(T)))
            {
                Debug.LogException(new ArgumentException("Method not compatible with requested setter type", nameof(method)));
                return null;
            }

            Type setterType = null;
            try
            {
                switch (methodParams.Length)
                {
                    case 2:
                        var methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,>) : typeof(VoidMethod<,,>);
                        setterType = methodType.MakeGenericType(typeof(S),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType);
                        return (BaseSetterMethod<S, T>)Activator.CreateInstance(setterType, method);
                    case 3:
                        methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,,>) : typeof(VoidMethod<,,,>);
                        setterType = methodType.MakeGenericType(typeof(S),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType);
                        return (BaseSetterMethod<S, T>)Activator.CreateInstance(setterType, method);
                    case 4:
                        methodType = method.DeclaringType.IsValueType ? typeof(VoidValueMethod<,,,,>) : typeof(VoidMethod<,,,,>);
                        setterType = methodType.MakeGenericType(typeof(S),
                                            methodParams[0].ParameterType,
                                            methodParams[1].ParameterType,
                                            methodParams[2].ParameterType,
                                            methodParams[3].ParameterType);
                        return (BaseSetterMethod<S, T>)Activator.CreateInstance(setterType, method);
                }

                return new FallbackVoidMethod<S, T>(method);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        #region [  RETURN METHODS  ]

        public abstract class BaseReturnMethod<S, T>
        {
            public bool AssignParameters(object[] parameters)
            {
                try
                {
                    Build(parameters);
                    return true;
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }

            protected abstract void Build(object[] parameters);
            public abstract T Invoke(in S source);
            public abstract BaseReturnMethod<S, T> Duplicate();

            protected static IValueProvider<P> BuildParameter<P>(object provider)
            {
                if (provider is IValueProvider<P> specialisedProvider)
                {
                    return specialisedProvider;
                }
                if (provider is IValueProvider genericProvider)
                {
                    return new CastProvider<P>(genericProvider);
                }
                return new ConstantProvider<P>(provider);
            }
        }

        public sealed class FallbackReturnMethod<S, T> : BaseReturnMethod<S, T>
        {
            private MethodInfo _method;
            private IValueProvider[] _params;
            private object[] _values;

            public FallbackReturnMethod(MethodInfo method)
            {
                _method = method;
                _params = new IValueProvider[_method.GetParameters().Length];
                _values = new object[_params.Length];
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new FallbackReturnMethod<S, T>(_method);
            }

            public override T Invoke(in S source)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    _values[i] = _params[i]?.UnsafeValue;
                }
                return (T)_method.Invoke(_method.IsStatic ? null : (object)source, _values);
            }

            protected override void Build(object[] parameters)
            {
                var length = Mathf.Min(_params.Length, parameters.Length);
                for (int i = 0; i < length; i++)
                {
                    _params[i] = (parameters[i] as IValueProvider) ?? new ConstantProvider(parameters[i]);
                }
            }
        }

        public sealed class ReturnMethod<S, T> : BaseReturnMethod<S, T>
        {
            private ReturnDelegate<S, T> _getter;

            public override T Invoke(in S source) => _getter(source);

            private ReturnMethod() { }

            public ReturnMethod(MethodInfo method)
            {
                _getter = (ReturnDelegate<S, T>)method.CreateDelegate(typeof(ReturnDelegate<S, T>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnMethod<S, T>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
               
            }
        }

        public sealed class ReturnMethod<S, T, P1> : BaseReturnMethod<S, T>
        {
            private ReturnDelegate<S, T, P1> _getter;
            private IValueProvider<P1> _p1;

            public override T Invoke(in S source) => _getter(source, _p1.Value);

            private ReturnMethod() { }

            public ReturnMethod(MethodInfo method)
            {
                _getter = (ReturnDelegate<S, T, P1>)method.CreateDelegate(typeof(ReturnDelegate<S, T, P1>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnMethod<S, T, P1>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
            }
        }

        public sealed class ReturnMethod<S, T, P1, P2> : BaseReturnMethod<S, T>
        {
            private ReturnDelegate<S, T, P1, P2> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value);


            private ReturnMethod() { }

            public ReturnMethod(MethodInfo method)
            {
                _getter = (ReturnDelegate<S, T, P1, P2>)method.CreateDelegate(typeof(ReturnDelegate<S, T, P1, P2>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnMethod<S, T, P1, P2>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
            }
        }

        public sealed class ReturnMethod<S, T, P1, P2, P3> : BaseReturnMethod<S, T>
        {
            private ReturnDelegate<S, T, P1, P2, P3> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value, _p3.Value);


            private ReturnMethod() { }

            public ReturnMethod(MethodInfo method)
            {
                _getter = (ReturnDelegate<S, T, P1, P2, P3>)method.CreateDelegate(typeof(ReturnDelegate<S, T, P1, P2, P3>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnMethod<S, T, P1, P2, P3>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
            }
        }

        public sealed class ReturnMethod<S, T, P1, P2, P3, P4> : BaseReturnMethod<S, T>
        {
            private ReturnDelegate<S, T, P1, P2, P3, P4> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;
            private IValueProvider<P4> _p4;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value, _p3.Value, _p4.Value);


            private ReturnMethod() { }

            public ReturnMethod(MethodInfo method)
            {
                _getter = (ReturnDelegate<S, T, P1, P2, P3, P4>)method.CreateDelegate(typeof(ReturnDelegate<S, T, P1, P2, P3, P4>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnMethod<S, T, P1, P2, P3, P4>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
                _p4 = BuildParameter<P4>(providers[3]);
            }
        }

        public sealed class ReturnValueMethod<S, T> : BaseReturnMethod<S, T>
        {
            private ReturnValueDelegate<S, T> _getter;

            public override T Invoke(in S source) => _getter(source);

            private ReturnValueMethod() { }

            public ReturnValueMethod(MethodInfo method)
            {
                _getter = (ReturnValueDelegate<S, T>)method.CreateDelegate(typeof(ReturnValueDelegate<S, T>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnValueMethod<S, T>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {

            }
        }

        public sealed class ReturnValueMethod<S, T, P1> : BaseReturnMethod<S, T>
        {
            private ReturnValueDelegate<S, T, P1> _getter;
            private IValueProvider<P1> _p1;

            public override T Invoke(in S source) => _getter(source, _p1.Value);

            private ReturnValueMethod() { }

            public ReturnValueMethod(MethodInfo method)
            {
                _getter = (ReturnValueDelegate<S, T, P1>)method.CreateDelegate(typeof(ReturnValueDelegate<S, T, P1>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnValueMethod<S, T, P1>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
            }
        }

        public sealed class ReturnValueMethod<S, T, P1, P2> : BaseReturnMethod<S, T>
        {
            private ReturnValueDelegate<S, T, P1, P2> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value);


            private ReturnValueMethod() { }

            public ReturnValueMethod(MethodInfo method)
            {
                _getter = (ReturnValueDelegate<S, T, P1, P2>)method.CreateDelegate(typeof(ReturnValueDelegate<S, T, P1, P2>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnValueMethod<S, T, P1, P2>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
            }
        }

        public sealed class ReturnValueMethod<S, T, P1, P2, P3> : BaseReturnMethod<S, T>
        {
            private ReturnValueDelegate<S, T, P1, P2, P3> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value, _p3.Value);


            private ReturnValueMethod() { }

            public ReturnValueMethod(MethodInfo method)
            {
                _getter = (ReturnValueDelegate<S, T, P1, P2, P3>)method.CreateDelegate(typeof(ReturnValueDelegate<S, T, P1, P2, P3>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnValueMethod<S, T, P1, P2, P3>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
            }
        }

        public sealed class ReturnValueMethod<S, T, P1, P2, P3, P4> : BaseReturnMethod<S, T>
        {
            private ReturnValueDelegate<S, T, P1, P2, P3, P4> _getter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;
            private IValueProvider<P4> _p4;

            public override T Invoke(in S source) => _getter(source, _p1.Value, _p2.Value, _p3.Value, _p4.Value);

            

            private ReturnValueMethod() { }

            public ReturnValueMethod(MethodInfo method)
            {
                _getter = (ReturnValueDelegate<S, T, P1, P2, P3, P4>)method.CreateDelegate(typeof(ReturnValueDelegate<S, T, P1, P2, P3, P4>));
            }

            public override BaseReturnMethod<S, T> Duplicate()
            {
                return new ReturnValueMethod<S, T, P1, P2, P3, P4>() { _getter = _getter };
            }

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
                _p4 = BuildParameter<P4>(providers[3]);
            }
        }

        #endregion

        #region [  VOID METHODS  ]

        public abstract class BaseVoidMethod<S>
        {
            public bool AssignParameters(object[] providers)
            {
                try
                {
                    Build(providers);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }
            protected abstract void Build(object[] providers);
            public abstract void Invoke(in S source);
            public abstract BaseVoidMethod<S> Duplicate();

            protected static IValueProvider<P> BuildParameter<P>(object provider)
            {
                if (provider is IValueProvider<P> specialisedProvider)
                {
                    return specialisedProvider;
                }
                if (provider is IValueProvider genericProvider)
                {
                    return new CastProvider<P>(genericProvider);
                }
                return new ConstantProvider<P>(provider);
            }
        }

        public abstract class BaseSetterMethod<S, T> : BaseVoidMethod<S>
        {
            public new bool AssignParameters(object[] providers)
            {
                try
                {
                    var adjustedProviders = new object[providers.Length + 1];
                    Array.Copy(providers, adjustedProviders, providers.Length);
                    adjustedProviders[providers.Length] = new ConstantProvider<T>(default);
                    Build(adjustedProviders);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }

            public abstract void Invoke(in S source, T value);
        }

        public sealed class FallbackVoidMethod<S> : BaseVoidMethod<S>
        {
            private MethodInfo _method;
            private IValueProvider[] _params;
            private object[] _values;

            public FallbackVoidMethod(MethodInfo method)
            {
                _method = method;
                _params = new IValueProvider[_method.GetParameters().Length];
                _values = new object[_params.Length];
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new FallbackVoidMethod<S>(_method);
            }

            public override void Invoke(in S source)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    _values[i] = _params[i]?.UnsafeValue;
                }
                _method.Invoke(_method.IsStatic ? null : (object)source, _values);
            }

            protected override void Build(object[] parameters)
            {
                var length = Mathf.Min(_params.Length, parameters.Length);
                for (int i = 0; i < length; i++)
                {
                    _params[i] = (parameters[i] as IValueProvider) ?? new ConstantProvider(parameters[i]);
                }
            }
        }

        public sealed class FallbackVoidMethod<S, T> : BaseSetterMethod<S, T>
        {
            private MethodInfo _method;
            private IValueProvider[] _params;
            private object[] _values;

            public FallbackVoidMethod(MethodInfo method)
            {
                _method = method;
                _params = new IValueProvider[_method.GetParameters().Length];
                _values = new object[_params.Length];
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new FallbackVoidMethod<S, T>(_method);
            }

            public override void Invoke(in S source)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    _values[i] = _params[i]?.UnsafeValue;
                }
                _method.Invoke(_method.IsStatic ? null : (object)source, _values);
            }

            public override void Invoke(in S source, T value)
            {
                _values[_params.Length - 1] = value;
                for (int i = 0; i < _params.Length - 1; i++)
                {
                    _values[i] = _params[i]?.UnsafeValue;
                }
                _method.Invoke(_method.IsStatic ? null : (object)source, _values);
            }

            protected override void Build(object[] parameters)
            {
                var length = Mathf.Min(_params.Length, parameters.Length);
                for (int i = 0; i < length; i++)
                {
                    _params[i] = (parameters[i] as IValueProvider) ?? new ConstantProvider(parameters[i]);
                }
            }
        }

        public sealed class VoidMethod<S> : BaseVoidMethod<S>
        {
            private VoidDelegate<S> _setter;

            public override void Invoke(in S source) => _setter(source);

            private VoidMethod(){ }

            public VoidMethod(MethodInfo method)
            {
                _setter = (VoidDelegate<S>)method.CreateDelegate(typeof(VoidDelegate<S>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidMethod<S>() { _setter = _setter };
            }

            protected override void Build(object[] providers)
            {
                
            }
        }

        public sealed class VoidMethod<S, P1> : BaseSetterMethod<S, P1>
        {
            private VoidDelegate<S, P1> _setter;
            private IValueProvider<P1> _p1;

            private VoidMethod() { }

            public VoidMethod(MethodInfo method)
            {
                _setter = (VoidDelegate<S, P1>)method.CreateDelegate(typeof(VoidDelegate<S, P1>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidMethod<S, P1>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value);
            public override void Invoke(in S source, P1 value) => _setter(source, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]); ;
            }
        }

        public sealed class VoidMethod<S, P1, P2> : BaseSetterMethod<S, P2>
        {
            private VoidDelegate<S, P1, P2> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;

            private VoidMethod() { }

            public VoidMethod(MethodInfo method)
            {
                _setter = (VoidDelegate<S, P1, P2>)method.CreateDelegate(typeof(VoidDelegate<S, P1, P2>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidMethod<S, P1, P2>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value);
            public override void Invoke(in S source, P2 value) => _setter(source, _p1.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = providers[1] as IValueProvider<P2>;
            }
        }

        public sealed class VoidMethod<S, P1, P2, P3> : BaseSetterMethod<S, P3>
        {
            private VoidDelegate<S, P1, P2, P3> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;

            private VoidMethod() { }

            public VoidMethod(MethodInfo method)
            {
                _setter = (VoidDelegate<S, P1, P2, P3>)method.CreateDelegate(typeof(VoidDelegate<S, P1, P2, P3>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidMethod<S, P1, P2, P3>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value, _p3.Value);
            public override void Invoke(in S source, P3 value) => _setter(source, _p1.Value, _p2.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = providers[2] as IValueProvider<P3>;
            }
        }

        public sealed class VoidMethod<S, P1, P2, P3, P4> : BaseSetterMethod<S, P4>
        {
            private VoidDelegate<S, P1, P2, P3, P4> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;
            private IValueProvider<P4> _p4;

            private VoidMethod() { }

            public VoidMethod(MethodInfo method)
            {
                _setter = (VoidDelegate<S, P1, P2, P3, P4>)method.CreateDelegate(typeof(VoidDelegate<S, P1, P2, P3, P4>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidMethod<S, P1, P2, P3, P4>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value, _p3.Value, _p4.Value);
            public override void Invoke(in S source, P4 value) => _setter(source, _p1.Value, _p2.Value, _p3.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
                _p4 = providers[3] as IValueProvider<P4>;
            }
        }

        public sealed class VoidValueMethod<S> : BaseVoidMethod<S>
        {
            private VoidValueDelegate<S> _setter;

            public override void Invoke(in S source) => _setter(source);

            private VoidValueMethod() { }

            public VoidValueMethod(MethodInfo method)
            {
                _setter = (VoidValueDelegate<S>)method.CreateDelegate(typeof(VoidValueDelegate<S>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidValueMethod<S>() { _setter = _setter };
            }

            protected override void Build(object[] providers)
            {

            }
        }

        public sealed class VoidValueMethod<S, P1> : BaseSetterMethod<S, P1>
        {
            private VoidValueDelegate<S, P1> _setter;
            private IValueProvider<P1> _p1;

            private VoidValueMethod() { }

            public VoidValueMethod(MethodInfo method)
            {
                _setter = (VoidValueDelegate<S, P1>)method.CreateDelegate(typeof(VoidValueDelegate<S, P1>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidValueMethod<S, P1>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value);
            public override void Invoke(in S source, P1 value) => _setter(source, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
            }
        }

        public sealed class VoidValueMethod<S, P1, P2> : BaseSetterMethod<S, P2>
        {
            private VoidValueDelegate<S, P1, P2> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;

            private VoidValueMethod() { }

            public VoidValueMethod(MethodInfo method)
            {
                _setter = (VoidValueDelegate<S, P1, P2>)method.CreateDelegate(typeof(VoidValueDelegate<S, P1, P2>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidValueMethod<S, P1, P2>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value);
            public override void Invoke(in S source, P2 value) => _setter(source, _p1.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = providers[1] as IValueProvider<P2>;
            }
        }

        public sealed class VoidValueMethod<S, P1, P2, P3> : BaseSetterMethod<S, P3>
        {
            private VoidValueDelegate<S, P1, P2, P3> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;

            private VoidValueMethod() { }

            public VoidValueMethod(MethodInfo method)
            {
                _setter = (VoidValueDelegate<S, P1, P2, P3>)method.CreateDelegate(typeof(VoidValueDelegate<S, P1, P2, P3>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidValueMethod<S, P1, P2, P3>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value, _p3.Value);
            public override void Invoke(in S source, P3 value) => _setter(source, _p1.Value, _p2.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = providers[2] as IValueProvider<P3>;
            }
        }

        public sealed class VoidValueMethod<S, P1, P2, P3, P4> : BaseSetterMethod<S, P4>
        {
            private VoidValueDelegate<S, P1, P2, P3, P4> _setter;
            private IValueProvider<P1> _p1;
            private IValueProvider<P2> _p2;
            private IValueProvider<P3> _p3;
            private IValueProvider<P4> _p4;

            private VoidValueMethod() { }

            public VoidValueMethod(MethodInfo method)
            {
                _setter = (VoidValueDelegate<S, P1, P2, P3, P4>)method.CreateDelegate(typeof(VoidValueDelegate<S, P1, P2, P3, P4>));
            }

            public override BaseVoidMethod<S> Duplicate()
            {
                return new VoidValueMethod<S, P1, P2, P3, P4>() { _setter = _setter };
            }

            public override void Invoke(in S source) => _setter(source, _p1.Value, _p2.Value, _p3.Value, _p4.Value);
            public override void Invoke(in S source, P4 value) => _setter(source, _p1.Value, _p2.Value, _p3.Value, value);

            protected override void Build(object[] providers)
            {
                _p1 = BuildParameter<P1>(providers[0]);
                _p2 = BuildParameter<P2>(providers[1]);
                _p3 = BuildParameter<P3>(providers[2]);
                _p4 = providers[3] as IValueProvider<P4>;
            }
        }

        #endregion [  VOID METHODS  ]
    }
}
