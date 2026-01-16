using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Converters
{
    internal interface IDecimalToStringConverter
    {
        bool AddDecimalSeparator { get; set; }
        bool IsEmptyString { get; set; }
    }
    
    /// <summary>
    /// This class contains all <see cref="FormatStringConverter{T}"/> converters for default types.
    /// </summary>
    public static class FormatStringConverters
    {
        [Serializable] private sealed class FormatStringConverter_Byte     : FormatStringConverter<byte> { }
        [Serializable] private sealed class FormatStringConverter_Ushort   : FormatStringConverter<ushort> { }
        [Serializable] private sealed class FormatStringConverter_Short    : FormatStringConverter<short> { }
        [Serializable] private sealed class FormatStringConverter_Uint     : FormatStringConverter<uint> { }
        [Serializable] private sealed class FormatStringConverter_Int      : FormatStringConverter<int> { }
        [Serializable] private sealed class FormatStringConverter_Ulong    : FormatStringConverter<ulong> { }
        [Serializable] private sealed class FormatStringConverter_Long     : FormatStringConverter<long> { }

        [Serializable]
        private sealed class FormatStringConverter_Float : FormatStringConverter<float>, IDecimalToStringConverter
        {
            public bool AddDecimalSeparator { get; set; }
            public bool IsEmptyString { get; set; }
            
            public override string Convert(float value)
            {
                if(value == 0 && IsEmptyString)
                {
                    return string.Empty;
                }
                return AddDecimalSeparator ? base.Convert(value) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator : base.Convert(value);
            }
        }

        [Serializable]
        private sealed class FormatStringConverter_Double : FormatStringConverter<double>, IDecimalToStringConverter
        {
            public bool AddDecimalSeparator { get; set; }
            public bool IsEmptyString { get; set; }
            public override string Convert(double value)
            {
                if(value == 0 && IsEmptyString)
                {
                    return string.Empty;
                }
                return AddDecimalSeparator ? base.Convert(value) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator : base.Convert(value);
            }
        }
        
        [Serializable]
        private sealed class FormatStringConverter_Decimal : FormatStringConverter<decimal>, IDecimalToStringConverter
        {
            public bool AddDecimalSeparator { get; set; }
            public bool IsEmptyString { get; set; }
            public override string Convert(decimal value)
            {
                if(value == 0 && IsEmptyString)
                {
                    return string.Empty;
                }
                return AddDecimalSeparator ? base.Convert(value) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator : base.Convert(value);
            }
        }
        
        [Serializable] private sealed class FormatStringConverter_Vector2   : FormatStringConverter<Vector2> { }
        [Serializable] private sealed class FormatStringConverter_Vector2I   : FormatStringConverter<Vector2Int> { }
        [Serializable] private sealed class FormatStringConverter_Vector3   : FormatStringConverter<Vector3> { }
        [Serializable] private sealed class FormatStringConverter_Vector3I   : FormatStringConverter<Vector3Int> { }
        [Serializable] private sealed class FormatStringConverter_Vector4   : FormatStringConverter<Vector4> { }
        [Serializable] private sealed class FormatStringConverter_Color        : FormatStringConverter<Color> { }
        [Serializable] private sealed class FormatStringConverter_Color32   : FormatStringConverter<Color32> { }
        [Serializable] private sealed class FormatStringConverter_Rect         : FormatStringConverter<Rect> { }
        [Serializable] private sealed class FormatStringConverter_RectInt   : FormatStringConverter<RectInt> { }
        [Serializable] private sealed class FormatStringConverter_Bounds       : FormatStringConverter<Bounds> { }
        [Serializable] private sealed class FormatStringConverter_BoundsInt   : FormatStringConverter<BoundsInt> { }
        [Serializable] private sealed class FormatStringConverter_DateTime   : FormatStringConverter<DateTime> { }
        [Serializable] private sealed class FormatStringConverter_TimeSpan   : FormatStringConverter<TimeSpan> { }

        /// <summary>
        /// Registers all defined converters
        /// </summary>
        public static void RegisterDefaultTypes()
        {
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Byte>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Ushort>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Short>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Uint>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Int>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Ulong>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Long>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Float>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Double>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Decimal>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Vector2>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Vector2I>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Vector3>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Vector3I>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Vector4>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Color>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Color32>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Rect>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_RectInt>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_Bounds>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_BoundsInt>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_DateTime>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter_TimeSpan>();
        }
    }

    /// <summary>
    /// A converter which transforms a given <typeparamref name="T"/> value into a string using the specified format.
    /// </summary>
    /// <typeparam name="T">The type of the value to be formatted</typeparam>
    [Serializable]
    [HideMember]
    public class FormatStringConverter<T> : IConverter<T, string> where T : IFormattable
    {
        [FormerlySerializedAs("_format")] [SerializeField]
        protected string _valueFormat;
        [SerializeField]
        protected bool _invariantCulture;
        
        protected T _prevValue;
        [NonSerialized]
        protected string _cacheOutput;

        /// <summary>
        /// The format to use for this converter.
        /// </summary>
        /// <remarks>The format should be compatible with <see cref="IFormattable"/> <typeparamref name="T"/> type</remarks>
        public string Format { get => _valueFormat; set => _valueFormat = value; }

        /// <inheritdoc/>
        public string Id { get; } = "Format String";

        /// <inheritdoc/>
        public string Description { get; } = "Formats a value into a string with specified format." +
            "\nThe format is passed as a parameter to the 'ToString()' method, and not as a format to string.Format() method";
        
        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public virtual string Convert(T value)
        {
            return string.IsNullOrEmpty(_valueFormat)
                ? value.ToString()
                : ConvertCached(value);
        }

        /// <inheritdoc/>
        public object Convert(object value)
        {
            if (string.IsNullOrEmpty(_valueFormat) && value is IFormattable formattable)
            {
                return ConvertCached(formattable);
            }
            return value?.ToString();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ConvertCached(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_prevValue, value) && _cacheOutput != null)
            {
                return _cacheOutput;
            }
            _prevValue = value;
            return _cacheOutput = value.ToString(_valueFormat, _invariantCulture ? NumberFormatInfo.InvariantInfo : NumberFormatInfo.CurrentInfo);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ConvertCached(IFormattable value)
        {
            if (_prevValue?.Equals(value) == true && _cacheOutput != null)
            {
                return _cacheOutput;
            }
            _prevValue = (T)value;
            return _cacheOutput = value.ToString(_valueFormat, _invariantCulture ? NumberFormatInfo.InvariantInfo : NumberFormatInfo.CurrentInfo);
        }
    }

    /// <summary>
    /// A converter which transforms any given value into a string using the specified format.
    /// </summary>
    [Serializable]
    [HideMember]
    public class FormatStringConverter : IConverter<object, string>
    {
        [SerializeField]
        protected string _format;
        [SerializeField]
        protected bool _invariantCulture;

        /// <summary>
        /// The format to use for this converter in case the value is of <see cref="IFormattable"/> type.
        /// </summary>
        /// <remarks>The format should be compatible with <see cref="IFormattable"/> type</remarks>
        public string Format { get => _format; set => _format = value; }

        /// <inheritdoc/>
        public string Id { get; } = "Format String";

        /// <inheritdoc/>
        public string Description { get; } = "Formats a value into a string with specified format." +
            "\nThe format is passed as a parameter to the 'ToString()' method, and not as a format to string.Format() method";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public object Convert(object value)
        {
            if(!string.IsNullOrEmpty(_format) && value is IFormattable formattable)
            {
                return formattable.ToString(_format, _invariantCulture ? NumberFormatInfo.InvariantInfo : NumberFormatInfo.CurrentInfo);
            }
            return value?.ToString();
        }

        /// <inheritdoc/>
        string IConverter<object, string>.Convert(object value)
        {
            if (!string.IsNullOrEmpty(_format) && value is System.IFormattable formattable)
            {
                return formattable.ToString(_format, _invariantCulture ? NumberFormatInfo.InvariantInfo : NumberFormatInfo.CurrentInfo);
            }
            return value?.ToString();
        }
    }
}
