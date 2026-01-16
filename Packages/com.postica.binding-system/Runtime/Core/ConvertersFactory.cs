using Postica.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Converts one value into another.
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// The unique id of this converter. It is also used as a displayed name
        /// </summary>
        string Id { get; }
        /// <summary>
        /// A short description about what this object converts. This value will show as a tooltip where needed..
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Converts the value into another value
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The converted value</returns>
        object Convert(object value);
        /// <summary>
        /// If true, the conversion will always succeed, regardles of the data. <br/>
        /// If false, however, the conversion may fail and it may as well generate an error or an exception.
        /// </summary>
        bool IsSafe { get; }
    }

    /// <summary>
    /// A strongly typed version of <see cref="IConverter"/>. <br/>
    /// It generally should be faster and potentially make zero memory allocations
    /// </summary>
    /// <typeparam name="T">The type of the value to be converted</typeparam>
    /// <typeparam name="TResult">The type of the converted value</typeparam>
    public interface IConverter<in T, out TResult> : IConverter
    {
        /// <summary>
        /// Converts the value of type <typeparamref name="T"/> into a value of type <typeparamref name="TResult"/>
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The converted value</returns>
        TResult Convert(T value);

        /// <inheritdoc/>
        object IConverter.Convert(object value)
        {
            try
            {
                return Convert((T)value);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"{nameof(ConvertersFactory)}: Cannot convert {value?.GetType().FullName} to {typeof(T).FullName}");
            }
        }
    }
    
    /// <summary>
    /// A compiler for converters. <br/>
    /// </summary>
    public interface IConverterCompiler
    {
        /// <summary>
        /// Compiles a converter for the specified types. <br/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        IConverter Compile(Type from, Type to);
    }

    /// <summary>
    /// A converter which can set a context for the conversion. <br/>
    /// This is used to set a context for the conversion, which is used to store additional data for the conversion.
    /// </summary>
    public interface IContextConverter
    {
        void SetContext(object context, Type contextType, string path);
    }
    
    /// <summary>
    /// This converter can short-circuit the conversion process. <br/>
    /// <b><i>Note: It will skip the whole modifiers chain, so use it wisely.</i></b>
    /// </summary>
    public interface IShortCircuitConverter
    {
        bool HasShortCircuited { get; }
        void SetTarget(object target, Type targetType, string path);
    }

    /// <summary>
    /// Means this converter can convert to a type to a type which is not assignable from the first one. <br/>
    /// </summary>
    public interface IContravariantConverter : IConverter
    {
        Type ActualTargetType { get; set; }
    }

    /// <summary>
    /// A converter which has a peer converter. <br/>
    /// The peer converter is another converter which is used as a reference for this
    /// </summary>
    public interface IPeerConverter
    {
        IConverter OtherConverter { get; set; }
    }
    
    /// <summary>
    /// A converter which can provide delegates for conversion and reverse conversion. <br/>
    /// </summary>
    public interface IDelegateConverter
    {
        (Delegate conversion, Delegate reverseConversion) GetConversions();
    }


    /// <summary>
    /// A convertes template which is used as a reference when creating new converters. <br/>
    /// The templates are used internally as a lazy factory for converters.
    /// </summary>
    public interface IConverterTemplate
    {
        /// <summary>
        /// The id of the template, should be unique. The created converters will use this id as a reference.
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Whether the converters are safe or not. Please refer to <see cref="IConverter.IsSafe"/>.
        /// </summary>
        bool IsSafe { get; }
        /// <summary>
        /// The type of the value to be converted.
        /// </summary>
        Type FromType { get; }
        /// <summary>
        /// The type of the converted value.
        /// </summary>
        Type ToType { get; }
        /// <summary>
        /// The factory method to create a converter out of this template.
        /// </summary>
        /// <returns>The <see cref="IConverter"/> based on this template</returns>
        IConverter Create();
    }

    /// <summary>
    /// This class is responsable of creating explicitly and implicitly converters, 
    /// both for user needs as well as for other internal binding systems.
    /// </summary>
    public static class ConvertersFactory
    {
        internal delegate object CreateInstanceDelegate(Type type, params object[] args);

        /// <summary>
        /// Used for internal purposes only. Please ignore.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="S"></typeparam>
        [Preserve]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class __ConverterDef<T, S>
        {
            private Converter<T, S> __converter;
            private CastConverter<T, object, object, S> __converter1;
        }

        /// <summary>
        /// Used for internal purposes only. Please ignore.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TF"></typeparam>
        /// <typeparam name="SF"></typeparam>
        /// <typeparam name="S"></typeparam>
        [Preserve]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class __CastConverterDef<T, TF, SF, S>
        {
            private CastConverter<T, TF, SF, S> __converter;
        }

        private class ConverterTemplate : IConverterTemplate
        {
            public string Id { get; }
            public bool IsSafe { get; }
            public bool HasContravariantConversion { get; private set; }
            public Type ConverterType { get; }
            public Type FromType { get; private set; }
            public Type ToType { get; private set; }

            public ConverterTemplate(string id, bool isSafe, Type converterType)
            {
                Id = id ?? StringUtility.NicifyName(converterType.Name);
                IsSafe = isSafe;
                ConverterType = converterType;
            }

            public IConverter Create() => CreateInstance(ConverterType) as IConverter;

            public override bool Equals(object obj) => obj is ConverterTemplate template && template.Id == Id;
            public override int GetHashCode() => Id.GetHashCode();

            public static IReadOnlyList<ConverterTemplate> CreateTemplates(string id, bool isSafe, Type converterType)
            {
                List<ConverterTemplate> templates = new List<ConverterTemplate>();

                var interfaces = converterType.GetInterfaces();
                var hasContravariant = interfaces.Any(i => i == typeof(IContravariantConverter));
                foreach (var iface in interfaces)
                {
                    if (!iface.IsGenericType || typeof(IConverter<,>) != iface.GetGenericTypeDefinition())
                    {
                        continue;
                    }
                    var genericArgs = iface.GetGenericArguments();
                    if (genericArgs.Length == 2)
                    {
                        templates.Add(new ConverterTemplate(id, isSafe, converterType)
                        {
                            FromType = genericArgs[0],
                            ToType = genericArgs[1],
                            HasContravariantConversion = genericArgs[0] != genericArgs[1] 
                                                         && genericArgs[1].IsClass 
                                                         && hasContravariant
                        });
                    }
                }

                return templates;
            }
        }
        
        private sealed class DelegateConverter<Tfrom, Tto> : IDelegateConverter
        {
            private readonly Func<Tfrom, Tto> _conversion;
            private readonly Func<Tto, Tfrom> _reverseConversion;

            private static Dictionary<(Type fromType, Type toType), Func<Tfrom, Tto>> _readValueProviderConversions;
            private static Dictionary<(Type fromType, Type toType), Func<Tto, Tfrom>> _writeValueProviderConversions;
            
            public (Delegate conversion, Delegate reverseConversion) GetConversions()
            {
                return (_conversion, _reverseConversion);
            }

            public DelegateConverter(IConverter readConverter, IConverter writeConverter)
            {
                if (readConverter is IConverter<Tfrom, Tto> ctConverter)
                {
                    _conversion = ctConverter.Convert;
                }
                else if (readConverter is IConverter<object, Tto> genericConverter)
                {
                    _conversion = v => genericConverter.Convert(v);
                }
                else if (!TryGetValueProviderConversion(out _conversion))
                {
                    _conversion = GetConversion<Tfrom, Tto>();
                }

                if (writeConverter is IConverter<Tto, Tfrom> tcConverter)
                {
                    _reverseConversion = tcConverter.Convert;
                }
                else if (!TryGetValueProviderConversion(out _reverseConversion))
                {
                    _reverseConversion = GetConversion<Tto, Tfrom>();
                }
                //if(_conversion == null)
                //{
                //    throw new InvalidOperationException($"{nameof(DelegateFactory)}: Unable to get conversions from {typeof(C).FullName} to {typeof(T).FullName}");
                //}
            }

            private static bool TryGetValueProviderConversion(out Func<Tfrom, Tto> conversion)
            {
                if (typeof(IValueProvider<Tto>).IsAssignableFrom(typeof(Tfrom)))
                {
                    _readValueProviderConversions ??= new();
                    if(_readValueProviderConversions.TryGetValue((typeof(Tfrom), typeof(Tto)), out conversion))
                    {
                        return true;
                    }

                    conversion = v => ((IValueProvider<Tto>)v).Value;
                    _readValueProviderConversions[(typeof(Tfrom), typeof(Tto))] = conversion;
                    return conversion != null;
                }

                conversion = null;
                return false;
            }

            private static bool TryGetValueProviderConversion(out Func<Tto, Tfrom> conversion)
            {
                if (typeof(IValueProvider<Tfrom>).IsAssignableFrom(typeof(Tto)))
                {
                    _writeValueProviderConversions ??= new();
                    if(_writeValueProviderConversions.TryGetValue((typeof(Tto), typeof(Tfrom)), out conversion))
                    {
                        return true;
                    }

                    conversion = v => ((IValueProvider<Tfrom>)v).Value;
                    _writeValueProviderConversions[(typeof(Tto), typeof(Tfrom))] = conversion;
                    return conversion != null;
                }

                conversion = null;
                return false;
            }
        }

        private static readonly Dictionary<(Type from, Type to), (Delegate del, bool isTransitive, bool isSafe)> _conversions
            = new();
        private static readonly Dictionary<Type, Dictionary<Type, IConverter>> _convertersByType = new();
        private static readonly Dictionary<string, IConverter> _convertersById = new();
        private static readonly HashSet<Type> _processedTypes = new();
        private static readonly Dictionary<Type, IConverter> _emptyDictionary = new();
        private static readonly StringBuilder _sb = new();

        private static readonly Dictionary<(Type from, Type to), List<ConverterTemplate>> _templates = new();
        private static readonly Dictionary<(Type from, Type to), List<ConverterTemplate>> _contravariantTemplates = new();
        private static readonly Dictionary<(Type from, Type to), (bool canConvert, bool isSafe)> _canConvertCache = new();
        private static readonly Dictionary<Type, List<ConverterTemplate>> _templatesByType = new();
        private static readonly HashSet<string> _templatesIds = new();

        private static readonly List<IConverterTemplate> _tempTemplatesList = new(16);

        internal static CreateInstanceDelegate CreateInstanceOverride { get; set; }

        private static object CreateInstance(Type type, params object[] args)
            => CreateInstanceOverride?.Invoke(type, args) ?? Activator.CreateInstance(type, args);

        [Preserve]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class CastConverter<T, TF, SF, S> : IConverter<T, S>
        {
            private readonly Func<TF, SF> _converter;
            private readonly string _id;
            private readonly bool _isSafe;
            private string _description;

            public string Id => _id;

            public string Description
            {
                get
                {
                    if (string.IsNullOrEmpty(_description))
                    {
                        _description = $"Basic conversion from {typeof(T).GetAliasName()} to {typeof(S).GetAliasName()}";
                    }
                    return _description;
                }
            }

            public bool IsSafe => _isSafe;

            public bool CanConvert(Type type) => typeof(T).IsAssignableFrom(type);

            public S Convert(T value) => value is TF tf ? _converter(tf) is S s ? s : default : default;

            public object Convert(object value) => value is TF tvalue ? _converter(tvalue)
            : throw new InvalidCastException($"{nameof(ConvertersFactory)}: Cannot convert {value?.GetType().FullName} to {typeof(T).FullName}");

            [Preserve]
            public CastConverter(string id, Func<TF, SF> conversion, bool isSafe)
            {
                _converter = conversion;
                _id = id;
                _isSafe = isSafe;
            }
        }

        [Preserve]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Converter<T, S> : IConverter<T, S>
        {
            private readonly Func<T, S> _converter;
            private readonly string _id;
            private readonly bool _isSafe;
            private string _description;

            public string Id => _id;

            public string Description
            {
                get
                {
                    if (string.IsNullOrEmpty(_description))
                    {
                        _description = $"Basic conversion from {typeof(T).GetAliasName()} to {typeof(S).GetAliasName()}";
                    }
                    return _description;
                }
            }

            public bool IsSafe => _isSafe;

            public bool CanConvert(Type type) => typeof(T).IsAssignableFrom(type);

            public S Convert(T value) => _converter(value);

            public object Convert(object value) => value is T tvalue ? _converter(tvalue)
            : throw new InvalidCastException($"{nameof(ConvertersFactory)}: Cannot convert {value?.GetType().FullName} to {typeof(T).FullName}");

            [Preserve]
            public Converter(string id, Func<T, S> conversion, bool isSafe)
            {
                _converter = conversion;
                _id = id;
                _isSafe = isSafe;
            }
        }

        internal static bool HasConversion(Type from, Type to, out bool isSafe)
        {
            // It is too slow, need to cache it somehow
            if (_canConvertCache.TryGetValue((from, to), out var result))
            {
                isSafe = result.isSafe;
                return result.canConvert;
            }
            if (TryGetConversionData(from, to, out var data))
            {
                isSafe = data.isSafe;
                _canConvertCache[(from, to)] = (data.conversion != null, data.isSafe);
                return data.conversion != null;
            }
            if (HasTemplatesFor(from, to))
            {
                _canConvertCache[(from, to)] = (true, true);
                isSafe = true;
                return true;
            }

            _canConvertCache[(from, to)] = (false, false);
            isSafe = false;
            return false;
        }

        internal static Delegate GetConversionDelegate(Type from, Type to)
        {
            if (TryGetConversionData(from, to, out var data))
            {
                return data.conversion;
            }
            return null;
        }

        private static bool TryGetConversionData(Type from, Type to, out (Delegate conversion, bool isTransitive, bool isSafe) data)
        {
            if (_conversions.TryGetValue((from, to), out data))
            {
                return data.conversion != null;
            }

            foreach (var pair in _conversions)
            {
                if (pair.Value.del != null && pair.Key.from.IsAssignableFrom(from) && to.IsAssignableFrom(pair.Key.to))
                {
                    data = pair.Value;
                    _conversions[(from, to)] = data;
                    return true;
                }
            }

            // Search for implicit and explicit operators
            if (TryGetOperatorConversion(from, to, out var conversionFunc, out var _))
            {
                data = (conversionFunc, false, true);
                _conversions[(from, to)] = data;
                return data.conversion != null;
            }

            if (from.IsEnum)
            {
                return TryGetConversionData(typeof(Enum), to, out data);
            }

            // Create transitive conversions
            // First check if it is even possible
            //foreach (var pair in _conversions)
            //{
            //    if (pair.Key.from.IsAssignableFrom(from))
            //    {
            //        foreach(var nextPair in )
            //        _conversions[(from, to)] = pair.Value;
            //        return pair.Value;
            //    }
            //}

            data = (null, false, false);
            _conversions[(from, to)] = data;
            return false;
        }

        private static Func<TFrom, TTo> TryGetCorrectConversion<TFrom, TTo>(Type from, Type to)
        {
            if (!TryGetConversionData(from, to, out var data))
            {
                return default;
            }

            if(data.conversion is Func<TFrom, TTo> preciseConversion)
            {
                return preciseConversion;
            }
            if(data.conversion.GetType().GetGenericTypeDefinition() == typeof(Func<,>))
            {
                var lambda = data.conversion;
                var lambdaArgs = lambda.GetType().GetGenericArguments();
                var castConverterType = typeof(CastConverter<,,,>).MakeGenericType(from, lambdaArgs[0], lambdaArgs[1], to);
                var castConverter = CreateInstance(castConverterType, $"CastConverter_{from.Name}_{to.Name}", lambda, data.isSafe);
                Func<TFrom, TTo> castedConversion = castConverter is IConverter<TFrom, TTo> convFromTo ? convFromTo.Convert : null;
                _conversions[(from, to)] = (castedConversion, data.isTransitive, data.isSafe);

                return castedConversion;
            }

            return default;
        }

        /// <summary>
        /// Gets a conversion functor to convert from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>. <br/>
        /// Returns null on failure.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <returns>The conversion functor or null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<TFrom, TTo> GetConversion<TFrom, TTo>()
         => TryGetCorrectConversion<TFrom, TTo>(typeof(TFrom), typeof(TTo)) ?? GetFallbackConversion<TFrom, TTo>();

        private static Func<TFrom, TTo> GetFallbackConversion<TFrom, TTo>() => v =>
        {
            if (v is IValueProvider<TTo> valueProvider)
            {
                return valueProvider.Value;
            }
            return (TTo)Convert.ChangeType(v, typeof(TTo));
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyDictionary<Type, IConverter> GetConvertersFor<T>() => GetConvertersFor(typeof(T));

        /// <summary>
        /// Gets all available converters which can convert to <paramref name="type"/>
        /// </summary>
        /// <param name="type">The type to convert to</param>
        /// <returns>A dictionary containing types to convert from as keys and converters as values</returns>
        public static IReadOnlyDictionary<Type, IConverter> GetConvertersFor(Type type)
        {
            return _convertersByType.GetValueOrDefault(type, _emptyDictionary);
        }

        /// <summary>
        /// Gets the converter with specified id.
        /// </summary>
        /// <param name="id">The id of the converter</param>
        /// <param name="converter">The converter</param>
        /// <returns>True if the conveter was found, false otherwise</returns>
        public static bool TryGetConverter(string id, out IConverter converter) => _convertersById.TryGetValue(id, out converter);
        
        /// <summary>
        /// Tries to get a converter which can convert from <paramref name="from"/> type to <paramref name="to"/> type.
        /// </summary>
        /// <param name="from">The type to convert from</param>
        /// <param name="to">The type to convert to</param>
        /// <param name="converter">The requested converter</param>
        /// <returns>True if the converter was found, false otherwise</returns>
        public static bool TryGetConverter(Type from, Type to, out IConverter converter)
        {
            if (_convertersByType.TryGetValue(to, out var converters) && converters.TryGetValue(from, out converter))
            {
                return true;
            }

            if (TryGetOperatorConversion(from, to, out var _, out converter))
            {
                return true;
            }
            
            // if (from.IsEnum)
            // {
            //     return TryGetConverter(typeof(Enum), to, out converter);
            // }
            
            converter = null;
            return false;
        }

        /// <summary>
        /// Creates a delegate converter for the specified types. <br/>
        /// </summary>
        /// <param name="readConverter"></param>
        /// <param name="writeConverter"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static (Delegate conversion, Delegate reverseConversion) GetDelegates(IConverter readConverter, IConverter writeConverter, Type from, Type to)
        {
            if (from == to)
            {
                return (null, null);
            }
            var delegateConverterType = typeof(DelegateConverter<,>).MakeGenericType(from, to);
            return ((IDelegateConverter)CreateInstance(delegateConverterType, readConverter, writeConverter)).GetConversions();
        }

        /// <summary>
        /// Registers a new converter into the factore store. <br/>
        /// If there is already a converter which converts from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>, 
        /// then the specified one will overwrite the existing one. <br/>
        /// This method can be used to overwrite an existing conversion as well.
        /// </summary>
        /// <typeparam name="TFrom">The type the converter converts from</typeparam>
        /// <typeparam name="TTo">The type the converter converts to</typeparam>
        /// <param name="converter">The converter to be registered</param>
        public static void Register<TFrom, TTo>(IConverter<TFrom, TTo> converter)
        {
            if (_convertersById.TryGetValue(converter.Id, out var existing))
            {
                Debug.LogErrorFormat("{0}: Converter with id {1} already exists of type {2}. Overwritting...",
                                    nameof(ConvertersFactory), converter.Id, existing.GetType().Name);
            }

            _convertersById[converter.Id] = converter;

            AddConverter(null, converter);

            var baseType = typeof(TTo).BaseType;
            while (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
            {
                AddConverter(baseType, converter);
                baseType = baseType.BaseType;
            }

            //AddTransitiveConversion(converter, steps: 3);
        }

        /// <summary>
        /// Register a template for subsequent converters creation. <br/>
        /// The template serves as a deferred converter creation and registration.
        /// </summary>
        /// <typeparam name="T">The type of the converter for the template</typeparam>
        /// <param name="id">The ids the converters will be based on</param>
        /// <param name="isSafe">Whether the converters will be safe or not</param>
        public static void RegisterTemplate<T>(string id = null, bool isSafe = true) where T : IConverter
        {
            RegisterTemplateInternal(typeof(T), id, isSafe);
        }

        /// <summary>
        /// Register a template for subsequent converters creation. <br/>
        /// The template serves as a deferred converter creation and registration.
        /// </summary>
        /// <param name="converterType">The type of the converter</param>
        /// <param name="id"></param>
        /// <param name="isSafe">Whether the conversion will be safe or not</param>
        public static void RegisterTemplate(Type converterType, string id = null, bool isSafe = true)
        {
            if (!typeof(IConverter).IsAssignableFrom(converterType))
            {
                Debug.LogErrorFormat("{0}: Type {1} is not a {2} type",
                                    nameof(ConvertersFactory), converterType, nameof(IConverter));
                return;
            }
            RegisterTemplateInternal(converterType, id, isSafe);
        }

        private static void RegisterTemplateInternal(Type converterType, string id, bool isSafe)
        {
            if (string.IsNullOrEmpty(id))
            {
                try
                {
                    var converter = (IConverter)CreateInstance(converterType);
                    id = converter.Id;
                }
                finally
                {
                    // As a fallback 
                    if (string.IsNullOrEmpty(id))
                    {
                        id = StringUtility.NicifyName(converterType.Name);
                        int suffix = 1;
                        while (_templatesIds.Contains(id))
                        {
                            id = StringUtility.NicifyName(converterType.Name + $" {suffix++}");
                        }
                    }
                }
            }

            foreach (var template in ConverterTemplate.CreateTemplates(id, isSafe, converterType))
            {
                var key = (template.FromType, template.ToType);
                if (!_templates.TryGetValue(key, out var templatesList))
                {
                    templatesList = new List<ConverterTemplate>();
                    _templates[key] = templatesList;
                }
                if (!templatesList.Contains(template))
                {
                    templatesList.Add(template);
                }

                if (!_templatesByType.TryGetValue(template.ToType, out var toTypeList))
                {
                    toTypeList = new List<ConverterTemplate>();
                    _templatesByType[template.ToType] = toTypeList;
                }
                if (!toTypeList.Contains(template))
                {
                    toTypeList.Add(template);
                }

                if (!template.HasContravariantConversion)
                {
                    continue;
                } 
                
                if(!_contravariantTemplates.TryGetValue(key, out var contravariantList))
                {
                    contravariantList = new List<ConverterTemplate>();
                    _contravariantTemplates[key] = contravariantList;
                }
                if (!contravariantList.Contains(template))
                {
                    contravariantList.Add(template);
                }
            }
        }

        /// <summary>
        /// Gets whether the type <paramref name="to"/> has templates or not.
        /// </summary>
        /// <param name="to">THe type to get whether there are templates or not</param>
        /// <returns>True if there are templates for <paramref name="to"/> type, false otherwise</returns>
        public static bool HasTemplatesFor(Type to)
        {
            if (_templatesByType.ContainsKey(to))
            {
                return true;
            }
            foreach (var pair in _templatesByType)
            {
                if (to.IsAssignableFrom(pair.Key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets whether there are templates for conversions <paramref name="from"/> type to <paramref name="to"/> type
        /// </summary>
        /// <param name="from">The type to convert from</param>
        /// <param name="to">The type to convert to</param>
        /// <returns>True if there are templates, false otherwise</returns>
        public static bool HasTemplatesFor(Type from, Type to)
        {
            if (_templates.ContainsKey((from, to)))
            {
                return true;
            }
            
            foreach (var pair in _templates)
            {
                if (pair.Key.from.IsAssignableFrom(from) && to.IsAssignableFrom(pair.Key.to))
                {
                    return true;
                }
            }

            if (_contravariantTemplates.ContainsKey((from, to)))
            {
                return true;
            }
            
            foreach (var pair in _contravariantTemplates)
            {
                if (pair.Key.from.IsAssignableFrom(from) && pair.Key.to.IsAssignableFrom(to))
                {
                    return true;
                }
            }

            if (from.IsEnum)
            {
                return HasTemplatesFor(typeof(Enum), to);
            }
            
            return false;
        }

        /// <summary>
        /// Gets the templates for conversion to <paramref name="to"/> type.
        /// </summary>
        /// <param name="to">The type to convert to</param>
        /// <returns>A read-only list of templates</returns>
        public static IReadOnlyList<IConverterTemplate> GetTemplatesFor(Type to)
        {
            List<IConverterTemplate> templates = new List<IConverterTemplate>();
            if (_templatesByType.TryGetValue(to, out var list))
            {
                templates.AddRange(list);
            }
            foreach (var pair in _templatesByType)
            {
                if (!to.IsAssignableFrom(pair.Key))
                {
                    continue;
                }
                foreach (var template in pair.Value)
                {
                    if (!templates.Contains(template))
                    {
                        templates.Add(template);
                    }
                }
            }
            return templates;
        }

        /// <summary>
        /// Gets the templates for conversion from <paramref name="from"/> type to <paramref name="to"/> type.
        /// </summary>
        /// <param name="from">The type to convert from</param>
        /// <param name="to">The type to convert to</param>
        /// <returns>A read-only list of templates</returns>
        public static IReadOnlyList<IConverterTemplate> GetTemplates(Type from, Type to)
        {
            List<IConverterTemplate> templates = new List<IConverterTemplate>();
            LoadTemplates(from, to, templates);
            if (from.IsEnum)
            {
                LoadTemplates(typeof(Enum), to, templates);
            }
            return templates;
        }

        private static void LoadTemplates(Type from, Type to, List<IConverterTemplate> templates)
        {
            if (_templates.TryGetValue((from, to), out var list))
            {
                foreach (var element in list)
                {
                    if (!templates.Contains(element))
                    {
                        templates.Add(element);
                    }
                }
            }
            foreach (var pair in _templates)
            {
                if (!pair.Key.from.IsAssignableFrom(from) || !to.IsAssignableFrom(pair.Key.to))
                {
                    continue;
                }
                foreach (var template in pair.Value)
                {
                    if (!templates.Contains(template))
                    {
                        templates.Add(template);
                    }
                }
            }
            
            if (_contravariantTemplates.TryGetValue((from, to), out var contravariantList))
            {
                foreach (var element in contravariantList)
                {
                    if (!templates.Contains(element))
                    {
                        templates.Add(element);
                    }
                }
            }
            
            foreach (var pair in _contravariantTemplates)
            {
                if (!pair.Key.from.IsAssignableFrom(from) || !pair.Key.to.IsAssignableFrom(to))
                {
                    continue;
                }
                foreach (var template in pair.Value)
                {
                    if (!templates.Contains(template))
                    {
                        templates.Add(template);
                    }
                }
            }
        }

        private static void AddConverter<TFrom, TTo>(Type type, IConverter<TFrom, TTo> converter)
        {
            var key = type ?? typeof(TTo);
            if (!_convertersByType.TryGetValue(key, out var dictionary))
            {
                dictionary = new Dictionary<Type, IConverter>();
                _convertersByType.Add(key, dictionary);
            }
            // Allow to overwrite the dictionary if the converter is precisely targeting TTo type
            if (type == null || !dictionary.ContainsKey(typeof(TFrom)))
            {
                dictionary[typeof(TFrom)] = converter;
            }

            if (type == null)
            {
                Func<TFrom, TTo> del = converter.Convert;
                if (!_conversions.TryGetValue((typeof(TFrom), typeof(TTo)), out var existing) || existing.isTransitive)
                {
                    _conversions[(typeof(TFrom), typeof(TTo))] = (del, false, converter.IsSafe);
                }
            }
            else
            {
                var delType = typeof(Func<,>).MakeGenericType(typeof(TFrom), type);
                var del = Delegate.CreateDelegate(delType, converter, nameof(IConverter<TFrom, TTo>.Convert));
                if (!_conversions.TryGetValue((typeof(TFrom), type), out var existing) || existing.isTransitive)
                {
                    _conversions[(typeof(TFrom), type)] = (del, false, converter.IsSafe);
                }
            }
        }

        /// <summary>
        /// Registers a new conversion into the system.
        /// </summary>
        /// <typeparam name="TFrom">The type to convert from</typeparam>
        /// <typeparam name="TTo">The type to convert to</typeparam>
        /// <param name="id">The id of the conversion, it will be used to further retrieve this conversion when needed</param>
        /// <param name="conversion">The conversion functor</param>
        /// <param name="isSafe">Whether the conversion is considered safe or not. An safe conversion guarantees to convert always</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TFrom, TTo>(string id, Func<TFrom, TTo> conversion, bool isSafe = true)
        {
            Register(new Converter<TFrom, TTo>(id, conversion, isSafe));
        }

        /// <summary>
        /// Registers a new conversion into the system.
        /// </summary>
        /// <typeparam name="TFrom">The type to convert from</typeparam>
        /// <typeparam name="TTo">The type to convert to</typeparam>
        /// <param name="id">The id of the conversion, it will be used to further retrieve this conversion when needed</param>
        /// <param name="conversion">The conversion functor</param>
        /// <param name="isSafe">Whether the conversion is considered safe or not. An safe conversion guarantees to convert always</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TFrom, TTo>(Func<TFrom, TTo> conversion, bool isSafe = true)
        {
            Register(new Converter<TFrom, TTo>(string.Concat(typeof(TFrom).Name, "To", typeof(TTo).Name), conversion, isSafe));
        }

        /// <summary>
        /// Gets whether a conversion with specified <paramref name="converterId"/> exists in the system or not.
        /// </summary>
        /// <param name="converterId"></param>
        /// <returns>True if the conversion exists, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConverterExists(string converterId) => _convertersById.ContainsKey(converterId);


        private static bool TryGetOperatorConversion(Type from, Type to, out Delegate conversion, out IConverter converter)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var allStaticMethods = new List<MethodInfo>(from.GetMethods(flags));
            allStaticMethods.AddRange(to.GetMethods(flags));
            var validOperators = new List<(MethodInfo method, int depth)>();
            foreach (var method in allStaticMethods)
            {
                // Not an operator method
                if (method.Name != "op_Implicit" && method.Name != "op_Explicit")
                {
                    continue;
                }
                // Returning type is not compatible
                if (!to.IsAssignableFrom(method.ReturnType))
                {
                    continue;
                }
                var parameters = method.GetParameters();
                // Parameters are not valid
                if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(from))
                {
                    continue;
                }
                validOperators.Add((method, method.ReturnType.GetTypeHierarchyDepth()));
            }

            conversion = null;
            converter = null;

            if (validOperators.Count > 0)
            {

                // Create the conversion function
                var conversionFuncType = typeof(Func<,>).MakeGenericType(from, to);
                // Descending order: From topmost type in type hierarchy
                validOperators.Sort((a, b) => b.depth - a.depth);
                // Register all operators
                foreach (var (method, depth) in validOperators)
                {

                    var conversionFunc = default(Delegate);
                    try
                    {
                        conversionFunc = method.CreateDelegate(conversionFuncType, null);
                    }
                    catch(ArgumentException)
                    {
                        // Most probably incompatible
                        continue;
                    }

                    // Create the converter
                    var converterId = $"{method.Name.Replace("op_", "")} {from.GetAliasName()} to {to.GetAliasName()}";
                    var converterType = typeof(Converter<,>).MakeGenericType(from, to);
                    var converterObj = CreateInstance(converterType,
                                                            converterId,
                                                            conversionFunc,
                                                            true);
                    // Add the converter
                    if (!_convertersByType.TryGetValue(to, out var dictionary))
                    {
                        dictionary = new Dictionary<Type, IConverter>();
                        _convertersByType.Add(to, dictionary);
                    }
                    // Allow to overwrite the dictionary if the converter is precisely targeting To type
                    if (!dictionary.ContainsKey(from))
                    {
                        dictionary[from] = converterObj as IConverter;
                    }

                    if (conversion == null)
                    {
                        conversion = conversionFunc;
                        converter = converterObj as IConverter;

                        // The first method should register anyways --> it is for the requested types
                        _convertersById[converterId] = converterObj as IConverter;
                        _conversions[(from, to)] = (conversionFunc, false, converter.IsSafe);
                    }
                    else
                    {
                        // Register to the id as well
                        if (!_convertersById.ContainsKey(converterId))
                        {
                            _convertersById[converterId] = converterObj as IConverter;
                        }

                        // Register the conversion as well
                        var key = (method.GetParameters()[0].ParameterType, method.ReturnType);
                        if (!_conversions.ContainsKey(key))
                        {
                            _conversions[key] = (conversionFunc, false, converter.IsSafe);
                        }
                    }
                }
            }

            if(conversion == null && typeof(UnityEngine.Object).IsAssignableFrom(from) && to == typeof(bool))
            {
                var functorType = typeof(Func<,>).MakeGenericType(from, to);
                var genericMethod = typeof(ConvertersFactory).GetMethod(nameof(UnityObjectIsAlive), BindingFlags.NonPublic | BindingFlags.Static);
                var method = genericMethod.MakeGenericMethod(from);
                var conversionFunc = method.CreateDelegate(functorType);

                var converterId = $"{from.GetAliasName()} is Alive";
                var converterType = typeof(Converter<,>).MakeGenericType(from, to);
                var converterObj = CreateInstance(converterType,
                                                        converterId,
                                                        conversionFunc,
                                                        true);
                // Add the converter
                if (!_convertersByType.TryGetValue(to, out var dictionary))
                {
                    dictionary = new Dictionary<Type, IConverter>();
                    _convertersByType.Add(to, dictionary);
                }

                // Allow to overwrite the dictionary if the converter is precisely targeting To type
                if (!dictionary.ContainsKey(from))
                {
                    dictionary[from] = converterObj as IConverter;
                }

                conversion = conversionFunc;
                converter = converterObj as IConverter;

                _convertersById[converterId] = converterObj as IConverter;
                _conversions[(from, to)] = (conversionFunc, false, converter.IsSafe);
            }

            return conversion != null;
        }

        private static bool UnityObjectIsAlive<T>(T unityObj) where T : UnityEngine.Object => unityObj;
    }
}