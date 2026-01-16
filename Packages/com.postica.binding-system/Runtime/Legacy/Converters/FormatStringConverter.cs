using System;
using System.Globalization;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class contains all <see cref="FormatStringConverter{T}"/> converters for default types.
    /// </summary>
    internal static class FormatStringConverters
    {
        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Byte : FormatStringConverter<byte>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Ushort : FormatStringConverter<ushort>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Short : FormatStringConverter<short>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Uint : FormatStringConverter<uint>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Int : FormatStringConverter<int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Ulong : FormatStringConverter<ulong>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Long : FormatStringConverter<long>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Float : FormatStringConverter<float>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Double : FormatStringConverter<double>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Vector2 : FormatStringConverter<Vector2>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Vector2I : FormatStringConverter<Vector2Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Vector3 : FormatStringConverter<Vector3>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Vector3I : FormatStringConverter<Vector3Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Vector4 : FormatStringConverter<Vector4>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Color : FormatStringConverter<Color>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Color32 : FormatStringConverter<Color32>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Rect : FormatStringConverter<Rect>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_RectInt : FormatStringConverter<RectInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_Bounds : FormatStringConverter<Bounds>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_BoundsInt : FormatStringConverter<BoundsInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_DateTime : FormatStringConverter<DateTime>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FormatStringConverter_TimeSpan : FormatStringConverter<TimeSpan>
        {
        }
    }

    /// <summary>
    /// A converter which transforms a given <typeparamref name="T"/> value into a string using the specified format.
    /// </summary>
    /// <typeparam name="T">The type of the value to be formatted</typeparam>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class FormatStringConverter<T> : IConverter<T, string> where T : IFormattable
    {
        [SerializeField] protected string _format;

        /// <summary>
        /// The format to use for this converter.
        /// </summary>
        /// <remarks>The format should be compatible with <see cref="IFormattable"/> <typeparamref name="T"/> type</remarks>
        public string Format
        {
            get => _format;
            set => _format = value;
        }

        /// <inheritdoc/>
        public string Id { get; } = "Format String";

        /// <inheritdoc/>
        public string Description { get; } = "Formats a value into a string with specified format." +
                                             "\nThe format is passed as a parameter to the 'ToString()' method, and not as a format to string.Format() method";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public string Convert(T value) => string.IsNullOrEmpty(_format)
            ? value.ToString()
            : value.ToString(_format, NumberFormatInfo.CurrentInfo);

        /// <inheritdoc/>
        public object Convert(object value)
        {
            if (!string.IsNullOrEmpty(_format) && value is IFormattable formattable)
            {
                return formattable.ToString(_format, NumberFormatInfo.CurrentInfo);
            }

            return value?.ToString();
        }
    }

    /// <summary>
    /// A converter which transforms any given value into a string using the specified format.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class FormatStringConverter : IConverter<object, string>
    {
        [SerializeField] protected string _format;

        /// <summary>
        /// The format to use for this converter in case the value is of <see cref="IFormattable"/> type.
        /// </summary>
        /// <remarks>The format should be compatible with <see cref="IFormattable"/> type</remarks>
        public string Format
        {
            get => _format;
            set => _format = value;
        }

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
            if (!string.IsNullOrEmpty(_format) && value is IFormattable formattable)
            {
                return formattable.ToString(_format, NumberFormatInfo.CurrentInfo);
            }

            return value?.ToString();
        }

        /// <inheritdoc/>
        string IConverter<object, string>.Convert(object value)
        {
            if (!string.IsNullOrEmpty(_format) && value is System.IFormattable formattable)
            {
                return formattable.ToString(_format, NumberFormatInfo.CurrentInfo);
            }

            return value?.ToString();
        }
    }
}