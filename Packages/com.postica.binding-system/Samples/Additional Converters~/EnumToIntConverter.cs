using System;
using UnityEngine;


namespace Postica.BindingSystem
{
    /// <summary>
    /// This converter will convert the input string as HEX value to a color
    /// </summary>
    // The System registers automatically the converter.
    public class EnumToIntConverter : IConverter<Enum, int>
    {
        // This id is used in Change Converter menu and when displaying the converter
        public string Id => "Enum to Int Converter";

        // The description is shown when hovering the converter in the Inspector
        public string Description => "Converts an enum into a numeric value";

        // If true, the converter always returns a valid value
        // In this case, the input string may not be a valid HEX value
        public bool IsSafe => true;

        // This method is used during runtime
        public int Convert(Enum value)
        {
            return System.Convert.ToInt32(value);
        }

        // This method is used mostly when debugging and when Safe Mode is activated
        public object Convert(object value)
        {
            return System.Convert.ToInt32(value);
        }
    }
}
