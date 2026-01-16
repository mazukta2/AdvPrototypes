using System;
using System.Collections.Generic;
using System.Linq;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Converters
{
    internal static class BooleanConverters
    {
        
        public static void RegisterDefaultTypes()
        {
            ConvertersFactory.RegisterTemplate<BooleanToIntConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToStringConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToBoolConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToFloatConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToDoubleConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToLongConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToShortConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToByteConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToSByteConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToCharConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToDecimalConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToVector2Converter>();
            ConvertersFactory.RegisterTemplate<BooleanToVector2IntConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToVector3IntConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToVector3Converter>();
            ConvertersFactory.RegisterTemplate<BooleanToVector4Converter>();
            ConvertersFactory.RegisterTemplate<BooleanToColorConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToQuaternionConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToRectConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToBoundsConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToMatrix4x4Converter>();
            ConvertersFactory.RegisterTemplate<BooleanToAnimationCurveConverter>();
            ConvertersFactory.RegisterTemplate<BooleanToGradientConverter>();
            ConvertersFactory.RegisterTemplate<BooleanUnityObjectConverter>();
            // Register specific component converters if needed
        }
    }
    
    /// <summary>
    /// A converter which converts from an enum to a different type.
    /// </summary>
    [Serializable]
    [HideMember]
    public class BooleanConverter<T> : IConverter<bool, T>, IDynamicComponent
    {
        [Tooltip("The choices to convert from the enum to the target type")]
        public Choices booleanMapping = CreateKnownChoices();
        
        private Dictionary<Boolean, ReadOnlyBind<T>> _enumToValueMap;
        
        private Dictionary<Type, IConverter> _compiledConverters = new();
        
        public virtual Type ToType => typeof(T);
        
        public string Id => $"Boolean to {typeof(T).UserFriendlyName()} Converter";
        public string Description => $"Converts a boolean value to {typeof(T).UserFriendlyName()}, by specifying the mapping in the choices.";
        public bool IsSafe => true;
        
        public T Convert(bool value)
        {
            return value ? booleanMapping.onTrue.Value : booleanMapping.onFalse.Value;
        }

        private static Choices CreateKnownChoices()
        {
            return typeof(T).Name switch
            {
                nameof(Int16) => Fill<short>(0, 1),
                nameof(Int32) => Fill(0, 1),
                nameof(Int64) => Fill<long>(0, 1),
                nameof(Byte) => Fill<byte>(0, 1),
                nameof(SByte) => Fill<sbyte>(0, 1),
                nameof(Char) => Fill('F', 'T'),
                nameof(String) => Fill("False", "True"),
                nameof(Single) => Fill(0f, 1f),
                nameof(Double) => Fill(0.0, 1.0),
                nameof(Decimal) => Fill<decimal>(0, 1),
                _ => new Choices()
            };

            Choices Fill<TValue>(TValue onFalse, TValue onTrue)
            {
                var choices = new Choices();
                choices.onFalse = onFalse is T tFalse ? tFalse.Bind() : new ReadOnlyBind<T>(default(T));
                choices.onTrue = onTrue is T tTrue ? tTrue.Bind() : new ReadOnlyBind<T>(default(T));
                return choices;
            }
        }

        [Serializable]
        public class Choices
        {
            [Tooltip("The value to set if the input value is false")]
            public ReadOnlyBind<T> onFalse = new();
            [Tooltip("The value to set if the input value is true")]
            public ReadOnlyBind<T> onTrue = new();
        }

        public bool IsDynamic => booleanMapping.onFalse.IsBound || booleanMapping.onTrue.IsBound;
    }
    
    /// <summary>
    /// A converter which converts from an enum to a different type of Unity Object.
    /// </summary>
    [Serializable]
    [HideMember]
    public class BooleanUnityObjectConverter : 
        IConverter<bool, Object>,
        IConverterCompiler, 
        IContravariantConverter,
        IDynamicComponent
    {
        [Tooltip("The choices to convert from the enum to the target type")]
        public Choices booleanMapping;
        
        private Dictionary<Boolean, ReadOnlyBind<Object>> _enumToValueMap;
        
        private Dictionary<Type, IConverter> _compiledConverters = new();
        
        public Type ToType => ActualTargetType ?? typeof(Object);

        public Type ActualTargetType
        {
            get
            {
                booleanMapping ??= new(); 
                return booleanMapping.targetType; 
            }
            set
            {
                booleanMapping ??= new(); 
                booleanMapping.targetType = value; 
            }
        }
        
        public string Id => $"Boolean to {ToType.UserFriendlyName()} Converter";
        public string Description => $"Converts a boolean value to {ToType.UserFriendlyName()}, by specifying the mapping in the choices.";
        public bool IsSafe => true;
        
        public Object Convert(bool value)
        {
            return value ? booleanMapping.onTrue.Value : booleanMapping.onFalse.Value;
        }

        [Serializable]
        public class Choices
        {
            [Tooltip("The value to set if the input value is false")]
            [BindTypeSource(nameof(targetType))]
            public ReadOnlyBind<Object> onFalse = new();
            [Tooltip("The value to set if the input value is true")]
            [BindTypeSource(nameof(targetType))]
            public ReadOnlyBind<Object> onTrue = new();
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
            if (from != typeof(bool))
            {
                throw new InvalidOperationException($"Invalid input type for {nameof(BooleanUnityObjectConverter)}. Expected bool, got {from.Name}.");
            }
            
            if(_compiledConverters.TryGetValue(from, out var converter))
            {
                return converter;
            }
            
            var genericConverterType = typeof(SpecificBooleanConverter<>);
            var specificConverterType = genericConverterType.MakeGenericType(to);
            var specificConverter = (IConverter)Activator.CreateInstance(specificConverterType, this);
            _compiledConverters[from] = specificConverter;
            return specificConverter;
        }
        
        [Serializable]
        private class SpecificBooleanConverter<T> : IConverter<bool, T>
            where T : Object
        {
            private readonly ReadOnlyBind<Object> _onFalse;
            private readonly ReadOnlyBind<Object> _onTrue;

            public SpecificBooleanConverter(BooleanUnityObjectConverter converter)
            {
                Id = converter.Id;
                Description = converter.Description;
                
                _onFalse = converter.booleanMapping.onFalse;
                _onTrue = converter.booleanMapping.onTrue;
            }

            public string Id { get; }
            public string Description { get; }
            public bool IsSafe => true;

            public T Convert(bool value)
            {
                var result = value ? _onTrue.Value : _onFalse.Value;
                return result as T;
            }

            object IConverter.Convert(object value)
            {
                if (value is bool boolValue)
                {
                    return Convert(boolValue);
                }
                throw new InvalidCastException($"Cannot convert {value.GetType()} to boolean.");
            }
        }

        public bool IsDynamic => booleanMapping.onFalse.IsBound || booleanMapping.onTrue.IsBound;
    }

    [Serializable] internal class BooleanToIntConverter : BooleanConverter<int> { }
    [Serializable] internal class BooleanToStringConverter : BooleanConverter<string> { }
    [Serializable] internal class BooleanToBoolConverter : BooleanConverter<bool> { }
    [Serializable] internal class BooleanToFloatConverter : BooleanConverter<float> { }
    [Serializable] internal class BooleanToDoubleConverter : BooleanConverter<double> { }
    [Serializable] internal class BooleanToLongConverter : BooleanConverter<long> { }
    [Serializable] internal class BooleanToShortConverter : BooleanConverter<short> { }
    [Serializable] internal class BooleanToByteConverter : BooleanConverter<byte> { }
    [Serializable] internal class BooleanToSByteConverter : BooleanConverter<sbyte> { }
    [Serializable] internal class BooleanToCharConverter : BooleanConverter<char> { }
    [Serializable] internal class BooleanToDecimalConverter : BooleanConverter<decimal> { }
    [Serializable] internal class BooleanToVector2Converter : BooleanConverter<Vector2> { }
    [Serializable] internal class BooleanToVector2IntConverter : BooleanConverter<Vector2Int> { }
    [Serializable] internal class BooleanToVector3IntConverter : BooleanConverter<Vector3Int> { }
    [Serializable] internal class BooleanToVector3Converter : BooleanConverter<Vector3> { }
    [Serializable] internal class BooleanToVector4Converter : BooleanConverter<Vector4> { }
    [Serializable] internal class BooleanToColorConverter : BooleanConverter<Color> { }
    [Serializable] internal class BooleanToQuaternionConverter : BooleanConverter<Quaternion> { }
    [Serializable] internal class BooleanToRectConverter : BooleanConverter<Rect> { }
    [Serializable] internal class BooleanToBoundsConverter : BooleanConverter<Bounds> { }
    [Serializable] internal class BooleanToMatrix4x4Converter : BooleanConverter<Matrix4x4> { }
    [Serializable] internal class BooleanToAnimationCurveConverter : BooleanConverter<AnimationCurve> { }
    [Serializable] internal class BooleanToGradientConverter : BooleanConverter<Gradient> { }
}