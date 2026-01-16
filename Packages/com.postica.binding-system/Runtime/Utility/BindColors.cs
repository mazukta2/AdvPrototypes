using System.Runtime.CompilerServices;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Class containing colors used by the binding system.
    /// </summary>
    public static class BindColors
    {
        public static bool IsDarkTheme { get; internal set; } = true;
        
        public static Color Background => IsDarkTheme ? new Color(0.180f, 0.180f, 0.180f) : new Color(0.941f, 0.941f, 0.941f);
        public static Color Debug => IsDarkTheme ? H("#eeae00") : H("#b37600");
        public static Color ConverterUnsafe => IsDarkTheme ? H("#eeae00") : H("#b37600");
        public static Color Primary => IsDarkTheme ? H("#7fdbef") : H("#007acc");
        public static Color Error => IsDarkTheme ? H("#ff6666") : H("#cc0000");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color H(string hex, Color fallback = default)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }
    }
}