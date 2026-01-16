using UnityEngine;


namespace Postica.BindingSystem
{
    /// <summary>
    /// This converter will convert the input string as HEX value to a color
    /// </summary>
    // The System registers automatically the converter.
    public class HexColorConverter : IConverter<string, Color>
    {
        // This id is used in Change Converter menu and when displaying the converter
        public string Id => "HEX Color Converter";

        // The description is shown when hovering the converter in the Inspector
        public string Description => "Converts a color in HEX format into color";

        // If true, the converter always returns a valid value
        // In this case, the input string may not be a valid HEX value
        public bool IsSafe => false;

        // This method is used during runtime
        public Color Convert(string value)
        {
            return UnityEngine.ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.clear;
        }

        // This method is used mostly when debugging and when Safe Mode is activated
        public object Convert(object value)
        {
            return Convert(value?.ToString());
        }
    } 
}
