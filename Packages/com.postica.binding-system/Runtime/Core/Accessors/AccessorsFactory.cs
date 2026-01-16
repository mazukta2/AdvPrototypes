using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Class which returns <see cref="IAccessor"/>s based on specified parameters 
    /// (mostly an object target and a path).<br/>
    /// It can return methods, fields and properties.
    /// <para/>
    /// <example>
    /// Here below are examples of paths:
    /// <para>
    /// -- <b>Methods</b> -- <i>[Experimental]</i><br/>
    /// Read-Only Method: <c>"localPosition.ToString()"</c><br/>
    /// Write-Only Method: <c>"localPosition.Scale(Vector3.one)"</c><br/>
    /// ReadWrite Method: <c>"localPosition.Scale(Vector3.one)|localPosition.ToString()"</c><br/>
    /// </para>
    /// <para>
    /// -- <b>Properties</b> --<br/>
    /// Standard Property: <c>"localPosition.normalized"</c><br/>
    /// Indexer Property: <c>"listOfTransforms.Item[index].localPosition"</c><br/>
    /// </para>
    /// <para>
    /// -- <b>Fields</b> --<br/>
    /// Field: <c>"localPosition.x"</c>
    /// </para>
    /// </example>
    /// </summary>
    public static class AccessorsFactory
    {
        internal delegate T GetterDelegate<S, T>(in S source);
        internal delegate void SetterDelegate<S, T>(in S source, T value);

        private static readonly Regex _indexerParamsRegex = new Regex(@"\[([\w\d, ]+)\]$", RegexOptions.Compiled);
        private static readonly Regex _methodParamsRegex = new Regex(@"\(([\w\d, ]*)\)$", RegexOptions.Compiled);
        private static readonly char[] _paramsSeparator = new char[] { ',' };
        private static Dictionary<FieldInfo, Func<object, object>> _fieldUnsafeGetter = new();
        private static Dictionary<FieldInfo, Action<object, object>> _fieldUnsafeSetter = new();

        private class KeyComparer : IEqualityComparer<(Type, string)>
        {
            public bool Equals((Type, string) x, (Type, string) y)
             => x.Item1 == y.Item1 && string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);

            public int GetHashCode((Type, string) obj) => obj.GetHashCode();
        }

        // transform.position.x
        // Func<Transform, Vector3> --> Func<Vector3, float>

        private static readonly Dictionary<(Type, string), IAccessor> _simpleAccessorsCache = new(new KeyComparer());
        private static readonly Dictionary<(Type, string), string> _simpleAccessorsNames = new(new KeyComparer());
        private static readonly Dictionary<(Type, string), IAccessor> _registeredAccessorsCache = new(new KeyComparer());
        private static readonly Dictionary<(Type, string), IAccessor> _accessorsCache = new(new KeyComparer());
        private static readonly Dictionary<string, IAccessorProvider> _accessorProviders = new StringDictionary<IAccessorProvider>();
        private static readonly char[] _splitChars = new[] { '.', '/' };

        /// <summary>
        /// Safe mode will wrap all critical calls with a try-catch block to allow a safer although slower execution 
        /// </summary>
        public static bool SafeMode { get; set; }

        internal static Action<string> LogError { get; set; }

        internal static void Reset(bool hardReset)
        {
            _simpleAccessorsCache.Clear();
            _accessorsCache.Clear();
            if (hardReset)
            {
                _registeredAccessorsCache.Clear();
            }
            else
            {
                foreach(var pair in _registeredAccessorsCache)
                {
                    _simpleAccessorsCache.Add(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>
        /// Creates a strongly typed getter function for the specified member path
        /// </summary>
        /// <typeparam name="T">The type to search the member path in</typeparam>
        /// <typeparam name="S">The type the member path points to</typeparam>
        /// <param name="memberName">The path of the member, separated by .</param>
        /// <returns>The <see cref="Func{T, TResult}"/> of the specified path, null otherwise</returns>
        /// <exception cref="ArgumentException">If the <paramref name="memberName"/> points to non-existent or not-available member</exception>
        public static Func<T, S> ValueGetter<T, S>(string memberName)
        {
            var accessor = GetAccessor<T, S>(memberName);
            if (accessor != null)
            {
                Func<T, S> getter = accessor.GetValue;
                return getter;
            }
            // Most probably it is a property
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var propertyInfo = typeof(T).GetProperty(memberName, flags);
            if(propertyInfo == null)
            {
                throw new ArgumentException($"Unable to find a getter for {typeof(T).FullName}.{memberName}", nameof(memberName));
            }
            return t => (S)propertyInfo.GetValue(t);
        }

        /// <summary>
        /// Registered pre-compiled <see cref="IAccessor"/>s. Used mostly to optimize certain members access.
        /// </summary>
        /// <param name="accessors">A triplet of <see cref="Type"/> of the source for the accessor, the path of the accessor and the accessor itself</param>
        /// <returns>A list of tuples with the outcome of the registration for each passed accessor</returns>
        public static IEnumerable<(string error, IAccessor accessor)> RegisterAccessors(params (Type type, string path, IAccessor accessor)[] accessors)
        {
            List<(string error, IAccessor accessor)> outcome = new List<(string error, IAccessor accessor)>();
            foreach(var (type, path, accessor) in accessors)
            {
                var id = (type, path);
                if (_simpleAccessorsCache.TryGetValue(id, out IAccessor existing))
                {
                    outcome.Add(($"Accessor with id {type.Name}.{path} already exists of type {existing.GetType().Name}", accessor));
                }
                _simpleAccessorsCache[id] = accessor;
                _registeredAccessorsCache[id] = accessor;
            }
            return outcome;
        }
        
        /// <summary>
        /// Whether the specified path is registered in the system or not
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsRegisteredAccessor(Type type, string path)
        {
            return _registeredAccessorsCache.ContainsKey((type, path));
        }

        /// <summary>
        /// Registers the <see cref="IAccessorProvider"/> in the system
        /// </summary>
        /// <param name="provider">The provider to be registered</param>
        /// <exception cref="ArgumentException">When there is already a provider with the same id as the specified one</exception>
        public static void RegisterAccessorProvider(IAccessorProvider provider)
        {
            if(_accessorProviders.TryGetValue(provider.Id, out var existing))
            {
                throw new ArgumentException($"Accessor Provider with id {provider.Id} already exists: {existing}");
            }
            _accessorProviders[provider.Id] = provider;
        }

        /// <summary>
        /// Unregisters the specified <see cref="IAccessorProvider"/>
        /// </summary>
        /// <param name="provider">The provider to unregister</param>
        /// <returns>True if the provider was previously registered, false otherwise</returns>
        public static bool UnregisterAccessorProvider(IAccessorProvider provider)
        {
            return _accessorProviders.Remove(provider.Id);
        }

        /// <summary>
        /// Gets the type a path points to.
        /// </summary>
        /// <param name="sourceType">The source for the path</param>
        /// <param name="path">The path</param>
        /// <param name="mainParamIndex">Optional, the parameter index if the path contains an indexer property or an array</param>
        /// <returns>The type the path points to</returns>
        public static Type GetTypeAtPath(Type sourceType, string path, int mainParamIndex = -1)
        {
            if (!_accessorsCache.TryGetValue((sourceType, path), out IAccessor accessor))
            {
                var accessors = GetAccessorsRecursive(sourceType, path, mainParamIndex, null);
                if (accessors.Count == 0)
                {
                    return sourceType;
                }
                return accessors[^1].ValueType;
            }
            return accessor?.ValueType;
        }

        /// <summary>
        /// Deconstructs the specified <see cref="IAccessor"/> into a list of types which corresponds to its internal accessors, if any
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="types">The list of all element types composing this accessor</param>
        public static void DeconstructAccessor(object accessor, ICollection<Type> types)
        {
            if(accessor == null)
            {
                return;
            }

            var accessorType = accessor.GetType();
            types.Add(accessorType);

            if (accessor is IWrapperAccessor wrapperAccessor)
            {
                foreach(var innerAccessor in wrapperAccessor.GetInnerAccessors())
                {
                    DeconstructAccessor(innerAccessor, types);
                }
                return;
            }
        }
        
        /// <summary>
        /// Deconstructs the specified <see cref="IAccessor"/> into a list of types which corresponds to its internal accessors' return types, if any
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="types">The list of all element return types composing this accessor</param>
        public static void GetAccessorReturnTypes(object accessor, ICollection<Type> types)
        {
            if(accessor is not IAccessor acc)
            {
                return;
            }

            types.Add(acc.ValueType);

            if (accessor is IWrapperAccessor wrapperAccessor)
            {
                foreach(var innerAccessor in wrapperAccessor.GetInnerAccessors())
                {
                    GetAccessorReturnTypes(innerAccessor, types);
                }
                return;
            }
        }

        /// <summary>
        /// Gets the member the path points to
        /// </summary>
        /// <param name="sourceType">The source for the path</param>
        /// <param name="path">The path</param>
        /// <returns>The <see cref="MemberInfo"/> the path points to</returns>
        public static MemberInfo GetMemberAtPath(Type sourceType, string path)
        {
            var pieces = path.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            return GetNextMember(sourceType, 0, pieces);
        }

        /// <summary>
        /// Prepares the specified converters for the specified accessor and target
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="readConverter"></param>
        /// <param name="writeConverter"></param>
        /// <param name="target"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static (IConverter readConverter, IConverter writeConverter) PrepareConverters(Type from, Type to,
            IConverter readConverter,
            IConverter writeConverter,
            object target,
            string path)
        {
            var type = target.GetType();
            if (readConverter is IConverterCompiler converterCompiler)
            {
                readConverter = converterCompiler.Compile(from, to);
            }
            if(writeConverter is IConverterCompiler writeConverterCompiler)
            {
                writeConverter = writeConverterCompiler.Compile(from, to);
            }
                
            if (readConverter is IContextConverter readContextConverter)
            {
                readContextConverter.SetContext(target, type, path);
            }
            if (readConverter != writeConverter && writeConverter is IContextConverter writeContextConverter)
            {
                writeContextConverter.SetContext(target, type, path);
            }

            if (readConverter is IPeerConverter readPeerConverter)
            {
                readPeerConverter.OtherConverter = writeConverter;
            }
            if (writeConverter is IPeerConverter writePeerConverter)
            {
                writePeerConverter.OtherConverter = readConverter;
            }
            
            return (readConverter, writeConverter);
        }

        /// <summary>
        /// Gets a generic <see cref="IAccessor"/> using the specified parameters
        /// </summary>
        /// <param name="target">The source for the accessor, can be any object which is compatible with the path</param>
        /// <param name="path">The path</param>
        /// <param name="parameters">Parameters values to use if the path has methods along the way</param>
        /// <param name="mainParamIndex">Optional, the index of the write parameter if the path contains methods</param>
        /// <returns>The accessor if found, or null otherwise</returns>
        public static IAccessor GetAccessor(object target, string path, object[] parameters = null, int mainParamIndex = -1)
        {
            
            var type = target.GetType();
            IAccessor accessor = FetchAccessor(type, path, mainParamIndex);

            if (parameters != null && accessor is IParametricAccessor parAccessor)
            {
                parAccessor.Parameters = parameters;
                parAccessor.MainParamIndex = mainParamIndex;
            }

            return accessor;
        }

        /// <summary>
        /// Gets a generic <see cref="IAccessor"/> using the specified parameters
        /// </summary>
        /// <param name="target">The source for the accessor, can be any object which is compatible with the path</param>
        /// <param name="path">The path</param>
        /// <param name="desiredOutputType">The output type the accessor should try to have</param>
        /// <param name="parameters">Parameters values to use if the path has methods along the way</param>
        /// <param name="mainParamIndex">Optional, the index of the write parameter if the path contains methods</param>
        /// <returns>The accessor if found, or null otherwise</returns>
        public static IAccessor GetAccessor(object target, string path, Type desiredOutputType, object[] parameters = null, int mainParamIndex = -1)
        {
            var type = target.GetType();
            IAccessor accessor = FetchAccessor(type, path, mainParamIndex);

            if (parameters != null && accessor is IParametricAccessor parAccessor)
            {
                parAccessor.Parameters = parameters;
                parAccessor.MainParamIndex = mainParamIndex;
            }
            
            if(desiredOutputType.IsAssignableFrom(accessor.ValueType) && desiredOutputType.IsClass && desiredOutputType != accessor.ValueType)
            {
                var inheritanceType = typeof(InheritanceAccessor<,,>).MakeGenericType(accessor.ObjectType, accessor.ValueType, desiredOutputType);
                var inheritanceAccessor = Activator.CreateInstance(inheritanceType, accessor);
                return inheritanceAccessor as IAccessor;
            }

            return accessor;
        }

        /// <summary>
        /// Get a partially specific <see cref="IAccessor{T}"/> using the specifed parameters
        /// </summary>
        /// <typeparam name="T">The type the path points to</typeparam>
        /// <param name="target">The source object which is compatible with the path</param>
        /// <param name="path">The path</param>
        /// <returns>The accessor if found, null otherwise</returns>
        public static IAccessor<T> GetAccessor<T>(object target, string path) => GetAccessor<T>(target, path, null, null, null, null);
        internal static IAccessor<T> GetAccessor<T>(object target,
            string path,
            IConverter readConverter,
            IConverter writeConverter,
            object[] parameters = null,
            int mainParamIndex = -1) => GetAccessor<T>(target, path, null, null, readConverter, writeConverter, parameters, mainParamIndex);
        
        internal static IAccessor<T> GetAccessor<T>(object target,
                                                    string path,
                                                    IBind requester,
                                                    IModifier[] preConversionModifiers,
                                                    IConverter readConverter,
                                                    IConverter writeConverter,
                                                    object[] parameters = null,
                                                    int mainParamIndex = -1)
        {
            var type = target.GetType();
            var accessor = FetchAccessor(type, path, mainParamIndex);

            if (parameters != null && accessor is IParametricAccessor parAccessor)
            {
                parAccessor.Parameters = parameters;
                parAccessor.MainParamIndex = mainParamIndex;
            }

            if (!typeof(T).IsAssignableFrom(accessor.ValueType))
            {
                if (preConversionModifiers?.Length > 0)
                {
                    var preConversionModifiersType = typeof(ModifiersAccessor<,>).MakeGenericType(accessor.ObjectType, accessor.ValueType);
                    accessor = Activator.CreateInstance(preConversionModifiersType, accessor, requester, preConversionModifiers) as IAccessor;
                }
                
                // We need a converter here
                (readConverter, writeConverter) = PrepareConverters(accessor.ValueType, typeof(T), readConverter, writeConverter, target, path);
                
                var converterType = typeof(ConverterAccessor<,,>).MakeGenericType(accessor.ObjectType, accessor.ValueType, typeof(T));
                var converterAccessor = Activator.CreateInstance(converterType, accessor, readConverter, writeConverter);
                accessor = converterAccessor as IAccessor;
            }
            else if(typeof(T).IsClass && typeof(T) != accessor.ValueType)
            {
                var inheritanceType = typeof(InheritanceAccessor<,,>).MakeGenericType(accessor.ObjectType, accessor.ValueType, typeof(T));
                var inheritanceAccessor = Activator.CreateInstance(inheritanceType, accessor);
                return (IAccessor<T>)inheritanceAccessor;
            }

            return (IAccessor<T>)accessor;
        }

        /// <summary>
        /// Gets a specific <see cref="IAccessor{S, T}"/> for the specified path
        /// </summary>
        /// <typeparam name="S">The type of the object compatible with the path</typeparam>
        /// <typeparam name="T">The type the path points to</typeparam>
        /// <param name="path">The path</param>
        /// <returns>The accessor if found, null otherwise</returns>
        public static IAccessor<S, T> GetAccessor<S, T>(string path) => GetAccessor<S, T>(path, null, null, null, null);
        internal static IAccessor<S, T> GetAccessor<S, T>(string path, IConverter readConverter, IConverter writeConverter, object[] parameters = null, int mainParamIndex = -1)
         => GetAccessor<S, T>(path, null, null, readConverter, writeConverter, parameters, mainParamIndex);
        internal static IAccessor<S, T> GetAccessor<S, T>(string path, IBind requester, IModifier[] preConversionModifiers, IConverter readConverter, IConverter writeConverter, object[] parameters = null, int mainParamIndex = -1)
        {
            var type = typeof(S);
            var accessor = FetchAccessor(type, path, mainParamIndex);

            if(parameters != null && accessor is IParametricAccessor parAccessor)
            {
                parAccessor.Parameters = parameters;
                parAccessor.MainParamIndex = mainParamIndex;
            }

            if (!typeof(T).IsAssignableFrom(accessor.ValueType))
            {
                if (preConversionModifiers?.Length > 0)
                {
                    var preConversionModifiersType = typeof(ModifiersAccessor<,>).MakeGenericType(accessor.ObjectType, accessor.ValueType);
                    accessor = Activator.CreateInstance(preConversionModifiersType, accessor, requester, preConversionModifiers) as IAccessor;
                }
                
                // We need a conveter here
                if (readConverter is IConverterCompiler converterCompiler)
                {
                    readConverter = converterCompiler.Compile(accessor.ValueType, typeof(T));
                }
                if(writeConverter is IConverterCompiler writeConverterCompiler)
                {
                    writeConverter = writeConverterCompiler.Compile(accessor.ValueType, typeof(T));
                }
                
                if (readConverter is IContextConverter readContextConverter)
                {
                    readContextConverter.SetContext(null, type, path);
                }
                if (readConverter != writeConverter && writeConverter is IContextConverter writeContextConverter)
                {
                    writeContextConverter.SetContext(null, type, path);
                }

                if (readConverter is IPeerConverter readPeerConverter)
                {
                    readPeerConverter.OtherConverter = writeConverter;
                }
                if (writeConverter is IPeerConverter writePeerConverter)
                {
                    writePeerConverter.OtherConverter = readConverter;
                }

                var converterType = typeof(ConverterAccessor<,,>).MakeGenericType(type, accessor.ValueType, typeof(T));
                var converterAccessor = Activator.CreateInstance(converterType, accessor, readConverter, writeConverter);
                accessor = converterAccessor as IAccessor;
            }
            else if (typeof(T).IsClass && typeof(T) != accessor.ValueType)
            {
                var inheritanceType = typeof(InheritanceAccessor<,,>).MakeGenericType(type, accessor.ValueType, typeof(T));
                var inheritanceAccessor = Activator.CreateInstance(inheritanceType, accessor);
                return (IAccessor<S, T>)inheritanceAccessor;
            }

            return (IAccessor<S, T>)accessor;
        }

        private static IAccessor FetchAccessor(Type type, string path, int mainParamIndex)
        {
            // TODO: Consider adding the Refactoring Mechanism
            
            path = path.Replace('/', '.');

            if (!_accessorsCache.TryGetValue((type, path), out IAccessor accessor))
            {
                var accessors = GetAccessorsRecursive(type, path, mainParamIndex, null);
                if(accessors.Count == 0)
                {
                    // Return the self reference type instead
                    var accessorType = typeof(SourceAccessor<>);
                    var selfAccessorType = accessorType.MakeGenericType(type);
                    accessor = Activator.CreateInstance(selfAccessorType, args: new object[] { nameof(AccessorsFactory) }) as IAccessor;

                    //return null;
                }
                else if (accessors.Count == 1)
                {
                    accessor = accessors[0];
                }
                else
                {
                    var accessorType = typeof(CompoundAccessor<,>).MakeGenericType(accessors[0].ObjectType, accessors.Last().ValueType);
                    accessor = Activator.CreateInstance(accessorType, accessors) as IAccessor;
                }

                _accessorsCache[(type, path)] = accessor;
                _accessorsCache[(accessor.ObjectType, path)] = accessor;
            }

            return accessor?.Duplicate();
        }
        
        private static List<IAccessor> GetAccessorsRecursive(Type type,
            string remainingPath,
            int mainParamIndex,
            List<IAccessor> list,
            List<string> namesList = null,
            bool useRuntimePath = false)
        {
            list ??= new List<IAccessor>();
            
            if (string.IsNullOrEmpty(remainingPath))
            {
                return list;
            }

            if (type == null)
            {
                return list;
            }
            
            var (path, rest) = SplitPath(remainingPath);
            if (useRuntimePath && type.TryMakeUnityRuntimePath(path, out var runtimePath, false))
            {
                // path = runtimePath;
                (path, rest) = SplitPath(runtimePath);
            }
            var id = (type, path);

            if (_simpleAccessorsCache.TryGetValue(id, out IAccessor accessor)
                && (accessor is not FieldAccessor || string.IsNullOrEmpty(rest)))
            {
                list.Add(accessor);
                
                if(namesList != null && _simpleAccessorsNames.TryGetValue(id, out string accessorName))
                {
                    namesList.Add(accessorName);
                }
                
                return GetAccessorsRecursive(accessor?.ValueType, rest, mainParamIndex, list, namesList, useRuntimePath);
            }
            
            PropertyInfo propertyInfo = null;
            Type memberType = null;

            var name = ExtractName(path);
            var normalizedName = path;
            var addNormalizedName = true;

            // Not cached, so need to create it
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Check for special accessors
            if (path.Length > 1 && path[0] == '[')
            {
                if (path.StartsWith(AccessorPath.ProviderPrefix, StringComparison.Ordinal)
                    && AccessorPath.TryGetProviderId(path, out var providerId, out var cleanPath))
                {
                    if (_accessorProviders.TryGetValue(providerId, out var provider))
                    {
                        accessor = provider.GetAccessor(type, cleanPath);
                    }
                    else if (LogError != null)
                    {
                        LogError($"A path is requested from provider {providerId} but the provider is not registered. " +
                                 $"Please consider registering the provider before requesting the path.");
                    }
                    else
                    {
                        throw new ArgumentException($"Provider {providerId} is not registered");
                    }
                }
                else if (path.StartsWith(AccessorPath.ArrayPrefix, StringComparison.Ordinal) && type.IsArray)
                {
                    // Most probably this is an array...
                    var arrayAccessorType = typeof(ArrayAccessor<,>).MakeGenericType(type, type.GetElementType());
                    accessor = (IAccessor)Activator.CreateInstance(arrayAccessorType, type);
                    memberType = type.GetElementType();
                }
                else
                {
                    if (type.IsArray)
                    {
                        var parameters = ExtractParameters(_indexerParamsRegex, path);
                        var arrayAccessorType = typeof(ArrayAccessor<,>).MakeGenericType(type, type.GetElementType());
                        accessor = (IAccessor)Activator.CreateInstance(arrayAccessorType, type, parameters);
                        memberType = type.GetElementType();
                    }
                    else if (typeof(ICollection).IsAssignableFrom(type))
                    {
                        TryGetIndexer(type, "Item" + path, "Item" + name, flags, out memberType, out accessor);
                    }
                }
            }
            else if (path.EndsWith(")", StringComparison.Ordinal))
            {
                // Here we have a method
                var methodParams = ExtractParameters(_methodParamsRegex, path);

                var methodName = ExtractName(path);
                var methods = type.GetMethods(flags);
                MethodInfo selectedMethod = null;
                foreach (var m in methods)
                {
                    if (m.Name == methodName && MatchParameters(m.GetParameters(), methodParams))
                    {
                        selectedMethod = m;
                        break;
                    }
                }

                if(selectedMethod == null)
                {
                    throw new ArgumentException($"Unable to find method: {type.Name}.{path}", path);
                }
                var valueType = selectedMethod.ReturnType != typeof(void) ? selectedMethod.ReturnType 
                    : mainParamIndex >= 0 && methodParams.Length > 0 
                        ? selectedMethod.GetParameters()[mainParamIndex].ParameterType 
                        : typeof(object);
                var accessorType = typeof(MethodAccessor<,>).MakeGenericType(type, valueType);
                accessor = Activator.CreateInstance(accessorType, selectedMethod) as IAccessor;
            }
            else if (path.EndsWith("]", StringComparison.Ordinal) 
                     && TryGetIndexer(type, name, ref path, ref rest, flags, out memberType, out accessor))
            {
                // Nothing to do here
            }
            else if (type.Name == typeof(Nullable<>).Name)
            {
                memberType = Nullable.GetUnderlyingType(type);
                var accessorType = typeof(NullableValueAccessor<>).MakeGenericType(memberType);
                accessor = Activator.CreateInstance(accessorType, path) as IAccessor;
            }
            else if ((propertyInfo = GetSmartPropertyInfo(type, name, flags)) != null)
            {
                // Most probably a property
                memberType = propertyInfo.PropertyType;
                normalizedName = propertyInfo.Name;
                if (type.IsValueType)
                {
                    // Here is the complex problem with the value types
                    var methodType = typeof(GetterDelegate<,>).MakeGenericType(type, propertyInfo.PropertyType);
                    var getter = propertyInfo.CanRead ? Delegate.CreateDelegate(methodType, propertyInfo.GetGetMethod(true)) : null;
                    Delegate setter = null;
                    if (propertyInfo.CanWrite)
                    {
                        var setterType = typeof(SetterDelegate<,>)
                            .MakeGenericType(type, propertyInfo.PropertyType);
                        var setMethod = propertyInfo.GetSetMethod(true);
                        setter = setMethod != null
                            ? Delegate.CreateDelegate(setterType, setMethod, false)
                            : null;
                    }

                    if (getter != null || setter != null)
                    {
                        var accessorType = typeof(PropertyValueTypeAccessor<,>).MakeGenericType(type, propertyInfo.PropertyType);
                        accessor = Activator.CreateInstance(accessorType, getter, setter) as IAccessor;
                    }
                }
                else
                {
                    var getter = propertyInfo.CanRead
                        ? Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(type, propertyInfo.PropertyType),
                            propertyInfo.GetGetMethod(true))
                        : null;
                    var setter = propertyInfo.CanWrite
                        ? Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(type, propertyInfo.PropertyType),
                            propertyInfo.GetSetMethod(true))
                        : null;

                    if (getter != null || setter != null)
                    {
                        var accessorType = typeof(PropertyObjectTypeAccessor<,>).MakeGenericType(type, propertyInfo.PropertyType);
                        accessor = Activator.CreateInstance(accessorType, getter, setter) as IAccessor;
                    }
                }
            }
            else if (TryGetFieldAccessor(type, ref path, ref rest, out memberType, out accessor))
            {
                namesList?.AddRange(path.Split('.', StringSplitOptions.RemoveEmptyEntries));
                addNormalizedName = false;
            }
            else if(!TryGetMaterialProperty(type, path, ref memberType, out accessor))
            {
                throw new ArgumentException($"Unable to find member: {type.Name}.{path}", path);
            }
            

            if (accessor == null) return list;

            if (addNormalizedName)
            {
                namesList?.Add(normalizedName);
            }

            _simpleAccessorsCache[(type, path)] = accessor;
            _simpleAccessorsNames[(type, path)] = normalizedName;
            // _simpleAccessorsCache[(memberType, path)] = accessor;
                
            list.Add(accessor);
            return GetAccessorsRecursive(memberType, rest, mainParamIndex, list, namesList, useRuntimePath);
        }

        private static bool TryGetMaterialProperty(Type type, string path, ref Type memberType, out IAccessor accessor)
        {
            if (!typeof(Material).IsAssignableFrom(type))
            {
                accessor = null;
                return false;
            }

            var split = path.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                accessor = null;
                return false;
            }
            
            var (propertyName, propertyTypename) = (split[0], split[1]);

            var propertyType = propertyTypename.Trim().ToLower() switch
            {
                "color" => typeof(Color),
                "vector2" => typeof(Vector2),
                "tiling" => typeof(Vector2),
                "offset" => typeof(Vector2),
                "vector3" => typeof(Vector3),
                "vector4" => typeof(Vector4),
                "vector" => typeof(Vector4),
                "range" => typeof(float),
                "float" => typeof(float),
                "int" => typeof(int),
                "texture" => typeof(Texture),
                _ => null
            };
            
            if (propertyType == null)
            {
                accessor = null;
                return false;
            }

            var materialAccessorType = typeof(MaterialPropertyAccessor<>).MakeGenericType(propertyType);
            accessor = Activator.CreateInstance(materialAccessorType, propertyName, propertyTypename) as IAccessor;
            
            memberType = propertyType;
            
            return accessor != null;
        }

        private static (string first, string rest) SplitPath(string path)
        {
            var index = path.IndexOfAny(_splitChars);
            return index < 0 ? (path, "") : (path[..index], path[(index + 1)..]);
        }
        
        private static (string first, string rest) SplitPathAtEnd(string path)
        {
            var index = path.LastIndexOfAny(_splitChars);
            return index < 0 ? ("", path) : (path[..index], path[(index + 1)..]);
        }
        
        private static string ReducePath(string path)
        {
            var index = path.LastIndexOfAny(_splitChars);
            return index < 0 ? null : path[..index];
        }
        
        private static bool TryGetFieldAccessor(Type type, ref string path, ref string rest,
            out Type memberType,
            out IAccessor accessor)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var (name, restOfName) = ExtractNameExtended(path);
            var lastField = type.GetField(name, flags);
            if (lastField == null)
            {
                memberType = null;
                accessor = null;
                return false;
            }

            var field = lastField;
            if (!path.EndsWith(']'))
            {
                while (field.FieldType.IsValueType)
                {
                    var (tempPath, tempRest) = SplitPath(rest);
                    var (tempName, tempRestOfName) = ExtractNameExtended(tempPath);
                    field = field.FieldType.GetField(tempName, flags);
                    if (field == null)
                    {
                        break;
                    }
                    
                    path += "." + tempName;
                    lastField = field;
                    
                    if (tempRestOfName.Length > 0)
                    {
                        rest = tempRestOfName + '.' + tempRest;
                        break;
                    }

                    rest = tempRest;
                }
            }
            else
            {
                rest = restOfName + '.' + rest;
            }
            

            memberType = lastField.FieldType;
            var accessorType = (type.IsValueType, lastField.FieldType.IsValueType) switch
            {
                (true, true) => typeof(StructFieldValueAccessor<,>).MakeGenericType(type, lastField.FieldType),
                (true, false) => typeof(StructFieldRefAccessor<,>).MakeGenericType(type, lastField.FieldType),
                (false, true) => typeof(ClassFieldValueAccessor<,>).MakeGenericType(type, lastField.FieldType),
                (false, false) => typeof(ClassFieldRefAccessor<,>).MakeGenericType(type, lastField.FieldType),
            };
            
            var fieldNames = path.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            
            accessor = Activator.CreateInstance(accessorType, fieldNames) as IAccessor;
            
            return true;
        }

        private static bool TryGetIndexer(Type type, string path, string name,
            BindingFlags flags,
            out Type memberType,
            out IAccessor accessor)
        {
            PropertyInfo propertyInfo = null;
            var parameters = ExtractParameters(_indexerParamsRegex, path);
            
            var properties = type.GetProperties(flags);
            foreach (var property in properties)
            {
                if (property.Name.FastEquals(name) && MatchParameters(property.GetIndexParameters(), parameters))
                {
                    propertyInfo = property;
                    break;
                }
            }

            if (propertyInfo == null)
            {
                if (typeof(IList).IsAssignableFrom(type) &&
                    TryConvertListParameter(parameters[0], type, out int convertedParam))
                {
                    propertyInfo = type.GetProperty("Item", flags);
                    var accessorListType =
                        typeof(IndexerAccessor<,>).MakeGenericType(type, type.GetGenericArguments()[0]);
                    accessor = Activator.CreateInstance(accessorListType, propertyInfo.GetGetMethod(),
                        propertyInfo.GetSetMethod(), new object[] { convertedParam }) as IAccessor;
                    memberType = propertyInfo.PropertyType;
                    return true;
                }

                memberType = null;
                accessor = null;
                return false;
                // throw new ArgumentException($"Unable to find indexer: {type.Name}.{path}", path);
            }

            memberType = propertyInfo.PropertyType;
            var accessorType = typeof(IndexerAccessor<,>).MakeGenericType(type, propertyInfo.PropertyType);
            accessor = Activator.CreateInstance(accessorType, propertyInfo.GetGetMethod(), propertyInfo.GetSetMethod()) as IAccessor;
            return true;
        }

        private static bool TryConvertListParameter(string parameter, Type type, out int paramValue)
        {
            var listType = type;
            while (listType != null && !listType.IsGenericType)
            {
                listType = listType.BaseType;
            }
            
            if (listType == null)
            {
                paramValue = 0;
                return false;
            }
            
            if (int.TryParse(parameter, out paramValue))
            {
                return true;
            }
            
            return false;
        }

        private static bool TryGetIndexer(Type type, string name, ref string path, ref string rest,
            BindingFlags flags,
            out Type memberType,
            out IAccessor accessor)
        {
            if (TryGetIndexer(type, path, name, flags, out memberType, out accessor)) return true;

            if (string.IsNullOrEmpty(rest))
            {
                rest = path[name.Length..];
            }
            else
            {
                rest = path[name.Length..] + '.' + rest;
            }

            path = name;

            return false;

        }

        private static PropertyInfo GetSmartPropertyInfo(Type type, string name, BindingFlags flags)
        {
            var propertyInfo = type.GetProperty(name, flags);
            if (propertyInfo != null)
            {
                return propertyInfo;
            }
            
            var fieldInfo = type.GetField(name, flags);
            if (fieldInfo != null)
            {
                return null;
            }
            
            // Remove prefixes such as 'm_' or '_'
            if (name.Length > 2 && name[0] == 'm' && name[1] == '_')
            {
                name = name.Substring(2);
            }
            else if (name.Length > 1 && name[0] == '_')
            {
                name = name.Substring(1);
            }
            
            return type.GetProperty(name, flags) 
                ?? type.GetProperty(InvertFirstLetter(name), flags);
        }

        private static string InvertFirstLetter(string name)
        {
            if (name.Length == 0) { return name; }
            return char.IsUpper(name[0]) ? char.ToLower(name[0]) + name[1..] : char.ToUpper(name[0]) + name[1..];
        }

        private static MemberInfo GetNextMember(MemberInfo previous, int index, string[] pieces)
        {
            if (pieces.Length <= index) { return previous; }

            var path = pieces[index];
            var type = previous.GetMemberType();

            FieldInfo fieldInfo;
            PropertyInfo propertyInfo = null;

            string name = ExtractName(path);

            // Not cached, so need to create it
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Check for special accessors
            if (path.Length > 0 && path[0] == '[')
            {
                if (path.StartsWith(AccessorPath.ProviderPrefix, StringComparison.Ordinal)
                        && AccessorPath.TryGetProviderId(path, out var providerId, out var cleanPath))
                {
                    if (_accessorProviders.TryGetValue(providerId, out var provider))
                    {
                        var accessor = provider.GetAccessor(type, cleanPath);
                        if (accessor != null)
                        {
                            return GetNextMember(accessor.ValueType, index + 1, pieces);
                        }
                    }
                    else if (LogError != null)
                    {
                        LogError($"A path is requested from provider {providerId} but the provider is not registered. " +
                            $"Please consider registering the provider before requesting the path.");
                        return previous;
                    }
                    else
                    {
                        throw new ArgumentException($"Provider {providerId} is not registered");
                    }
                }
                else if (type.IsArray)
                {
                    // Most probably this is an array...
                    if(pieces.Length <= index + 1) { return previous; }

                    return GetNextMember(type.GetElementType(), index + 1, pieces);
                }
            }
            else if (path.EndsWith(")", StringComparison.Ordinal))
            {
                // Here we have a method
                var methodParams = ExtractParameters(_methodParamsRegex, path);

                var methodName = ExtractName(path);
                var methods = type.GetMethods(flags);
                MethodInfo selectedMethod = null;
                foreach (var m in methods)
                {
                    if (m.Name == methodName && MatchParameters(m.GetParameters(), methodParams))
                    {
                        selectedMethod = m;
                        break;
                    }
                }

                if (selectedMethod == null)
                {
                    throw new ArgumentException($"Unable to find member: {type.Name}.{path}", path);
                }

                return GetNextMember(selectedMethod, index + 1, pieces);
            }
            else if (path.EndsWith("]", StringComparison.Ordinal))
            {
                // Here we have indexer properties
                var parameters = ExtractParameters(_indexerParamsRegex, path);
                var properties = type.GetProperties(flags);
                foreach (var property in properties)
                {
                    if (property.Name == name && MatchParameters(property.GetIndexParameters(), parameters))
                    {
                        propertyInfo = property;
                        break;
                    }
                }

                if (propertyInfo == null)
                {
                    throw new ArgumentException($"Unable to find member: {type.Name}.{path}", path);
                }

                return GetNextMember(propertyInfo, index + 1, pieces);
            }
            else if ((fieldInfo = type.GetField(name, flags)) != null)
            {
                return GetNextMember(fieldInfo, index + 1, pieces);
            }
            else if ((propertyInfo = GetSmartPropertyInfo(type, name, flags)) != null)
            {
                return GetNextMember(propertyInfo, index + 1, pieces);
            }
            else
            {
                throw new ArgumentException($"Unable to find member: {type.Name}.{path}", path);
            }
            return previous;
        }

        private static string ExtractName(string path)
        {
            return path.EndsWith(")", StringComparison.Ordinal)
                    ? path.Substring(0, path.LastIndexOf('('))
                    : path.EndsWith("]", StringComparison.Ordinal)
                    ? path.Substring(0, path.LastIndexOf('['))
                    : path;
        }
        
        private static (string name, string rest) ExtractNameExtended(string path)
        {
            return path.EndsWith(")", StringComparison.Ordinal)
                ? (path[..path.LastIndexOf('(')], path[path.LastIndexOf('(')..])
                : path.EndsWith("]", StringComparison.Ordinal)
                    ? (path[..path.LastIndexOf('[')], path[path.LastIndexOf('[')..])
                    : (path, "");
        }

        private static string[] ExtractParameters(Regex regex, string path)
        {
            var match = regex.Match(path);
            if (!match.Success) { return Array.Empty<string>(); }

            var parameters = regex.Match(path).Groups[1].Value.Split(_paramsSeparator);
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = parameters[i].Trim();
            }
            return parameters;
        }

        private static bool MatchParameters(ParameterInfo[] parInfoes, string[] parameters)
        {
            if(parInfoes?.Length != parameters.Length)
            {
                return false;
            }
            for (int i = 0; i < parInfoes.Length; i++)
            {
                if(!parInfoes[i].ParameterType.Name.FastEquals(parameters[i])
                    && !parInfoes[i].ParameterType.UserFriendlyName().FastEquals(parameters[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the normalized path for the specified context and path. A <b>normalized path</b> is a runtime ready path.
        /// <remarks>Some paths may be different during Edit Time and not be available for runtime.
        /// With this method an attempt is made to render the path runtime ready</remarks>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        /// <returns>Normalized path</returns>
        public static string NormalizePath(Object context, string path)
        {
            if (context == null)
            {
                return path;
            }

            var names = new List<string>();
            var type = context.GetType();
            GetAccessorsRecursive(type, path, -1, null, names, useRuntimePath: true);

            var sb = new StringBuilder();
            foreach (var name in names)
            {
                sb.Append(name).Append('.');
            }
            
            return sb.ToString(0, sb.Length - 1);
        }
    }
}
