using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class QuaternionEaseModifier : EaseModifier<Quaternion>
    {
        protected override Quaternion Lerp(Quaternion from, Quaternion to, float progress)
        {
            return Quaternion.Slerp(from, to, progress);
        }
    }
}