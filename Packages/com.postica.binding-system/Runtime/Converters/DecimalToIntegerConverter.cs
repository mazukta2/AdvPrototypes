using System;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// Converts a floating point number into an integer value.
    /// </summary>
    [HideMember]
    [Serializable]
    public class DecimalToIntegerConverter : 
        IConverter<float, short>,
        IConverter<float, ushort>,
        IConverter<float, int>,
        IConverter<float, uint>,
        IConverter<float, ulong>,
        IConverter<float, long>,
        IConverter<float, byte>,
        IConverter<IValueProvider<float>, short>,
        IConverter<IValueProvider<float>, ushort>,
        IConverter<IValueProvider<float>, int>,
        IConverter<IValueProvider<float>, uint>,
        IConverter<IValueProvider<float>, ulong>,
        IConverter<IValueProvider<float>, long>,
        IConverter<IValueProvider<float>, byte>,
        IConverter<double, short>,
        IConverter<double, ushort>,
        IConverter<double, int>,
        IConverter<double, uint>,
        IConverter<double, ulong>,
        IConverter<double, long>,
        IConverter<double, byte>,
        IConverter<IValueProvider<double>, short>,
        IConverter<IValueProvider<double>, ushort>,
        IConverter<IValueProvider<double>, int>,
        IConverter<IValueProvider<double>, uint>,
        IConverter<IValueProvider<double>, ulong>,
        IConverter<IValueProvider<double>, long>,
        IConverter<IValueProvider<double>, byte>,
        IConverter<decimal, short>,
        IConverter<decimal, ushort>,
        IConverter<decimal, int>,
        IConverter<decimal, uint>,
        IConverter<decimal, ulong>,
        IConverter<decimal, long>,
        IConverter<decimal, byte>,
        IConverter<IValueProvider<decimal>, short>,
        IConverter<IValueProvider<decimal>, ushort>,
        IConverter<IValueProvider<decimal>, int>,
        IConverter<IValueProvider<decimal>, uint>,
        IConverter<IValueProvider<decimal>, ulong>,
        IConverter<IValueProvider<decimal>, long>,
        IConverter<IValueProvider<decimal>, byte>
    {
        private enum CastType
        {
            Standard,
            Floor,
            Ceiling,
            Round
        }
        
        [SerializeField]
        [Tooltip("How to convert the values")]
        private CastType _conversion;
        
        public string Id => "Decimal to Integer Converter";

        public string Description => "Converts a floating point value to an integer value";
        
        private long ConvertInner(float value)
        {
            switch (_conversion)
            {
                case CastType.Standard:
                    return (long) value;
                case CastType.Floor:
                    return (long) Mathf.Floor(value);
                case CastType.Ceiling:
                    return (long) Mathf.Ceil(value);
                case CastType.Round:
                    return (long) Mathf.Round(value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private long ConvertInner(double value)
        {
            switch (_conversion)
            {
                case CastType.Standard:
                    return (long) value;
                case CastType.Floor:
                    return (long) Math.Floor(value);
                case CastType.Ceiling:
                    return (long) Math.Ceiling(value);
                case CastType.Round:
                    return (long) Math.Round(value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private long ConvertInner(decimal value)
        {
            switch (_conversion)
            {
                case CastType.Standard:
                    return (long) value;
                case CastType.Floor:
                    return (long) Math.Floor(value);
                case CastType.Ceiling:
                    return (long) Math.Ceiling(value);
                case CastType.Round:
                    return (long) Math.Round(value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        short IConverter<float, short>.Convert(float value) => (short)ConvertInner(value);

        ushort IConverter<float, ushort>.Convert(float value) => (ushort)ConvertInner(value);

        int IConverter<float, int>.Convert(float value) => (int)ConvertInner(value);

        uint IConverter<float, uint>.Convert(float value) => (uint)ConvertInner(value);

        ulong IConverter<float, ulong>.Convert(float value) => (ulong)ConvertInner(value);

        long IConverter<float, long>.Convert(float value) => (long)ConvertInner(value);

        byte IConverter<float, byte>.Convert(float value) => (byte)ConvertInner(value);

        short IConverter<IValueProvider<float>, short>.Convert(IValueProvider<float> value) => (short)ConvertInner(value.Value);

        ushort IConverter<IValueProvider<float>, ushort>.Convert(IValueProvider<float> value) => (ushort)ConvertInner(value.Value);

        int IConverter<IValueProvider<float>, int>.Convert(IValueProvider<float> value) => (int)ConvertInner(value.Value);

        uint IConverter<IValueProvider<float>, uint>.Convert(IValueProvider<float> value) => (uint)ConvertInner(value.Value);

        ulong IConverter<IValueProvider<float>, ulong>.Convert(IValueProvider<float> value) => (ulong)ConvertInner(value.Value);

        long IConverter<IValueProvider<float>, long>.Convert(IValueProvider<float> value) => (long)ConvertInner(value.Value);

        byte IConverter<IValueProvider<float>, byte>.Convert(IValueProvider<float> value) => (byte)ConvertInner(value.Value);

        short IConverter<double, short>.Convert(double value) => (short)ConvertInner(value);

        ushort IConverter<double, ushort>.Convert(double value) => (ushort)ConvertInner(value);

        int IConverter<double, int>.Convert(double value) => (int)ConvertInner(value);

        uint IConverter<double, uint>.Convert(double value) => (uint)ConvertInner(value);

        ulong IConverter<double, ulong>.Convert(double value) => (ulong)ConvertInner(value);

        long IConverter<double, long>.Convert(double value) => (long)ConvertInner(value);

        byte IConverter<double, byte>.Convert(double value) => (byte)ConvertInner(value);

        short IConverter<IValueProvider<double>, short>.Convert(IValueProvider<double> value) => (short)ConvertInner(value.Value);

        ushort IConverter<IValueProvider<double>, ushort>.Convert(IValueProvider<double> value) => (ushort)ConvertInner(value.Value);

        int IConverter<IValueProvider<double>, int>.Convert(IValueProvider<double> value) => (int)ConvertInner(value.Value);

        uint IConverter<IValueProvider<double>, uint>.Convert(IValueProvider<double> value) => (uint)ConvertInner(value.Value);

        ulong IConverter<IValueProvider<double>, ulong>.Convert(IValueProvider<double> value) => (ulong)ConvertInner(value.Value);

        long IConverter<IValueProvider<double>, long>.Convert(IValueProvider<double> value) => (long)ConvertInner(value.Value);

        byte IConverter<IValueProvider<double>, byte>.Convert(IValueProvider<double> value) => (byte)ConvertInner(value.Value);

        short IConverter<decimal, short>.Convert(decimal value) => (short)ConvertInner(value);

        ushort IConverter<decimal, ushort>.Convert(decimal value) => (ushort)ConvertInner(value);

        int IConverter<decimal, int>.Convert(decimal value) => (int)ConvertInner(value);

        uint IConverter<decimal, uint>.Convert(decimal value) => (uint)ConvertInner(value);

        ulong IConverter<decimal, ulong>.Convert(decimal value) => (ulong)ConvertInner(value);

        long IConverter<decimal, long>.Convert(decimal value) => (long)ConvertInner(value);

        byte IConverter<decimal, byte>.Convert(decimal value) => (byte)ConvertInner(value);

        short IConverter<IValueProvider<decimal>, short>.Convert(IValueProvider<decimal> value) => (short)ConvertInner(value.Value);

        ushort IConverter<IValueProvider<decimal>, ushort>.Convert(IValueProvider<decimal> value) => (ushort)ConvertInner(value.Value);

        int IConverter<IValueProvider<decimal>, int>.Convert(IValueProvider<decimal> value) => (int)ConvertInner(value.Value);

        uint IConverter<IValueProvider<decimal>, uint>.Convert(IValueProvider<decimal> value) => (uint)ConvertInner(value.Value);

        ulong IConverter<IValueProvider<decimal>, ulong>.Convert(IValueProvider<decimal> value) => (ulong)ConvertInner(value.Value);

        long IConverter<IValueProvider<decimal>, long>.Convert(IValueProvider<decimal> value) => (long)ConvertInner(value.Value);

        byte IConverter<IValueProvider<decimal>, byte>.Convert(IValueProvider<decimal> value) => (byte)ConvertInner(value.Value);
        
        public object Convert(object value) => value is float f ? ConvertInner(f)
                                            : value is double d ? ConvertInner(d)
                                            : ConvertInner((decimal)value);

        public bool IsSafe => true;
        
        
    }
}