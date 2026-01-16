using System;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier which return the absolute value of the input numeric value.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class AbsoluteValueModifier : NumericModifier
    {
        /// <inheritdoc/>
        public override string Id => "Absolute Value";

        /// <inheritdoc/>
        protected override long Modify(long value) => value < 0 ? -value : value;

        /// <inheritdoc/>
        protected override double Modify(double value) => value < 0 ? -value : value;
    }
}