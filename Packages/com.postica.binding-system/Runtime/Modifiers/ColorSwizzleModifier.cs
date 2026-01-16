using System;
using Postica.BindingSystem.Utility;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// A modifier which swizzles (mixes) color channels.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/color-swizzle")]
    [TypeDescription("Swizzles (mixes) the color channels.")]
    public sealed class ColorSwizzleModifier : BaseModifier<Color>, IDynamicComponent
    {
        [Tooltip("The swizzle to apply to the color.")]
        public SwizzleColor swizzle;
        
        ///<inheritdoc/>
        public override string Id => "Swizzle Color";

        ///<inheritdoc/>
        public override string ShortDataDescription => swizzle.ToColoredString();

        protected override Color Modify(Color value) => swizzle.Swizzle(value);
        
        protected override Color InverseModify(Color output) => swizzle.InverseSwizzle(output);

        public bool IsDynamic => false;
    }
}