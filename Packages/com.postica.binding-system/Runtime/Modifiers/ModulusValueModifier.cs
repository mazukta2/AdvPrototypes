using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Returns the modulus of the input numeric value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeDescription("Returns the modulus of the input numeric value.")]
    public sealed class ModulusValueModifier : NumericModifier, IDynamicComponent
    {
        [SerializeField]
        public ReadOnlyBind<float> modulus = 1f.Bind();

        ///<inheritdoc/>
        public override string Id => "Modulus Value";

        ///<inheritdoc/>
        public override string ShortDataDescription => $" % {(modulus.BindData.HasValue ? VarFormat("modulus") : modulus.Value.ToString())}";

        protected override double Modify(double value) => value % modulus.Value;
        protected override long Modify(long value) => value % (long)modulus.Value;

        public bool IsDynamic => modulus.IsBound;
    }
}