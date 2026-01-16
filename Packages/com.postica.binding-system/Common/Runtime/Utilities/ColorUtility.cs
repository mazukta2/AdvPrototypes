using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.Common
{
    /// <summary>
    /// Provides multiple additional utility functions to <see cref="Color"/> class.
    /// </summary>
    internal static class ColorUtility
    {
        /// <summary>
        /// Changes the Red channel value and returns the color
        /// </summary>
        /// <param name="color">The color to change the channel from</param>
        /// <param name="red">The value of the channel to change. Should be [0, 1]</param>
        /// <returns>A new color with changed channel value</returns>
        public static Color Red(this Color color, float red)
        {
            color.r = red;
            return color;
        }

        /// <summary>
        /// Changes the Green channel value and returns the color
        /// </summary>
        /// <param name="color">The color to change the channel from</param>
        /// <param name="green">The value of the channel to change. Should be [0, 1]</param>
        /// <returns>A new color with changed channel value</returns>
        public static Color Green(this Color color, float green)
        {
            color.g = green;
            return color;
        }

        /// <summary>
        /// Changes the Blue channel value and returns the color
        /// </summary>
        /// <param name="color">The color to change the channel from</param>
        /// <param name="blue">The value of the channel to change. Should be [0, 1]</param>
        /// <returns>A new color with changed channel value</returns>
        public static Color Blue(this Color color, float blue)
        {
            color.b = blue;
            return color;
        }

        /// <summary>
        /// Changes the Alpha channel value and returns the color
        /// </summary>
        /// <param name="color">The color to change the channel from</param>
        /// <param name="alpha">The value of the channel to change. Should be [0, 1]</param>
        /// <returns>A new color with changed channel value</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Adapts the channels of the color to be compatible with the specified <see cref="TextureFormat"/>
        /// </summary>
        /// <remarks>Some formats require more advanced channels manipulation, this is why not every format is supported.</remarks>
        /// <param name="color">The color to adapt</param>
        /// <param name="format">The <see cref="TextureFormat"/> to adapt the color to</param>
        /// <returns>A new color which is compatible with the format</returns>
        public static Color AdaptFormat(this Color color, TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32: return new Color(color.a, color.r, color.g, color.b);
                case TextureFormat.ARGB4444: return new Color(color.a, color.r, color.g, color.b);
                case TextureFormat.BGRA32: return new Color(color.b, color.g, color.r, color.b);
                case TextureFormat.R16: 
                case TextureFormat.R8: return new Color(color.r, 0, 0, 1);
                case TextureFormat.RGB24: return new Color(color.r, color.g, color.b, 1);
                case TextureFormat.RGBA32: return new Color(color.r, color.g, color.b, color.a);

                default: return color;
            }
        }
    }

}
