using System;
using Postica.Common;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// A modifier which return the absolute value of the input numeric value.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeDescription("Returns the absolute value of the input numeric value.")]
    public sealed class AbsoluteValueModifier : NumericModifier
    {
        /// <inheritdoc/>
        public override string Id => "Absolute Value";

        /// <inheritdoc/>
        protected override long Modify(long value) => value < 0 ? -value : value;

        /// <inheritdoc/>
        protected override double Modify(double value) => value < 0 ? -value : value;
    }
}