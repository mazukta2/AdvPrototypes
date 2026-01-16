using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Clamps the input numeric value between min and max;
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class ClampValueModifier : NumericModifier
    {
        [SerializeField]
        public ReadOnlyBind<float> min = 0f.Bind();
        [SerializeField]
        public ReadOnlyBind<float> max = 1f.Bind();

        ///<inheritdoc/>
        public override string Id => "Clamp Value";
        ///<inheritdoc/>
        public override string ShortDataDescription => $"[{(min.BindData.HasValue ? VarFormat("min") : min.Value.ToString())}" +
                                                       $", {(max.BindData.HasValue ? VarFormat("max") : max.Value.ToString())}]";

        protected override long Modify(long value) => Mathf.Clamp((int)value, (int)min, (int)max);

        protected override double Modify(double value) => Mathf.Clamp((float)value, min, max);
    }
}