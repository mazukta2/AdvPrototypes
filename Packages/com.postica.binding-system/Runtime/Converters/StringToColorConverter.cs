using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// This class converts a string to Color value, either by color name, RGBA(red, green, blue, alpha) values, HEX or HTML value
    /// </summary>
    [Serializable]
    public class StringToColorConverter : IConverter<string, Color>
    {
        private const string colorPattern = @"RGBA\((\d*\.?\d*) *\, *(\d*\.?\d*) *\, *(\d*\.?\d*) *\, *(\d*\.?\d*) *\)";
        private static Regex colorRegex;
        
        private string _prevInput;
        private Color _cacheOutput;

        // These fields will be available to the user to change
        [Tooltip("The color to use when the input is invalid")]
        public ReadOnlyBind<Color> fallbackColor = Color.magenta.Bind();

        // This id is used in Change Converter menu and when displaying the converter
        public string Id => "String to Color";

        // The description is shown when hovering the converter in the Inspector
        public string Description => "Converts color string representation into color. \n" +
                                     "Supports the following formats:\n" +
                                     " - Color names: red, White, BLUE, etc.\n" +
                                     " - HEX values: 33BBEF, #2e34f3\n" +
                                     " - RGBA: RGBA(0.14, 0.94, 0.65, 0.5)";

        // If true, the converter always returns a valid value
        // In this case, the input string may not be a valid color value.
        public bool IsSafe => false;

        public object Convert(object value)
        {
            return Convert(value?.ToString());
        }

        public Color Convert(string value)
        {
            if (fallbackColor.IsBound)
            {
                return ConvertPure(value);
            }
            
            if(_prevInput?.Equals(value, StringComparison.Ordinal) == true)
            {
                return _cacheOutput;
            }
            _prevInput = value;
            return _cacheOutput = ConvertPure(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Color ConvertPure(string value)
        {
            return TryConvertFromKnownColorNames(value, out var color)
                                  || TryConvertFromHex(value, out color)
                                  || TryConvertWithRegex(value, out color)
                ? color
                : fallbackColor.Value;
        }

        private bool TryConvertFromKnownColorNames(string value, out Color color)
        {
            var parsedColor = value.Trim().ToLower() switch
            {
                nameof(Color.white) => Color.white,
                nameof(Color.red) => Color.red,
                nameof(Color.blue) => Color.blue,
                nameof(Color.green) => Color.green,
                nameof(Color.yellow) => Color.yellow,
                nameof(Color.cyan) => Color.cyan,
                nameof(Color.black) => Color.black,
                nameof(Color.magenta) => Color.magenta,
                nameof(Color.gray) => Color.gray,
                "transparent" => Color.clear,
                _ => (Color?)null,
            };
            
            if (parsedColor.HasValue)
            {
                color = parsedColor.Value;
                return true;
            }
            color = Color.clear;
            return false;
        }

        private bool TryConvertWithRegex(string value, out Color color)
        {
            colorRegex ??= new Regex(colorPattern);

            var match = colorRegex.Match(value.ToUpper());
            if (!match.Success)
            {
                color = Color.clear;
                return false;
            }
            color = new Color(float.Parse(match.Groups[1].Value),
                             float.Parse(match.Groups[2].Value),
                             float.Parse(match.Groups[3].Value),
                              float.Parse(match.Groups[4].Value));
            return true;
        }
        
        private bool TryConvertFromHex(string value, out Color color)
        {
            if (ColorUtility.TryParseHtmlString(value.TrimStart('#'), out var parsedColor))
            {
                color = parsedColor;
                return true;
            }
            color = Color.clear;
            return false;
        }
    } 
}
