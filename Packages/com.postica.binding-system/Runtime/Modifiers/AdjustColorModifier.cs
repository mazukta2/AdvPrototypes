using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This modifier serves as an example how a bound Color can be modified.
    /// </summary>
    // The System registers automatically this modifier.
    [HideMember]
    [TypeIcon("_bsicons/modifiers/adjust")]
    [TypeDescription("Adjusts the brightness, hue and saturation.")]
    public class AdjustColorModifier : BaseModifier<Color>
    {
        [Range(-1f, 1f)]
        public float brightness;
        [Range(-1f, 1f)]
        public float hue;
        [Range(-1f, 1f)]
        public float saturation;

        // The Id is shown in Bind Menu and in Bind drawer
        public override string Id => "Adjust Color";

        // This description is used when the modifier is collapsed
        public override string ShortDataDescription => $"[ B:{brightness:0.0}, H:{hue:0.0}, S:{saturation:0.0} ]";

        // The modify method is called when reading a value or when writing when BindMode is set to WriteOnly
        protected override Color Modify(Color value)
        {
            // Apply brightness
            value = ApplyBrightness(value, brightness);

            // Apply hue
            return ApplyHue(value, hue, saturation);
        }

        // This method is only called when writing the value when BindMode is set to ReadWrite
        protected override Color InverseModify(Color output)
        {
            // Apply hue
            var value = ApplyHue(output, -hue, -saturation);

            // Apply brightness
            return ApplyBrightness(value, -brightness);
        }

        private static Color ApplyHue(Color value, float hue, float saturation)
        {
            var argMax = 0;
            var min = float.MaxValue;
            var max = float.MinValue;

            for (int i = 0; i < 3; i++)
            {
                if(value[i] < min)
                {
                    min = value[i];
                }
                if(value[i] > max)
                {
                    max = value[i];
                    argMax = i;
                }
            }

            // If Red is max, then Hue = (G - B) / (max - min)
            // If Green is max, then Hue = 2.0 + (B - R) / (max - min)
            // If Blue is max, then Hue = 4.0 + (R - G) / (max - min)

            // Hue
            var H = argMax switch
            {
                0 => (value.g - value.b) / (max - min),
                1 => 2f + (value.b - value.r) / (max - min),
                2 => 4f + (value.r - value.g) / (max - min),
                _ => 0
            };

            H *= 60;
            if(H < 0)
            {
                H += 360;
            }

            // Saturation
            var S = max == 0 ? 0 : (max - min) / max;
            // Value
            var V = max;

            // Apply delta Hue
            H *= 1 + hue;
            if(H > 360)
            {
                H -= 360;
            }
            else if(H < 0)
            {
                H += 360;
            }

            // Apply delta saturation
            S = Mathf.Clamp01(S + saturation);

            // Convert back to RGB
            var C = V * S;
            var X = C * (1 - Mathf.Abs((H / 60) % 2 - 1));
            var m = V - C;

            var (r, g, b) = H switch
            {
                >= 0 and < 60       => (C, X, 0),
                >= 60 and < 120     => (X, C, 0),
                >= 120 and < 180    => (0, C, X),
                >= 180 and < 240    => (0, X, C),
                >= 240 and < 300    => (X, 0, C),
                >= 300 and < 360    => (C, 0, X),
                _ => (C, X, 0f)
            };

            value.r = Mathf.Clamp01(r + m);
            value.g = Mathf.Clamp01(g + m);
            value.b = Mathf.Clamp01(b + m);

            return value;
        }

        private static Color ApplyBrightness(Color value, float brightness)
        {
            value.r = Mathf.Clamp01(value.r + brightness);
            value.g = Mathf.Clamp01(value.g + brightness);
            value.b = Mathf.Clamp01(value.b + brightness);
            return value;
        }
    } 
}