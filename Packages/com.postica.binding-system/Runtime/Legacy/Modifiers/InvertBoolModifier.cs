using System;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier which negates a boolean value.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class InvertBoolModifier : BaseModifier<bool>
    {
        ///<inheritdoc/>
        public override string Id => "Invert Boolean";

        ///<inheritdoc/>
        public override string ShortDataDescription => "";

        protected override bool Modify(bool value) => !value;
        protected override bool InverseModify(bool output) => !output;
    }
}