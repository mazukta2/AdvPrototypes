using System;
using System.Runtime.CompilerServices;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Normalizes the input float value between min and max.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class NormalizeValueModifier : BaseModifier<float>, IReadWriteModifier<double>
    {
        [SerializeField]
        public ReadOnlyBind<float> min = 0f.Bind();
        [SerializeField]
        public ReadOnlyBind<float> max = 1f.Bind();

        ///<inheritdoc/>
        public override string Id => "Normalize Value";

        ///<inheritdoc/>
        public override string ShortDataDescription => $"[{(min.BindData.HasValue ? VarFormat("min") : min.Value.ToString())}, {(max.BindData.HasValue ? VarFormat("max") : max.Value.ToString())}]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Modify(float value) => Mathf.Clamp01((value - min) / (max - min));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float InverseModify(float value) => value * (max - min) + min;

        public double ModifyRead(in double value) => Mathf.Clamp01(((float)value - min) / (max - min));

        public double ModifyWrite(in double value) => value * (max - min) + min;
    }
}