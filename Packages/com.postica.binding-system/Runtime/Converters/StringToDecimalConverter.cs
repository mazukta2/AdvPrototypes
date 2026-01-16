using System;
using System.Globalization;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    [Serializable]
    internal sealed class StringToFloatConverter : StringToDecimalConverter<float>
    {
        protected override bool TryParse(string value, out float result) => float.TryParse(value, out result);
    }
    
    [Serializable]
    internal sealed class StringToDoubleConverter : StringToDecimalConverter<double>
    {
        protected override bool TryParse(string value, out double result) => double.TryParse(value, out result);
    }
    
    [Serializable]
    internal sealed class StringToDecimalConverter : StringToDecimalConverter<decimal>
    {
        protected override bool TryParse(string value, out decimal result) => decimal.TryParse(value, out result);
    }
    
    /// <summary>
    /// This class converts a string to decimal number value
    /// </summary>
    [Serializable]
    [HideMember]
    internal abstract class StringToDecimalConverter<T> : 
        IConverter<string, T>, IPeerConverter, IDynamicComponent where T : struct
    {
        [Tooltip("The value to use when the input is invalid")]
        public ReadOnlyBind<T> fallbackValue;
        [Tooltip("Allow empty string to be converted to fallback value, otherwise in case of both directions, it will get back an empty string.")]
        public bool fallbackOnEmpty = true;

        private IDecimalToStringConverter _decimalToStringConverter;

        public string Id => "String to Decimal";

        public string Description => "Converts number string representation into a decimal value.";

        public bool IsSafe => false;
        
        public bool IsDynamic => fallbackValue.IsBound;
        
        protected abstract bool TryParse(string value, out T result);

        public object Convert(object value)
        {
            return Convert(value?.ToString());
        }

        public T Convert(string value)
        {
            if (_decimalToStringConverter != null)
            {
                _decimalToStringConverter.AddDecimalSeparator = value.EndsWith(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
                _decimalToStringConverter.IsEmptyString = !fallbackOnEmpty && string.IsNullOrEmpty(value);
            }
            if (string.IsNullOrEmpty(value))
            {
                return fallbackValue;
            }
            return TryParse(value, out var result) ? result : fallbackValue;
        }

        public IConverter OtherConverter
        {
            get => _decimalToStringConverter as IConverter;
            set => _decimalToStringConverter = value as IDecimalToStringConverter;
        }
    } 
}
