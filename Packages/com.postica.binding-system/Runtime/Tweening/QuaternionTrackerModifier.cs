using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class QuaternionDampedTrackerModifier : DampedTrackerModifier<Quaternion>
    {
        protected override Quaternion Add(Quaternion a, Quaternion b) => a * b;

        protected override Quaternion Subtract(Quaternion a, Quaternion b) => a * Quaternion.Inverse(b);

        protected override Quaternion Multiply(Quaternion a, float b) => Quaternion.Euler(a.eulerAngles * b);
    }
}