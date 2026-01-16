using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// A converter which transforms any given value into a string using the specified format.
    /// </summary>
    [Serializable]
    [HideMember]
    public class StringBuilderToStringConverter : 
        IConverter<StringBuilder, string>,
        IShortCircuitConverter
    {
        [Tooltip("When true, the converter will use TextMeshPro's StringBuilder instead of string. " +
                 "This is useful for performance when working with TextMeshPro components.")]
        public bool optimizeForTextMeshPro = true;
        
        private TMP_Text _textMeshPro;
        
        public bool HasShortCircuited { get; set; }
        
        /// <inheritdoc/>
        public string Id { get; } = "StringBuilder to String";

        /// <inheritdoc/>
        public string Description { get; } = "Converts a StringBuilder value to a string. If the context is a TextMeshPro, then the StringBuilder will be used instead of string.";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public string Convert(StringBuilder value)
        {
            if (optimizeForTextMeshPro && _textMeshPro)
            {
                _textMeshPro.SetText(value);
                return null;
            }
            
            return value?.ToString();
        }

        /// <inheritdoc/>
        object IConverter.Convert(object value)
        {
            return Convert(value as StringBuilder);
        }

        public void SetTarget(object target, Type targetType, string path)
        {
            if(target is TMP_Text textMeshPro)
            {
                HasShortCircuited = optimizeForTextMeshPro;
                _textMeshPro = textMeshPro;
            }
        }
    }
}
