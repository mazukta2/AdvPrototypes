using System;
using Postica.Common;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// A modifier which negates a boolean value.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeDescription("Negates the boolean value.")]
    public sealed class InvertBoolModifier : BaseModifier<bool>
    {
        ///<inheritdoc/>
        public override string Id => "Invert Boolean";

        ///<inheritdoc/>
        public override string ShortDataDescription => "";

        protected override bool Modify(bool value) => !value;
        protected override bool InverseModify(bool output) => !output;
    }
}