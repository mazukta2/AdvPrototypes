using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// A converter which transforms any given value into a string.
    /// </summary>
    [Serializable]
    [HideMember]
    public class CharArrayToStringConverter : 
        IConverter<char[], string>,
        IShortCircuitConverter
    {
        [Tooltip("When true, the converter will use TextMeshPro's char[] instead of string. " +
                 "This is useful for performance when working with TextMeshPro components.")]
        public bool optimizeForTextMeshPro = true;
        
        private TMP_Text _textMeshPro;

        public bool HasShortCircuited { get; set; }
        
        /// <inheritdoc/>
        public string Id { get; } = "Char Array to String";

        /// <inheritdoc/>
        public string Description { get; } = "Converts a char array value to a string. If the context is a TextMeshPro, then the char array will be used instead of string.";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public string Convert(char[] value)
        {
            if (optimizeForTextMeshPro && _textMeshPro)
            {
                _textMeshPro.SetText(value);
                return null;
            }

            return new string(value);
        }

        /// <inheritdoc/>
        object IConverter.Convert(object value)
        {
            return Convert(value as char[]);
        }

        public void SetTarget(object target, Type targetType, string path)
        {
            if(target is TMP_Text textMeshPro)
            {
                _textMeshPro = textMeshPro;
                HasShortCircuited = optimizeForTextMeshPro;
            }
        }
    }
    
    /// <summary>
    /// A converter which transforms any given value into a string.
    /// </summary>
    [Serializable]
    [HideMember]
    public class StringToCharArrayConverter : 
        IConverter<string, char[]>
    {
        [NonSerialized]
        private string _previousString;
        [NonSerialized]
        private char[] _cachedCharArray;
        
        /// <inheritdoc/>
        public string Id { get; } = "String to Char Array";

        /// <inheritdoc/>
        public string Description { get; } = "Converts a string to a char array value.";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public char[] Convert(string value)
        {
            if (value == _previousString && _cachedCharArray != null)
            {
                return _cachedCharArray;
            }
            _previousString = value;
            return _cachedCharArray = value.ToCharArray();
        }

        /// <inheritdoc/>
        object IConverter.Convert(object value)
        {
            return Convert(value?.ToString());
        }
    }
}
