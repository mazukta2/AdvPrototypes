using System;
using System.Collections.Generic;
using System.Reflection;
using Postica.BindingSystem.Accessors;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    public sealed class ConverterAccessor<S, Tfrom, Tto> :
        IAccessor, IAccessor<Tto>, IAccessor<S, Tto>,
        IConcurrentAccessor, IConcurrentAccessor<Tto>, IConcurrentAccessor<S, Tto>,
        IWrapperAccessor
    {   
        private readonly IAccessor<S, Tfrom> _accessor;
        private readonly Func<Tfrom, Tto> _conversion;
        private readonly Func<Tto, Tfrom> _reverseConversion;
        
        private S _cachedValue;

        private ref S Ref(S source)
        {
            _cachedValue = source;
            return ref _cachedValue;
        }
        
        private static MethodInfo _getValueProviderConversionMethod;
        private static Dictionary<Type, Func<Tfrom, Tto>> _readValueProviderConversions;
        private static Dictionary<Type, Func<Tto, Tfrom>> _writeValueProviderConversions;

        public Type ObjectType => typeof(S);

        public Type ValueType => typeof(Tto);

        public bool CanRead => (_accessor as IAccessor).CanRead && _conversion != null;

        public bool CanWrite => (_accessor as IAccessor).CanWrite && _reverseConversion != null;

        internal IAccessor<S, Tfrom> SourceAccessor => _accessor;

        public ConverterAccessor(IAccessor<S, Tfrom> accessor, IConverter readConverter, IConverter writeConverter)
        {
            _accessor = accessor;
            if (readConverter is IConverter<Tfrom, Tto> ctConverter)
            {
                _conversion = ctConverter.Convert;
            }
            else if(readConverter is IConverter<object, Tto> genericConverter)
            {
                _conversion = v => genericConverter.Convert(v);
            }
            else if(!TryGetValueProviderConversion(readConverter, out _conversion))
            {
                _conversion = ConvertersFactory.GetConversion<Tfrom, Tto>();
            }
            
            if (writeConverter is IConverter<Tto, Tfrom> tcConverter)
            {
                _reverseConversion = tcConverter.Convert;
            }
            else if(!TryGetValueProviderConversion(writeConverter, out _reverseConversion))
            {
                _reverseConversion = ConvertersFactory.GetConversion<Tto, Tfrom>();
            }
            //if(_conversion == null)
            //{
            //    throw new InvalidOperationException($"{nameof(DelegateFactory)}: Unable to get conversions from {typeof(C).FullName} to {typeof(T).FullName}");
            //}
        }

        private ConverterAccessor(IAccessor<S, Tfrom> accessor, Func<Tfrom, Tto> conversion, Func<Tto, Tfrom> reverseConversion)
        {
            _accessor = accessor;
            _conversion = conversion;
            _reverseConversion = reverseConversion;
        }

        private static bool TryGetValueProviderConversion(IConverter converter, out Func<Tfrom, Tto> conversion)
        {
            if (typeof(IValueProvider).IsAssignableFrom(typeof(Tfrom)))
            {
                var providerType = typeof(Tfrom).GetInterface(typeof(IValueProvider<int>).Name);
                _readValueProviderConversions ??= new();
                if (_readValueProviderConversions.TryGetValue(providerType, out conversion))
                {
                    return conversion != null;
                }
                _getValueProviderConversionMethod ??= typeof(ConverterAccessor<S, Tfrom, Tto>).GetMethod(nameof(GetValueProviderConversion), BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethod = _getValueProviderConversionMethod.MakeGenericMethod(providerType);
                var specificMethod = genericMethod.CreateDelegate(typeof(Func<IConverter, Func<Tfrom, Tto>>)) as Func<IConverter, Func<Tfrom, Tto>>;
                conversion = specificMethod(converter);
                _readValueProviderConversions[providerType] = conversion;
                return conversion != null;
            }
            conversion = null;
            return false;
        }
        
        private static Func<Tfrom, Tto> GetValueProviderConversion<TProvider>(IConverter converter)
        {
            if (converter is not IConverter<TProvider, Tto> specificConverter)
            {
                return null;
            }
            return v => v is TProvider valueProvider ? specificConverter.Convert(valueProvider) : (Tto)converter.Convert(v);
        }
        
        private static bool TryGetValueProviderConversion(IConverter converter, out Func<Tto, Tfrom> conversion)
        {
            if (typeof(IValueProvider).IsAssignableFrom(typeof(Tto)))
            {
                var providerType = typeof(Tto).GetInterface(typeof(IValueProvider<int>).Name);
                _writeValueProviderConversions ??= new();
                if (_writeValueProviderConversions.TryGetValue(providerType, out conversion))
                {
                    return conversion != null;
                }
                _getValueProviderConversionMethod ??= typeof(ConverterAccessor<S, Tfrom, Tto>).GetMethod(nameof(GetWriteValueProviderConversion), BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethod = _getValueProviderConversionMethod.MakeGenericMethod(providerType);
                var specificMethod = genericMethod.CreateDelegate(typeof(Func<IConverter, Func<Tto, Tfrom>>)) as Func<IConverter, Func<Tto, Tfrom>>;
                conversion = specificMethod(converter);
                _writeValueProviderConversions[providerType] = conversion;
                return conversion != null;
            }
            conversion = null;
            return false;
        }
        
        private static Func<Tto, Tfrom> GetWriteValueProviderConversion<TProvider>(IConverter converter)
        {
            if (converter is not IConverter<TProvider, Tfrom> specificConverter)
            {
                return null;
            }
            return v => v is TProvider valueProvider ? specificConverter.Convert(valueProvider) : (Tfrom)converter.Convert(v);
        }

        public object GetValue(object target)
        {
            return _conversion(_accessor.GetValue((S)target));
        }

        public Tto GetValue(S target)
        {
            return _conversion(_accessor.GetValue(target));
        }

        public IConcurrentAccessor MakeConcurrent()
        {
            return new ConverterAccessor<S, Tfrom, Tto>((_accessor as IAccessor).MakeConcurrent() as IAccessor<S, Tfrom>, _conversion, _reverseConversion);
        }

        public IAccessor Duplicate()
        {
            return new ConverterAccessor<S, Tfrom, Tto>(_accessor, _conversion, _reverseConversion);
        }

        public void SetValue(object target, object value)
        {
            _accessor.SetValue(ref Ref((S)target), _reverseConversion((Tto)value));
        }

        public void SetValue(object target, in Tto value)
        {
            _accessor.SetValue(ref Ref((S)target), _reverseConversion(value));
        }

        public void SetValue(ref S target, in Tto value)
        {
            _accessor.SetValue(ref target, _reverseConversion(value));
        }

        Tto IAccessor<Tto>.GetValue(object target)
        {
            return _conversion(_accessor.GetValue((S)target));
        }

        Tto IConcurrentAccessor<Tto>.GetValue(object target)
        {
            return _conversion((_accessor as IConcurrentAccessor<Tfrom>).GetValue((S)target));
        }

        Tto IConcurrentAccessor<S, Tto>.GetValue(S target)
        {
            return _conversion((_accessor as IConcurrentAccessor<S, Tfrom>).GetValue(target));
        }

        object IConcurrentAccessor.GetValue(object target)
        {
            return _conversion((Tfrom)(_accessor as IConcurrentAccessor).GetValue(target));
        }

        void IConcurrentAccessor<Tto>.SetValue(object target, in Tto value)
        {
            (_accessor as IConcurrentAccessor<Tfrom>).SetValue(target, _reverseConversion(value));
        }

        void IConcurrentAccessor<S, Tto>.SetValue(S target, in Tto value)
        {
            (_accessor as IConcurrentAccessor<S, Tfrom>).SetValue(target, _reverseConversion(value));
        }

        void IConcurrentAccessor.SetValue(object target, object value)
        {
            (_accessor as IConcurrentAccessor).SetValue(target, _reverseConversion((Tto)value));
        }

        IConcurrentAccessor<Tto> IAccessor<Tto>.MakeConcurrent()
        {
            return new ConverterAccessor<S, Tfrom, Tto>((_accessor as IAccessor<Tfrom>).MakeConcurrent() as IAccessor<S, Tfrom>, _conversion, _reverseConversion);
        }

        IConcurrentAccessor<S, Tto> IAccessor<S, Tto>.MakeConcurrent()
        {
            return new ConverterAccessor<S, Tfrom, Tto>(_accessor.MakeConcurrent() as IAccessor<S, Tfrom>, _conversion, _reverseConversion);
        }

        public IEnumerable<object> GetInnerAccessors() => new object[] { _accessor };
    }

}
