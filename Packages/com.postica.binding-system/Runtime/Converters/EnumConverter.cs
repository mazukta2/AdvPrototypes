using System;
using System.Collections.Generic;
using System.Linq;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Converters
{
    internal static class EnumConverters
    {
        internal interface IEnumConverter
        {
            Type EnumType { get; }
            Type ToType { get; }
        }
        
        public static void RegisterDefaultTypes()
        {
            ConvertersFactory.RegisterTemplate<EnumToIntConverter>();
            ConvertersFactory.RegisterTemplate<EnumToStringConverter>();
            ConvertersFactory.RegisterTemplate<EnumToBoolConverter>();
            ConvertersFactory.RegisterTemplate<EnumToFloatConverter>();
            ConvertersFactory.RegisterTemplate<EnumToDoubleConverter>();
            ConvertersFactory.RegisterTemplate<EnumToLongConverter>();
            ConvertersFactory.RegisterTemplate<EnumToShortConverter>();
            ConvertersFactory.RegisterTemplate<EnumToByteConverter>();
            ConvertersFactory.RegisterTemplate<EnumToSByteConverter>();
            ConvertersFactory.RegisterTemplate<EnumToCharConverter>();
            ConvertersFactory.RegisterTemplate<EnumToDecimalConverter>();
            ConvertersFactory.RegisterTemplate<EnumToVector2Converter>();
            ConvertersFactory.RegisterTemplate<EnumToVector2IntConverter>();
            ConvertersFactory.RegisterTemplate<EnumToVector3IntConverter>();
            ConvertersFactory.RegisterTemplate<EnumToVector3Converter>();
            ConvertersFactory.RegisterTemplate<EnumToVector4Converter>();
            ConvertersFactory.RegisterTemplate<EnumToColorConverter>();
            ConvertersFactory.RegisterTemplate<EnumToQuaternionConverter>();
            ConvertersFactory.RegisterTemplate<EnumToRectConverter>();
            ConvertersFactory.RegisterTemplate<EnumToBoundsConverter>();
            ConvertersFactory.RegisterTemplate<EnumToMatrix4x4Converter>();
            ConvertersFactory.RegisterTemplate<EnumToAnimationCurveConverter>();
            ConvertersFactory.RegisterTemplate<EnumToGradientConverter>();
            ConvertersFactory.RegisterTemplate<EnumUnityObjectConverter>();
            // Register specific component converters if needed
        }
    }
    
    /// <summary>
    /// A converter which converts from an enum to a different type.
    /// </summary>
    [Serializable]
    [HideMember]
    public class EnumConverter<TEnum, T> : IConverter<TEnum, T>,
        EnumConverters.IEnumConverter,
        IDynamicComponent where TEnum : Enum
    {
        [Tooltip("The choices to convert from the enum to the target type")]
        public Choices choices;
        
        private Dictionary<TEnum, ReadOnlyBind<T>> _enumToValueMap;
        
        public string Id => $"{typeof(TEnum).UserFriendlyName()} to {typeof(T).UserFriendlyName()} Converter";
        public string Description => $"Converts an {typeof(TEnum).UserFriendlyName()} enum to {typeof(T).UserFriendlyName()}";
        public bool IsSafe => true;

        public Type EnumType { get; } = typeof(TEnum);
        public Type ToType { get; } = typeof(T);
        
        public T Convert(TEnum value)
        {
            if (_enumToValueMap == null || _enumToValueMap.Count != choices.values.Count)
            {
                _enumToValueMap = new();
                var enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
                // Maybe an order by value or something similar could be added here
                
                var minCount = Mathf.Min(enumValues.Count, choices.values.Count);
                for (int i = 0; i < minCount; i++)
                {
                    _enumToValueMap[enumValues[i]] = choices.values[i];
                }
            }
            
            return _enumToValueMap.TryGetValue(value, out var bind) ? bind.Value : choices.fallback;
        }

        [Serializable]
        public class Choices
        {
            [Tooltip("The values to convert to")]
            public List<ReadOnlyBind<T>> values = new();
            [Tooltip("The fallback value to use if the enum value is not found in the choices")]
            public ReadOnlyBind<T> fallback;
        }

        public bool IsDynamic => choices.values.Any(v => v.IsBound) || choices.fallback.IsBound;
    }
    
    /// <summary>
    /// A converter which converts from an enum to a different type.
    /// </summary>
    [Serializable]
    [HideMember]
    public class EnumConverter<T> : IConverter<Enum, T>,
        IContextConverter,
        IConverterCompiler,
        EnumConverters.IEnumConverter,
        IDynamicComponent
    {
        [Tooltip("The choices to convert from the enum to the target type")]
        public Choices choices;
        [HideInInspector]
        [SerializeField]
        private SerializedType _enumType;
        
        private Dictionary<Enum, ReadOnlyBind<T>> _enumToValueMap;
        
        private Dictionary<Type, IConverter> _compiledConverters = new();
        
        public Type EnumType
        {
            get => _enumType?.Get();
            set => _enumType = value;
        }
        
        public virtual Type ToType => typeof(T);
        
        public string Id => $"{_enumType?.Name ?? "Enum"} to {typeof(T).UserFriendlyName()} Converter";
        public string Description => $"Converts an {_enumType?.Name ?? "Enum"} enum to {typeof(T).UserFriendlyName()}, by specifying the mapping in the choices.";
        public bool IsSafe => true;
        
        public T Convert(Enum value)
        {
            if (_enumToValueMap == null || _enumToValueMap.Count != choices.values.Count)
            {
                _enumToValueMap = new();
                var enumValues = Enum.GetValues(_enumType).Cast<Enum>().ToList();
                // Maybe an order by value or something similar could be added here
                
                var minCount = Mathf.Min(enumValues.Count, choices.values.Count);
                for (int i = 0; i < minCount; i++)
                {
                    _enumToValueMap[enumValues[i]] = choices.values[i];
                }
            }
            
            return _enumToValueMap.TryGetValue(value, out var bind) ? bind.Value : choices.fallback;
        }
        
        
        public void SetContext(object context, Type contextType, string path)
        {
            try
            {
                var memberInfo = AccessorsFactory.GetMemberAtPath(context?.GetType() ?? contextType, path);
                if (memberInfo == null)
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member not found.");
                    return;
                }
                
                var memberType = memberInfo.GetMemberType();
                if (memberType?.IsEnum == true)
                {
                    FillUpKnownValues(memberType);
                    _enumType = memberType;
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member Value is not an Enum.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{GetType().Name}: Unable to set context. {e}");
            }
        }

        private void FillUpKnownValues(Type enumType)
        {
            if(_enumType?.Get() == enumType)
            {
                return; // Already set
            }
            
            var enumValues = Enum.GetValues(enumType).Cast<int>().ToList();
            enumValues.Sort();
            choices ??= new Choices();
            choices.values ??= new List<ReadOnlyBind<T>>();
            choices.values.Clear();
            switch (typeof(T).Name)
            {
                case nameof(Int16):
                    Fill(v => (short)v);
                    break;
                case nameof(Int32):
                    Fill(v => v);
                    break;
                case nameof(Int64):
                    Fill(v => (long)v);
                    break;
                case nameof(Byte):
                    Fill(v => (byte)v);
                    break;
                case nameof(SByte):
                    Fill(v => (sbyte)v);
                    break;
                case nameof(Char):
                    Fill(v => Enum.GetName(enumType, v)?[0]);
                    break;
                case nameof(String):
                    Fill(v => Enum.GetName(enumType, v));
                    break;
                case nameof(Boolean):
                    Fill(v => v != 0);
                    break;
                case nameof(Single):
                    Fill(v => (float)v);
                    break;
                case nameof(Double):
                    Fill(v => (double)v);
                    break;
                case nameof(Decimal):
                    Fill(v => (decimal)v);
                    break;
                
            }

            void Fill<TValue>(Func<int, TValue> valueGetter)
            {
                foreach (var value in enumValues)
                {
                    var tVal = valueGetter(value) is T tValue ? tValue : default;
                    var bind = tVal.Bind();
                    choices.values.Add(bind);
                }
            }
        }

        [Serializable]
        public class Choices
        {
            [Tooltip("The values to convert to")]
            public List<ReadOnlyBind<T>> values = new();
            [Tooltip("The fallback value to use if the enum value is not found in the choices")]
            public ReadOnlyBind<T> fallback;
        }

        public IConverter Compile(Type from, Type to)
        {
            if (from is not { IsEnum: true })
            {
                throw new InvalidOperationException($"Invalid enum type: {_enumType?.Name}");
            }
            
            if(_compiledConverters.TryGetValue(from, out var converter))
            {
                return converter;
            }
            
            var genericConverterType = typeof(SpecificEnumConverter<>);
            var specificConverterType = genericConverterType.MakeGenericType(to, from);
            var specificConverter = (IConverter)Activator.CreateInstance(specificConverterType, this);
            _compiledConverters[from] = specificConverter;
            return specificConverter;
        }
        
        [Serializable]
        private class SpecificEnumConverter<TEnum> : IConverter<TEnum, T>
            where TEnum : Enum
        {
            private readonly Dictionary<TEnum, ReadOnlyBind<T>> _enumToValueMap;
            private readonly ReadOnlyBind<T> _fallback;

            public SpecificEnumConverter(EnumConverter<T> converter)
            {
                Id = converter.Id;
                Description = converter.Description;
                
                _fallback = converter.choices.fallback;
                _enumToValueMap = new();
                var enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
                // Maybe an order by value or something similar could be added here
                
                var minCount = Mathf.Min(enumValues.Count, converter.choices.values.Count);
                for (int i = 0; i < minCount; i++)
                {
                    _enumToValueMap[enumValues[i]] = converter.choices.values[i];
                }
            }

            public string Id { get; }
            public string Description { get; }
            public bool IsSafe => true;

            public T Convert(TEnum value)
            {
                return _enumToValueMap.TryGetValue(value, out var bind) ? bind.Value : _fallback;
            }

            object IConverter.Convert(object value)
            {
                if (value is TEnum enumValue)
                {
                    return Convert(enumValue);
                }
                throw new InvalidCastException($"Cannot convert {value.GetType()} to {typeof(TEnum)}");
            }
        }

        public bool IsDynamic => choices.values.Any(v => v.IsBound) || choices.fallback.IsBound;
    }
    
    /// <summary>
    /// A converter which converts from an enum to a different type of Unity Object.
    /// </summary>
    [Serializable]
    [HideMember]
    public class EnumUnityObjectConverter : 
        IConverter<Enum, Object>, 
        IContextConverter, 
        IConverterCompiler, 
        EnumConverters.IEnumConverter, 
        IContravariantConverter,
        IDynamicComponent
    {
        [Tooltip("The choices to convert from the enum to the target type")]
        public Choices choices;
        
        [HideInInspector]
        [SerializeField]
        private SerializedType _enumType;
        
        private Dictionary<Enum, ReadOnlyBind<Object>> _enumToValueMap;
        
        private Dictionary<Type, IConverter> _compiledConverters = new();
        
        public Type EnumType
        {
            get => _enumType?.Get();
            set => _enumType = value;
        }
        
        public Type ToType => ActualTargetType ?? typeof(Object);

        public Type ActualTargetType
        {
            get
            {
                choices ??= new(); 
                return choices.targetType; 
            }
            set
            {
                choices ??= new(); 
                choices.targetType = value; 
            }
        }
        
        public string Id => $"{_enumType?.Name ?? "Enum"} to {ToType.UserFriendlyName()} Converter";
        public string Description => $"Converts an {_enumType?.Name ?? "Enum"} enum to {ToType.UserFriendlyName()}, by specifying the mapping in the choices.";
        public bool IsSafe => true;
        
        public Object Convert(Enum value)
        {
            if (_enumToValueMap == null || _enumToValueMap.Count != choices.values.Count)
            {
                _enumToValueMap = new();
                var enumValues = Enum.GetValues(_enumType).Cast<Enum>().ToList();
                // Maybe an order by value or something similar could be added here
                
                var minCount = Mathf.Min(enumValues.Count, choices.values.Count);
                for (int i = 0; i < minCount; i++)
                {
                    _enumToValueMap[enumValues[i]] = choices.values[i];
                }
            }
            
            return _enumToValueMap.TryGetValue(value, out var bind) ? bind.Value : choices.fallback;
        }
        
        
        public void SetContext(object context, Type contextType, string path)
        {
            try
            {
                var memberInfo = AccessorsFactory.GetMemberAtPath(context?.GetType() ?? contextType, path);
                if (memberInfo == null)
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member not found.");
                    return;
                }
                
                var memberType = memberInfo.GetMemberType();
                if (memberType?.IsEnum == true)
                {
                    _enumType = memberType;
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member Value is not an Enum.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{GetType().Name}: Unable to set context. {e}");
            }
        }

        [Serializable]
        public class Choices
        {
            [Tooltip("The values to convert to")]
            [BindTypeSource(nameof(targetType))]
            public List<ReadOnlyBind<Object>> values = new();
            [Tooltip("The fallback value to use if the enum value is not found in the choices")]
            [BindTypeSource(nameof(targetType))]
            public ReadOnlyBind<Object> fallback;
            [HideInInspector]
            [SerializeField]
            private SerializedType _targetType;
            
            public Type targetType
            {
                get => _targetType?.Get();
                set => _targetType = value;
            }
        }

        public IConverter Compile(Type from, Type to)
        {
            if (from is not { IsEnum: true })
            {
                throw new InvalidOperationException($"Invalid enum type: {_enumType?.Name}");
            }
            
            if(_compiledConverters.TryGetValue(from, out var converter))
            {
                return converter;
            }
            
            var genericConverterType = typeof(SpecificEnumConverter<,>);
            var specificConverterType = genericConverterType.MakeGenericType(from, to);
            var specificConverter = (IConverter)Activator.CreateInstance(specificConverterType, this);
            _compiledConverters[from] = specificConverter;
            return specificConverter;
        }
        
        private class SpecificEnumConverter<TEnum, T> : IConverter<TEnum, T>
            where TEnum : Enum
            where T : Object
        {
            private readonly Dictionary<TEnum, IValueProvider<Object>> _enumToValueMap;
            private readonly IValueProvider<Object> _fallback;

            public SpecificEnumConverter(EnumUnityObjectConverter converter)
            {
                Id = converter.Id;
                Description = converter.Description;
                
                _fallback = converter.choices.fallback;
                _enumToValueMap = new();
                var enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
                // Maybe an order by value or something similar could be added here
                
                var minCount = Mathf.Min(enumValues.Count, converter.choices.values.Count);
                for (int i = 0; i < minCount; i++)
                {
                    _enumToValueMap[enumValues[i]] = converter.choices.values[i];
                }
            }

            public string Id { get; }
            public string Description { get; }
            public bool IsSafe => true;

            public T Convert(TEnum value)
            {
                var result = _enumToValueMap.TryGetValue(value, out var bind) ? bind.Value : _fallback.Value;
                return result as T;
            }

            object IConverter.Convert(object value)
            {
                if (value is TEnum enumValue)
                {
                    return Convert(enumValue);
                }
                throw new InvalidCastException($"Cannot convert {value.GetType()} to {typeof(TEnum)}");
            }
        }

        public bool IsDynamic => choices.values.Any(v => v.IsBound) || choices.fallback.IsBound;
    }

    [Serializable] internal class EnumToIntConverter : EnumConverter<int> { }
    [Serializable]
    internal class EnumToStringConverter : EnumConverter<string> { }
    [Serializable]
    internal class EnumToBoolConverter : EnumConverter<bool> { }
    [Serializable]
    internal class EnumToFloatConverter : EnumConverter<float> { }
    [Serializable]
    internal class EnumToDoubleConverter : EnumConverter<double> { }
    [Serializable]
    internal class EnumToLongConverter : EnumConverter<long> { }
    [Serializable]
    internal class EnumToShortConverter : EnumConverter<short> { }
    [Serializable]
    internal class EnumToByteConverter : EnumConverter<byte> { }
    [Serializable]
    internal class EnumToSByteConverter : EnumConverter<sbyte> { }
    [Serializable]
    internal class EnumToCharConverter : EnumConverter<char> { }
    [Serializable]
    internal class EnumToDecimalConverter : EnumConverter<decimal> { }
    [Serializable]
    internal class EnumToVector2Converter : EnumConverter<Vector2> { }
    [Serializable]
    internal class EnumToVector2IntConverter : EnumConverter<Vector2Int> { }
    [Serializable]
    internal class EnumToVector3IntConverter : EnumConverter<Vector3Int> { }
    [Serializable]
    internal class EnumToVector3Converter : EnumConverter<Vector3> { }
    [Serializable]
    internal class EnumToVector4Converter : EnumConverter<Vector4> { }
    [Serializable]
    internal class EnumToColorConverter : EnumConverter<Color> { }
    [Serializable]
    internal class EnumToQuaternionConverter : EnumConverter<Quaternion> { }
    [Serializable]
    internal class EnumToRectConverter : EnumConverter<Rect> { }
    [Serializable]
    internal class EnumToBoundsConverter : EnumConverter<Bounds> { }
    [Serializable]
    internal class EnumToMatrix4x4Converter : EnumConverter<Matrix4x4> { }
    [Serializable]
    internal class EnumToAnimationCurveConverter : EnumConverter<AnimationCurve> { }
    [Serializable]
    internal class EnumToGradientConverter : EnumConverter<Gradient> { }
}