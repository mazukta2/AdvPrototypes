using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// A modifier which multiplies each component of a Color.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/tint")]
    [TypeDescription("Multiplies each component of the color with another color.")]
    public sealed class TintColorModifier : BaseModifier<Color>, IDynamicComponent
    {
        [Tooltip("The tint to apply to the color.")]
        public ReadOnlyBind<Color> tint = Color.white.Bind();
        
        ///<inheritdoc/>
        public override string Id => "Tint Color";

        ///<inheritdoc/>
        public override string ShortDataDescription => "";

        protected override Color Modify(Color value)
        {
            return value * tint.Value;
        }
        
        protected override Color InverseModify(Color output)
        {
            var divider = tint.Value;
            
            return new Color(Divide(output.r, divider.r), 
                Divide(output.g, divider.g), 
                Divide(output.b, divider.b), 
                Divide(output.a, divider.a));
            
            float Divide(float value, float dividendValue)
            {
                if (Math.Abs(dividendValue) < float.Epsilon)
                {
                    return 0;
                }

                return value / dividendValue;
            }
        }

        public bool IsDynamic => tint.IsBound;
    }
}