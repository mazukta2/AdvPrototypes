using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Returns the modulus of the input numeric value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class ModulusValueModifier : NumericModifier
    {
        [SerializeField]
        public ReadOnlyBind<float> modulus = 1f.Bind();

        ///<inheritdoc/>
        public override string Id => "Modulus Value";

        ///<inheritdoc/>
        public override string ShortDataDescription => $" % {(modulus.BindData.HasValue ? VarFormat("modulus") : modulus.Value.ToString())}";

        protected override double Modify(double value) => value % modulus.Value;
        protected override long Modify(long value) => value % (long)modulus.Value;
    }
}